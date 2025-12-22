Add migration
```shell
# change directory to root
cd ..
dotnet ef migrations --project Storages.DbMigrations --startup-project Storages.DbMigrations add <name>
```