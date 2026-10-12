using System.Diagnostics;
using Application.CQRS;
using Application.CQRS.Matches.Commands;
using Application.CQRS.Matches.Queries;
using Application.CQRS.Notifications.Commands;
using Application.Dtos;
using Application.Interfaces;
using Application.Services;
using Domain.Interfaces;
using Domain.Models;
using Footex.Configuration;
using Footex.Controllers;
using global::Infrastructure;
using global::Infrastructure.Configuration;
using global::Infrastructure.Repositories;
using global::Infrastructure.Services;
using global::Infrastructure.Services.EventProcessors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services;

public sealed class PythonGrpcFactAttribute : FactAttribute
{
    public PythonGrpcFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PIXELPITCH_STREAM_TEST_PYTHON")))
            Skip = "Set PIXELPITCH_STREAM_TEST_PYTHON to the simulation engine Python executable for the wire smoke test.";
    }
}

public class PipelineBWireSmokeTests
{
    [PythonGrpcFact]
    public async Task ControllerToPythonGrpcToPersistedResult()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !Directory.Exists(Path.Combine(root.FullName, "simulation-engine"))) root = root.Parent;
        Assert.NotNull(root);
        var start = new ProcessStartInfo(Environment.GetEnvironmentVariable("PIXELPITCH_STREAM_TEST_PYTHON")!)
        { RedirectStandardOutput = true, RedirectStandardError = true, RedirectStandardInput = true, UseShellExecute = false, CreateNoWindow = true };
        start.ArgumentList.Add(Path.Combine(root!.FullName, "simulation-engine", "tests", "fake_grpc_engine.py"));
        using var python = Process.Start(start)!;
        var errors = python.StandardError.ReadToEndAsync();
        try
        {
            var port = await python.StandardOutput.ReadLineAsync().WaitAsync(TimeSpan.FromSeconds(15));
            Assert.True(int.TryParse(port, out _), $"Fixture did not report a port: {port}");
            var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var services = new ServiceCollection();
            var database = Guid.NewGuid().ToString();
            services.AddDbContext<FootballDbContext>(o => o.UseInMemoryDatabase(database));
            services.AddScoped<IUnitOfWork>(p =>
            {
                var db = p.GetRequiredService<FootballDbContext>();
                var unit = new Mock<IUnitOfWork>();
                unit.SetupGet(u => u.Matches).Returns(new MatchRepository(db));
                unit.SetupGet(u => u.MatchEvents).Returns(new MatchEventsRepository(db));
                unit.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(async (CancellationToken token) =>
                {
                    var count = await db.SaveChangesAsync(token);
                    if (db.ChangeTracker.Entries<Domain.Models.Match>().Any(e => e.Entity.MatchStatus == "Completed")) finished.TrySetResult();
                    return count;
                });
                return unit.Object;
            });
            services.AddSingleton<IEventAnalysisService>(new EventAnalysisService([new MatchStatusEventProcessor(), new ShotEventProcessor()]));
            using var provider = services.BuildServiceProvider();
            using (var seed = provider.CreateScope())
            {
                var db = seed.ServiceProvider.GetRequiredService<FootballDbContext>();
                db.Teams.AddRange(new Team { Id = 1, Name = "Home" }, new Team { Id = 2, Name = "Away" });
                db.Users.Add(new ApplicationUser { Id = "user", UserName = "user", FirstName = "Test", LastName = "User" });
                await db.SaveChangesAsync();
            }
            var scopes = provider.GetRequiredService<IServiceScopeFactory>();
            var live = Mock.Of<ILiveMatchStatisticsService>();
            var performance = Mock.Of<IPerformanceMonitoringService>();
            var processor = new MatchEventProcessor(scopes, performance, live, NullLogger<MatchEventProcessor>.Instance);
            using var grpc = new SimulationGrpcClient(Options.Create(new SimulationGrpcOptions { ServerUrl = $"http://127.0.0.1:{port}" }), NullLogger<SimulationGrpcClient>.Instance);
            var consumer = new MatchEventGrpcStreamConsumer(grpc, processor, NullLogger<MatchEventGrpcStreamConsumer>.Instance);
            var lifecycle = new MatchStreamLifecycle(scopes);
            var queue = new MatchStreamingQueue();
            using var worker = new MatchStreamWorker(queue, consumer, Options.Create(new EventIngestionOptions()), NullLogger<MatchStreamWorker>.Instance, lifecycle);
            using var requestScope = provider.CreateScope();
            var requestDb = requestScope.ServiceProvider.GetRequiredService<FootballDbContext>();
            var create = new Mock<IRequestHandler<CreateMatchCommand, CreateMatchCommandResponse>>();
            create.Setup(h => h.Handle(It.IsAny<CreateMatchCommand>(), It.IsAny<CancellationToken>())).Returns(async (CreateMatchCommand command, CancellationToken token) =>
            {
                requestDb.Matches.Add(new Domain.Models.Match { Id = 42, CreatorId = "user", HomeTeamId = 1, AwayTeamId = 2,
                    MatchStatus = command.MatchStatus, IsLive = command.IsLive,
                    HomeTeamInMatchName = command.HomeTeamInMatchName, AwayTeamInMatchName = command.AwayTeamInMatchName });
                await requestDb.SaveChangesAsync(token);
                return new CreateMatchCommandResponse { Succeeded = true, Id = 42 };
            });
            var liveHandler = new Mock<IRequestHandler<GetLiveMatchQuery, GetLiveMatchQueryResponse>>();
            liveHandler.Setup(h => h.Handle(It.IsAny<GetLiveMatchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new GetLiveMatchQueryResponse());
            var notifications = new Mock<IRequestHandler<CreateNotificationCommand, CreateNotificationCommandResponse>>();
            notifications.Setup(h => h.Handle(It.IsAny<CreateNotificationCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new CreateNotificationCommandResponse { Succeeded = true });
            var update = Mock.Of<IRequestHandler<UpdateMatchStatusCommand, UpdateMatchStatusCommandResponse>>();
            var controller = new MatchesController(Mock.Of<IHttpClientFactory>(), Mock.Of<IMatchMapper>(), Options.Create(new SimulationServiceOptions()), scopes,
                requestScope.ServiceProvider.GetRequiredService<IUnitOfWork>(), performance, live, NullLogger<MatchesController>.Instance,
                Mock.Of<IHubContext<NotificationService, INotificationService>>(), Mock.Of<ICacheService>(),
                Options.Create(new EventIngestionOptions { Mode = "GrpcStream" }), queue, grpc, lifecycle);
            await worker.StartAsync(default);
            var accepted = await controller.SimulateMatch("user", new SimulateMatchDto { HomeTeamId = 1, AwayTeamId = 2, HomeTeamName = "Home", AwayTeamName = "Away",
                HomeTeamSeason = "2019/2020", AwayTeamSeason = "2020/2021", HomeSeasonId = 1, AwaySeasonId = 2 }, default,
                create.Object, update, notifications.Object, liveHandler.Object);
            Assert.IsType<OkObjectResult>(accepted.Result);
            await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await worker.StopAsync(default);
            var result = await controller.GetSimulationResult("grpc_42", default, notifications.Object, update, Mock.Of<IRequestHandler<UpdateMatchCommand, UpdateMatchCommandResponse>>());
            var response = Assert.IsType<SimulationResultResponse>(Assert.IsType<ObjectResult>(result.Result).Value);
            Assert.Equal("completed", response.Status);
            Assert.Equal(1, response.HomeTeamScore);
            Assert.Equal(3, response.EventsCount);
            using var verification = provider.CreateScope();
            var persisted = await verification.ServiceProvider.GetRequiredService<FootballDbContext>().Matches.Include(m => m.MatchEvents).SingleAsync(m => m.Id == 42);
            Assert.False(persisted.IsLive);
            Assert.Equal("Saúl Ñíguez", persisted.MatchEvents!.GetEvents<List<FootballMatchEvent>>()!.Single(e => e.action == "shot").player);
        }
        finally
        {
            if (!python.HasExited)
            {
                await python.StandardInput.WriteLineAsync("stop");
                await python.StandardInput.FlushAsync();
                try { await python.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
                catch (TimeoutException) { python.Kill(entireProcessTree: true); }
            }
            await errors;
        }
    }
}
