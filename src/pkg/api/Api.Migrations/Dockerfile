FROM mcr.microsoft.com/dotnet/sdk:8.0.202 AS build
WORKDIR /source

# Install EF tools
RUN dotnet tool install --global dotnet-ef --version 8.0.22 \
    && echo 'export PATH="$PATH:/root/.dotnet/tools"' >> ~/.bashrc
ENV PATH="/root/.dotnet/tools:${PATH}"

COPY Api.Domain/*.csproj Api.Domain/
COPY Api.Infrastructure/*.csproj Api.Infrastructure/
COPY Api.Migrations/*.csproj Api.Migrations/

RUN dotnet restore Api.Migrations/Api.Migrations.csproj

COPY Api.Infrastructure/ Api.Infrastructure/
COPY Api.Domain/ Api.Domain/
COPY Api.Migrations/ Api.Migrations/

RUN dotnet ef migrations bundle \
    -p Api.Migrations/Api.Migrations.csproj \
    -o /out/efbundle \
    -v

FROM mcr.microsoft.com/dotnet/runtime:8.0.3
WORKDIR /db-migrator
COPY --from=build /out/efbundle .
COPY Api.Migrations/appsettings.json .
ENTRYPOINT [ "/db-migrator/efbundle" ]