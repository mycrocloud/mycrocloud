FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder
WORKDIR /source

COPY WebApp.FunctionInvoker/WebApp.FunctionInvoker.csproj WebApp.FunctionInvoker/

RUN dotnet restore WebApp.FunctionInvoker/WebApp.FunctionInvoker.csproj

COPY WebApp.FunctionInvoker/ WebApp.FunctionInvoker/

RUN dotnet publish WebApp.FunctionInvoker/WebApp.FunctionInvoker.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "WebApp.FunctionInvoker.dll"]