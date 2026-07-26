# ── Stage 1: Build ──────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files first (for layer caching).
# All five projects in Upkilo.API's ProjectReference graph must be present before
# restore — omitting Upkilo.Application or Upkilo.AI fails the restore outright.
COPY src/backend/Upkilo.Core/Upkilo.Core.csproj Upkilo.Core/
COPY src/backend/Upkilo.Application/Upkilo.Application.csproj Upkilo.Application/
COPY src/backend/Upkilo.Infrastructure/Upkilo.Infrastructure.csproj Upkilo.Infrastructure/
COPY src/backend/Upkilo.AI/Upkilo.AI.csproj Upkilo.AI/
COPY src/backend/Upkilo.API/Upkilo.API.csproj Upkilo.API/

# Restore dependencies
RUN dotnet restore Upkilo.API/Upkilo.API.csproj

# Copy full source
COPY src/backend/Upkilo.Core/ Upkilo.Core/
COPY src/backend/Upkilo.Application/ Upkilo.Application/
COPY src/backend/Upkilo.Infrastructure/ Upkilo.Infrastructure/
COPY src/backend/Upkilo.AI/ Upkilo.AI/
COPY src/backend/Upkilo.API/ Upkilo.API/

# Build and publish
RUN dotnet publish Upkilo.API/Upkilo.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ── Stage 2: Runtime ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Security: run as non-root user
RUN groupadd -r upkilo && useradd -r -g upkilo upkilo

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*

# Copy published output
COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

# Health check
# start-period gives .NET 8 time to complete DI validation + EF warmup + Key Vault auth
# before Docker starts counting health-check failures. Without it the first check fires
# at t=0s and the container restarts before the app is ready.
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Run as non-root
USER upkilo

ENTRYPOINT ["dotnet", "Upkilo.API.dll"]
