# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY WebApp.Domain/WebApp.Domain.csproj WebApp.Domain/
COPY WebApp.Infrastructure/WebApp.Infrastructure.csproj WebApp.Infrastructure/
COPY WebApp.FunctionShared/WebApp.FunctionShared.csproj WebApp.FunctionShared/
COPY WebApp.ApiGateway/WebApp.ApiGateway.csproj WebApp.ApiGateway/

RUN dotnet restore WebApp.ApiGateway/WebApp.ApiGateway.csproj

# copy and build app and libraries
COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Infrastructure/ WebApp.Infrastructure/
COPY WebApp.FunctionShared/ WebApp.FunctionShared/
COPY WebApp.ApiGateway/ WebApp.ApiGateway/

RUN dotnet publish WebApp.ApiGateway/WebApp.ApiGateway.csproj -c Release -o /app

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "WebApp.ApiGateway.dll"]