# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY WebApp.SlackIntegrationApi/WebApp.SlackIntegrationApi.csproj WebApp.SlackIntegrationApi/
COPY WebApp.Domain/WebApp.Domain.csproj WebApp.Domain/
COPY WebApp.Infrastructure/WebApp.Infrastructure.csproj WebApp.Infrastructure/

RUN dotnet restore WebApp.SlackIntegrationApi/WebApp.SlackIntegrationApi.csproj

# copy and build app and libraries
COPY WebApp.SlackIntegrationApi/ WebApp.SlackIntegrationApi/
COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Infrastructure/ WebApp.Infrastructure/

ARG COMMIT_HASH

RUN dotnet publish WebApp.SlackIntegrationApi/WebApp.SlackIntegrationApi.csproj /p:SourceRevisionId=$COMMIT_HASH -c Release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebApp.SlackIntegrationApi.dll"]