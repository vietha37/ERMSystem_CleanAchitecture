FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for efficient Docker layer caching
COPY BackE/ERMSystem.sln BackE/
COPY BackE/Directory.Build.props BackE/
COPY BackE/NuGet.Config BackE/
COPY BackE/ERMSystem.Domain/ERMSystem.Domain.csproj BackE/ERMSystem.Domain/
COPY BackE/ERMSystem.Application/ERMSystem.Application.csproj BackE/ERMSystem.Application/
COPY BackE/ERMSystem.Infrastructure/ERMSystem.Infrastructure.csproj BackE/ERMSystem.Infrastructure/
COPY BackE/ERMSystem.API/ERMSystem.API.csproj BackE/ERMSystem.API/

RUN dotnet restore BackE/ERMSystem.API/ERMSystem.API.csproj

# Copy source code after dependencies are cached
COPY BackE/ERMSystem.Domain/ BackE/ERMSystem.Domain/
COPY BackE/ERMSystem.Application/ BackE/ERMSystem.Application/
COPY BackE/ERMSystem.Infrastructure/ BackE/ERMSystem.Infrastructure/
COPY BackE/ERMSystem.API/ BackE/ERMSystem.API/

RUN dotnet publish BackE/ERMSystem.API/ERMSystem.API.csproj -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

# Security: Prepare DataProtectionKeys directory with app user permissions and run as non-root user
RUN mkdir -p /app/App_Data/DataProtectionKeys \
    && chown -R app:app /app

USER app

ENTRYPOINT ["dotnet", "ERMSystem.API.dll"]
