FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /source

COPY WebApp.FunctionInvoker.csproj .

RUN dotnet restore

COPY . .

RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "WebApp.FunctionInvoker.dll"]
