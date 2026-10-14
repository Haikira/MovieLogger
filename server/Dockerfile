# Production image for MovieLogger.Api. Build from the repository root with:
#   docker build -t movielogger-api -f server/Dockerfile server
# (or `make docker-build`). The build context is server/, filtered by server/.dockerignore.
# See docs/deployment.md for the configuration the container needs at runtime.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from the project files alone, so the restore layer is cached until a dependency changes.
COPY src/MovieLogger.Api/MovieLogger.Api.csproj src/MovieLogger.Api/
COPY src/MovieLogger.Service/MovieLogger.Service.csproj src/MovieLogger.Service/
COPY src/MovieLogger.DAL/MovieLogger.DAL.csproj src/MovieLogger.DAL/
RUN dotnet restore src/MovieLogger.Api/MovieLogger.Api.csproj

COPY src/ src/
RUN dotnet publish src/MovieLogger.Api/MovieLogger.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# The .NET images listen on 8080 by default; set explicitly because the load balancer targets it.
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Run as the image's built-in non-root user.
USER $APP_UID

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MovieLogger.Api.dll"]
