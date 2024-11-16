FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /source

COPY WebApp.Domain/WebApp.Domain.csproj WebApp.Domain/
COPY WebApp.Infrastructure/WebApp.Infrastructure.csproj WebApp.Infrastructure/
COPY WebApp.FunctionShared/WebApp.FunctionShared.csproj WebApp.FunctionShared/
COPY WebApp.FunctionInvoker/WebApp.FunctionInvoker.csproj WebApp.FunctionInvoker/

RUN dotnet restore WebApp.FunctionInvoker/WebApp.FunctionInvoker.csproj

COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Infrastructure/ WebApp.Infrastructure/
COPY WebApp.FunctionShared/ WebApp.FunctionShared/
COPY WebApp.FunctionInvoker/ WebApp.FunctionInvoker/

RUN dotnet publish WebApp.FunctionInvoker/WebApp.FunctionInvoker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "WebApp.FunctionInvoker.dll"]