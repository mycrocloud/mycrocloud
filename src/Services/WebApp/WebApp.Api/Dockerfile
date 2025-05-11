# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY WebApp.RestApi/WebApp.RestApi.csproj WebApp.RestApi/
COPY WebApp.Domain/WebApp.Domain.csproj WebApp.Domain/
COPY WebApp.Infrastructure/WebApp.Infrastructure.csproj WebApp.Infrastructure/

RUN dotnet restore WebApp.RestApi/WebApp.RestApi.csproj

# copy and build app and libraries
COPY WebApp.RestApi/ WebApp.RestApi/
COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Infrastructure/ WebApp.Infrastructure/

ARG COMMIT_HASH

RUN dotnet publish WebApp.RestApi/WebApp.RestApi.csproj /p:SourceRevisionId=$COMMIT_HASH -c Release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebApp.RestApi.dll"]