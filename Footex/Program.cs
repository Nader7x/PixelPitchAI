using System.Net;
using System.Text;
using System.Text.Json.Serialization;
using Application;
using Application.Services;
using Domain.Models;
using DotNetEnv;
using Footex.Configuration;
using Footex.Extensions;
using HealthChecks.UI.Client;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Sinks.PostgreSQL;

// Load environment variables from .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
    Env.Load(envPath);
else
    // Try loading from the current directory
    Env.Load();

// Initialize Serilog first
Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Services.Configure<HostOptions>(ho =>
    {
        ho.ServicesStartConcurrently = true;
        ho.ServicesStopConcurrently = false;
    });
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // You should configure KnownProxies or KnownNetworks to only trust your specific reverse proxy.
        // This is crucial for security to prevent IP spoofing.
        // Example: If your proxy is on localhost (e.g., during development with Kestrel behind IIS Express or another local proxy)
        options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
        options.KnownProxies.Add(IPAddress.Parse("::1")); // IPv6 loopback

        // If your proxy has a specific IP or is within a specific network:
        // options.KnownProxies.Add(IPAddress.Parse("YOUR_PROXY_IP_ADDRESS"));
        // options.KnownNetworks.Add(new IPNetwork(IPAddress.Parse("YOUR_PROXY_NETWORK_PREFIX"), YOUR_PROXY_NETWORK_PREFIX_LENGTH));

        // If you don't set KnownProxies/KnownNetworks, it might trust any proxy, which is a security risk.
        // For cloud environments (Azure App Service, AWS ELB, etc.), these services often handle
        // X-Forwarded-For correctly and might not require explicit KnownProxies if configured to do so.
        // However, it's best practice to be explicit if possible.
        // If you are unsure about your proxy's IP, you might need to log HttpContext.Connection.RemoteIpAddress
        // *before* UseForwardedHeaders to identify it.
    });
    // Override configuration with environment variables
    builder.Configuration.AddUserSecrets<Program>();
    builder.Configuration.AddEnvironmentVariables();

   
    // Bind simulation service configuration
    builder.Services.Configure<SimulationServiceOptions>(options =>
    {
        options.BaseUrl =
            builder.Configuration["SimulationService:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("SIMULATION_SERVICE_URL")
            ?? "http://localhost:8000";
        options.ApiKey =
            Environment.GetEnvironmentVariable("SIMULATION_API_KEY")
            ?? builder.Configuration["SimulationService:ApiKey"]
            ?? "";
    });

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(
            "AllowSomeOrigins",
            corsBuilder =>
            {
                corsBuilder
                    .WithOrigins(
                        "http://localhost:3000",
                        "https://localhost:3000",
                        "https://gourav-d.github.io/SignalR-Web-Client",
                        "https://localhost:7082",
                        "http://localhost:5025", // HTTP API
                        "http://localhost:5025/swagger", // Swagger UI HTTP
                        "https://localhost:7082/swagger"
                    ) // Swagger UI HTTPS
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials()
                    .SetIsOriginAllowedToAllowWildcardSubdomains();
            }
        );
    });
    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
    });

    // Configure Swagger with JWT support
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc(
            "v1",
            new OpenApiInfo
            {
                Title = "Footex API",
                Version = "v1",
                Description = "Football League Management API",
            }
        ); // Explicitly define the server Swagger UI should use
        c.AddServer(
            new OpenApiServer
            {
                Url = "https://localhost:7082",
                Description = "Development HTTPS Server",
            }
        );
        c.AddServer(
            new OpenApiServer
            {
                Url = "http://localhost:5025",
                Description = "Development HTTP Server",
            }
        );

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition(
            "Bearer",
            new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "Bearer",
            }
        );

        c.AddSecurityRequirement(
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer",
                        },
                    },
                    []
                },
            }
        );
    });

    // Configure Serilog
    builder.Host.UseSerilog(
        (context, services, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services);

            // Get a column writers dictionary if configured in DI
            if (
                services.GetService(typeof(IDictionary<string, ColumnWriterBase>))
                is not IDictionary<string, ColumnWriterBase> columnWriters
            )
                return;
            // Apply custom column writers for PostgresSQL sink
            var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
            loggerConfiguration.WriteTo.PostgreSQL(
                connectionString,
                "Logs",
                columnWriters,
                needAutoCreateTable: true
            );
        }
    );

    // Add application and infrastructure services
    builder.Services.AddApplication().AddInfrastructure(builder.Configuration);
    // Add Identity configuration
    builder
        .Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            // User settings
            options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;

            // User settings
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<FootballDbContext>()
        .AddDefaultTokenProviders();

    // Add JWT Authentication
    var jwtSettings = builder.Configuration.GetSection("JWT");
    var key = Encoding.UTF8.GetBytes(jwtSettings["Secret"] ?? string.Empty);

    builder
        .Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultForbidScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignOutScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = false; // Set to true in production
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["ValidIssuer"],
                ValidAudience = jwtSettings["ValidAudience"],
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero,
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (
                        !string.IsNullOrEmpty(accessToken)
                        && (
                            path.StartsWithSegments("/Notify")
                            || path.StartsWithSegments("/matchSimulationHub")
                        )
                    )
                        // Read the token out of the query string
                        context.Token = accessToken;

                    return Task.CompletedTask;
                },
            };
            options.IncludeErrorDetails = true;
            options.SaveToken = true;
        });

    // Add Authorization Policies
    builder
        .Services.AddAuthorizationBuilder()
        // Add Authorization Policies
        .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
        // Add Authorization Policies
        .AddPolicy("PremiumUser", policy => policy.RequireRole("Premium"));

    builder
        .Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
    builder
        .Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
        })
        .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = null);

    // Add Health Checks
    builder
        .Services.AddHealthChecks()
        .AddNpgSql(
            builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
            name: "PostgresSQL",
            tags: ["db", "postgres"]
        )
        .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"));

    var app = builder.Build();
    app.UseForwardedHeaders();
    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Footex API v1"));
        app.ApplyMigrations();
    }

    app.UseHttpsRedirection();
    app.UseSerilogRequestLogging(); // Add request logging

    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseWebSockets();
    app.UseCors("AllowSomeOrigins");
    app.UseAuthorization();

    // Map health check endpoint
    app.MapHealthChecks(
        "/api/health",
        new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse }
    );

    app.MapControllers();
    if (!app.Environment.IsEnvironment("Testing"))
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            var roles = new[] { "Admin", "User", "Premium" };
            foreach (var role in roles)
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

            var adminEmail = builder.Configuration["AdminUser:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var admin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "Admin",
                        LastName = "User",
                        EmailConfirmed = true,
                    };

                    var result = await userManager.CreateAsync(
                        admin,
                        builder.Configuration["AdminUser:Password"] ?? string.Empty
                    );
                    if (result.Succeeded)
                        await userManager.AddToRoleAsync(admin, "Admin");
                }
            }

            if (!app.Environment.IsDevelopment() || !app.Environment.IsEnvironment("Testing"))
            {
                using var dataScope = app.Services.CreateScope();
                var dataSeeder = dataScope.ServiceProvider.GetRequiredService<DataSeeder>();
                await dataSeeder.SeedAllAsync();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }

    app.MapHub<NotificationService>(
        "/Notify",
        options =>
        {
            options.Transports = HttpTransportType.WebSockets;
        }
    );
    app.MapHub<MatchHub>(
        "/matchSimulationHub",
        options =>
        {
            options.Transports = HttpTransportType.WebSockets;
        }
    );
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

public abstract partial class Program;
