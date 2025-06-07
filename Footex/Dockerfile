# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["Footex/Footex.csproj", "Footex/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

# Restore dependencies
RUN dotnet restore "Footex/Footex.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/Footex"
RUN dotnet build "Footex.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Footex.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN groupadd -r footex && useradd -r -g footex footex

# Create required directories and set permissions
RUN mkdir -p /app/Logs /app/wwwroot /app/temp && \
    chown -R footex:footex /app

# Copy published application
COPY --from=publish /app/publish/ ./

# Copy .env file if it exists
COPY --from=build /src/.env* ./ 

# Switch to non-root user
USER footex

# Expose port
EXPOSE 8080

# Health check with proper endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=30s --retries=3 \
  CMD curl -f http://localhost:8080/api/health || exit 1

# Entry point
ENTRYPOINT ["dotnet", "Footex.dll"]
