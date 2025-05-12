using System.Text.Json.Serialization;
using Application;
using Infrastructure;
using Serilog;
using Serilog.Sinks.PostgreSQL;

// Initialize Serilog first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Configure Serilog
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);

        // Get column writers dictionary if configured in DI
        if (services.GetService(typeof(IDictionary<string, ColumnWriterBase>)) is IDictionary<string, ColumnWriterBase> columnWriters)
        {
            // Apply custom column writers for PostgreSQL sink
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
            loggerConfiguration.WriteTo.PostgreSQL(
                connectionString: connectionString,
                tableName: "Logs",
                columnOptions: columnWriters,
                needAutoCreateTable: true);
        }
    });

    // Add application and infrastructure services
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);

    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging(); // Add request logging

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}