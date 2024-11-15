# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY WebApp.MiniApiGateway/WebApp.MiniApiGateway.csproj WebApp.MiniApiGateway/
COPY WebApp.Domain/WebApp.Domain.csproj WebApp.Domain/
COPY WebApp.Infrastructure/WebApp.Infrastructure.csproj WebApp.Infrastructure/
COPY WebApp.FunctionShared/WebApp.FunctionShared.csproj WebApp.FunctionShared/

RUN dotnet restore WebApp.MiniApiGateway/WebApp.MiniApiGateway.csproj

# copy and build app and libraries
COPY WebApp.MiniApiGateway/ WebApp.MiniApiGateway/
COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Infrastructure/ WebApp.Infrastructure/
COPY WebApp.FunctionShared/ WebApp.FunctionShared/

RUN dotnet publish WebApp.MiniApiGateway/WebApp.MiniApiGateway.csproj -c Release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebApp.MiniApiGateway.dll"]