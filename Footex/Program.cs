using System.Text.Json.Serialization;
using Application;
using Domain.Models;
using Infrastructure;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Sinks.PostgreSQL;
using System.Text;
using Application.Services;
using Footex.Extensions;
using Footex.Configuration;
using DotNetEnv;

// Load environment variables from .env file
var envPath = Path.Combine(Directory.GetCurrentDirectory(), "..", ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
}
else
{
    // Try loading from current directory
    Env.Load();
}

// Initialize Serilog first
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Override configuration with environment variables
    builder.Configuration.AddEnvironmentVariables();

    // Bind simulation service configuration
    builder.Services.Configure<SimulationServiceOptions>(options =>
    {
        options.BaseUrl = builder.Configuration["SimulationService:BaseUrl"] ??
                          Environment.GetEnvironmentVariable("SIMULATION_SERVICE_URL") ?? "http://localhost:8000";
        options.ApiKey = Environment.GetEnvironmentVariable("SIMULATION_API_KEY") ??
                         builder.Configuration["SimulationService:ApiKey"] ?? "";
    });

    // Add services to the container.
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowSomeOrigins",
            corsBuilder =>
            {
                corsBuilder.WithOrigins("http://localhost:3000", "https://localhost:3000" ,"https://localhost:80","https://localhost:433")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });
    });
    builder.Services.Configure<RouteOptions>(options => { options.LowercaseUrls = true; });

    // Configure Swagger with JWT support
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Footex API",
            Version = "v1",
            Description = "Football League Management API"
        });

        // Add JWT Authentication to Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                []
            }
        });
    });

    // Configure Serilog
    builder.Host.UseSerilog((context, services, loggerConfiguration) =>
    {
        loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services);

        // Get a column writers dictionary if configured in DI
        if (services.GetService(typeof(IDictionary<string, ColumnWriterBase>)) is not
            IDictionary<string, ColumnWriterBase>
            columnWriters) return;
        // Apply custom column writers for PostgresSQL sink
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection");
        loggerConfiguration.WriteTo.PostgreSQL(
            connectionString: connectionString,
            tableName: "Logs",
            columnOptions: columnWriters,
            needAutoCreateTable: true);
    });

    // Add application and infrastructure services
    builder.Services
        .AddApplication()
        .AddInfrastructure(builder.Configuration);
    // Add Identity configuration
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
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

    builder.Services.AddAuthentication(options =>
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
                ClockSkew = TimeSpan.Zero
            };
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && (path.StartsWithSegments("/Notify") ||
                                                               path.StartsWithSegments("/matchSimulationHub")))
                    {
                        // Read the token out of the query string
                        context.Token = accessToken;
                        
                    }

                    return Task.CompletedTask;
                }
            };
            options.IncludeErrorDetails = true;
            options.SaveToken = true;
        });

    // Add Authorization Policies
    builder.Services.AddAuthorizationBuilder()
        // Add Authorization Policies
        .AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"))
        // Add Authorization Policies
        .AddPolicy("PremiumUser", policy => policy.RequireRole("Premium"));


    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
    builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB
            options.ClientTimeoutInterval = TimeSpan.FromMinutes(3);
            options.KeepAliveInterval = TimeSpan.FromSeconds(9000); 
        })
        .AddJsonProtocol(options => options.PayloadSerializerOptions.PropertyNamingPolicy = null);

    var app = builder.Build();

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
    app.UseCors("AllowAllOrigins");
    app.UseAuthorization();
    app.MapControllers();

    // Seed roles and admin user on startup
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            // Seed roles
            var roles = new[] { "Admin", "User", "Premium" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Seed admin user
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
                        EmailConfirmed = true
                    };

                    var result = await userManager.CreateAsync(admin,
                        builder.Configuration["AdminUser:Password"] ?? string.Empty);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while seeding the database.");
        }
    }
    app.MapHub<NotificationService>("/Notify",
        options => { options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets; });
    app.MapHub<MatchHub>("/matchSimulationHub",
        options =>
        {
            options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
        });
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