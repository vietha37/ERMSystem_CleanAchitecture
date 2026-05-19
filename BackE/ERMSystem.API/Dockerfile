FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY BackE/ERMSystem.sln BackE/
COPY BackE/Directory.Build.props BackE/
COPY BackE/NuGet.Config BackE/
COPY BackE/ERMSystem.API/ BackE/ERMSystem.API/
COPY BackE/ERMSystem.Application/ BackE/ERMSystem.Application/
COPY BackE/ERMSystem.Domain/ BackE/ERMSystem.Domain/
COPY BackE/ERMSystem.Infrastructure/ BackE/ERMSystem.Infrastructure/

RUN dotnet restore BackE/ERMSystem.API/ERMSystem.API.csproj
RUN dotnet publish BackE/ERMSystem.API/ERMSystem.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ERMSystem.API.dll"]
