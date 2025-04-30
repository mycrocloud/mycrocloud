using System.Data.Common;
using Dapper;

namespace WebApp.FunctionShared.Hooks;

public class ObjectStorage(int appId, DbConnection connection)
{
    public const string HookName = "useObjectStorage";
    
    public byte[] Read(string key)
    {
        const string sql = """
                           SELECT "Content" FROM "Objects" WHERE "AppId" = @AppId AND "Key" = @Key
                           """;
        
        return connection.ExecuteScalar<byte[]>(sql, new
        {
            AppId = appId,
            Key = key
        })!;
    }

    public void Write(string key, byte[] content)
    {
        const string sql = """
                           INSERT INTO "Objects" ("AppId", "Key", "Content")
                           VALUES (@AppId, @Key, @Content)
                           ON CONFLICT ("AppId", "Key")
                           DO UPDATE SET "Content" = EXCLUDED."Content";
                           """;
        
        connection.Execute(sql, new
        {
            AppId = appId,
            Key = key,
            Content = content
        });
    }
}