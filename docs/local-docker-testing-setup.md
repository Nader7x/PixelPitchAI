# Local Docker & Testcontainers Setup Guide (Windows + WSL 2)

This guide documents the configuration required to successfully run the `Footex.IntegrationTests` project locally on a Windows host when the Docker daemon is hosted inside WSL 2 (without Docker Desktop integrations).

---

## The Problem

By default, when you execute `dotnet test` natively on Windows, the .NET `Testcontainers` library searches for a Windows Named Pipe (`npipe://./pipe/docker_engine`) to communicate with the Docker daemon. 

If your Docker engine is running inside WSL 2 (e.g., you installed Docker directly in Ubuntu and exposed it over a TCP port or UNIX socket), Testcontainers will fail to connect, showing:
```text
DotNet.Testcontainers.Builders.DockerUnavailableException : Docker is either not running or misconfigured.
Details: Failed to connect to Docker endpoint at 'npipe://./pipe/docker_engine'.
```

---

## The Solution

To configure Testcontainers on Windows to route requests to your WSL 2 Docker daemon:

### 1. Identify Your Docker Context & Endpoint
Run `docker context ls` on Windows to check which context is currently active:
```powershell
docker context ls
```
Example output:
```text
NAME                DESCRIPTION                               DOCKER ENDPOINT
wsl-tcp-context *   Ported docker from wsl directly           tcp://localhost:2375
```
Inspect the active context to find the Host URL:
```powershell
docker context inspect wsl-tcp-context
```

### 2. Configure Testcontainers Properties
Testcontainers reads global configuration from a `.testcontainers.properties` file located in your user profile folder (`~/.testcontainers.properties` or `%USERPROFILE%\.testcontainers.properties`).

Create or update this file to define the Docker host:
```properties
docker.host=tcp://localhost:2375
ryuk.disabled=true
```

> [!NOTE]
> **Why `ryuk.disabled=true`?**
> The Ryuk container is a Testcontainers resource reaper used to clean up orphaned containers. However, pulling the Ryuk image can trigger Docker Hub `429 Too Many Requests` (rate limits) in developer environments. Since the `postgres:15-alpine` image is cached locally, disabling Ryuk allows you to run integration tests fully offline without making external registry calls.

---

## Verifying the Setup

Once the properties file is saved, run the integration tests from the repository root:
```powershell
dotnet test ./backend/tests/Footex.IntegrationTests/Footex.IntegrationTests.csproj
```
Testcontainers will now automatically discover the WSL 2 Docker daemon via the TCP port and spin up the PostgreSQL container.
