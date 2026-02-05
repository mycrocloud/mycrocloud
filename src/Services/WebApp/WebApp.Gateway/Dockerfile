# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY WebApp.Domain/WebApp.Domain.csproj WebApp.Domain/
COPY WebApp.Infrastructure/WebApp.Infrastructure.csproj WebApp.Infrastructure/
COPY WebApp.Gateway/WebApp.Gateway.csproj WebApp.Gateway/

RUN dotnet restore WebApp.Gateway/WebApp.Gateway.csproj

# copy and build app and libraries
COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Infrastructure/ WebApp.Infrastructure/
COPY WebApp.Gateway/ WebApp.Gateway/

RUN dotnet publish WebApp.Gateway/WebApp.Gateway.csproj -c Release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebApp.Gateway.dll"]