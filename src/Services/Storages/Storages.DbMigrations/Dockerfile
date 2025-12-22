FROM mcr.microsoft.com/dotnet/sdk:10.0.101 AS build
WORKDIR /source

RUN dotnet tool install --global dotnet-ef --version 10.0.1
ENV PATH="${PATH}:/root/.dotnet/tools"

COPY Storages.Core/*.csproj Storages.Core/
COPY Storages.DbMigrations/*.csproj Storages.DbMigrations/

RUN dotnet restore Storages.DbMigrations/Storages.DbMigrations.csproj

COPY Storages.Core/ Storages.Core/
COPY Storages.DbMigrations/ Storages.DbMigrations/

RUN dotnet build Storages.DbMigrations/Storages.DbMigrations.csproj \
    && dotnet ef migrations bundle -p Storages.DbMigrations/Storages.DbMigrations.csproj -o /out/efbundle -v

FROM mcr.microsoft.com/dotnet/runtime:10.0.1
WORKDIR /db-migrator
COPY --from=build /out/efbundle .
COPY Storages.DbMigrations/appsettings.json .
ENTRYPOINT [ "/db-migrator/efbundle" ]