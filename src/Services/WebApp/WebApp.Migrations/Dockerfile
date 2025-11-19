FROM mcr.microsoft.com/dotnet/sdk:8.0.202 AS build
WORKDIR /source

# Install EF tools
RUN dotnet tool install --global dotnet-ef --version 8.0.12 \
    && echo 'export PATH="$PATH:/root/.dotnet/tools"' >> ~/.bashrc
ENV PATH="/root/.dotnet/tools:${PATH}"

COPY WebApp.Domain/*.csproj WebApp.Domain/
COPY WebApp.Infrastructure/*.csproj WebApp.Infrastructure/
COPY WebApp.Migrations/*.csproj WebApp.Migrations/

RUN dotnet restore WebApp.Migrations/WebApp.Migrations.csproj

COPY WebApp.Infrastructure/ WebApp.Infrastructure/
COPY WebApp.Domain/ WebApp.Domain/
COPY WebApp.Migrations/ WebApp.Migrations/

RUN dotnet ef migrations bundle \
    -p WebApp.Migrations/WebApp.Migrations.csproj \
    -o /out/efbundle \
    -v

FROM mcr.microsoft.com/dotnet/runtime:8.0.3
WORKDIR /db-migrator
COPY --from=build /out/efbundle .
COPY WebApp.Migrations/appsettings.json .
ENTRYPOINT [ "/db-migrator/efbundle" ]