using System.Net;
using System.Text;
using Application.Services;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Footex.UnitTests.Infrastructure.Services;

public class CloudflareR2SpatialTrackStorageServiceTests
{
    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

        public HttpRequestMessage? LastRequest { get; private set; }
        public string? LastContentType { get; private set; }

        public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastContentType = request.Content?.Headers.ContentType?.MediaType;
            return Task.FromResult(_handler(request));
        }
    }

    private static CloudflareR2SpatialTrackStorageService CreateService(
        StubHttpMessageHandler handler,
        bool configured = true
    )
    {
        var dict = new Dictionary<string, string?>();
        if (configured)
        {
            dict["R2:Endpoint"] = "https://r2.example.cloudflarestorage.com";
            dict["R2:BucketName"] = "pixelpitch-spatial-tracks";
            dict["R2:AccessKeyId"] = "test-access-key";
            dict["R2:SecretAccessKey"] = "test-secret-key";
        }

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient("R2SpatialStorage")).Returns(new HttpClient(handler));

        var azureFallback = new AzureBlobStorageService(config);

        return new CloudflareR2SpatialTrackStorageService(
            httpFactory.Object,
            azureFallback,
            config,
            NullLogger<CloudflareR2SpatialTrackStorageService>.Instance
        );
    }

    [Fact]
    public async Task UploadSpatialTrackAsync_SignsRequestWithSigV4AndZstdContentType()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var service = CreateService(handler);

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes("zstd-frame-payload"));
        var key = await service.UploadSpatialTrackAsync("/matches/42/spatial.zst", ms);

        key.Should().Be("/matches/42/spatial.zst");
        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Put);
        handler.LastRequest.RequestUri!.ToString().Should().Be("https://r2.example.cloudflarestorage.com/pixelpitch-spatial-tracks/matches/42/spatial.zst");
        handler.LastContentType.Should().Be("application/zstd");
        handler.LastRequest.Headers.TryGetValues("Authorization", out var authValues).Should().BeTrue();
        authValues!.Single().Should().StartWith("AWS4-HMAC-SHA256 Credential=test-access-key/");
        handler.LastRequest.Headers.Contains("x-amz-date").Should().BeTrue();
        handler.LastRequest.Headers.Contains("x-amz-content-sha256").Should().BeTrue();
    }

    [Fact]
    public async Task OpenSpatialTrackReadStreamAsync_ReturnsNullOnNotFoundAndStreamOnOk()
    {
        var notFoundHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var serviceNotFound = CreateService(notFoundHandler);

        var missing = await serviceNotFound.OpenSpatialTrackReadStreamAsync("matches/404/spatial.zst");
        missing.Should().BeNull();

        var okHandler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent([1, 2, 3, 4]),
        });
        var serviceOk = CreateService(okHandler);

        await using var stream = await serviceOk.OpenSpatialTrackReadStreamAsync("matches/200/spatial.zst");
        stream.Should().NotBeNull();
        using var copy = new MemoryStream();
        await stream!.CopyToAsync(copy);
        copy.ToArray().Should().Equal([1, 2, 3, 4]);
    }

    [Fact]
    public async Task DeleteSpatialTrackAsync_IgnoresNotFound()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var service = CreateService(handler);

        await service.DeleteSpatialTrackAsync("matches/404/spatial.zst");

        handler.LastRequest.Should().NotBeNull();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Delete);
    }
}
