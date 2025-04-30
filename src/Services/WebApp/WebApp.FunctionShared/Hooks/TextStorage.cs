using System.Data.Common;
using Dapper;

namespace WebApp.FunctionShared.Hooks;

public class TextStorage(int appId, string name, DbConnection connection)
{
    public const string HookName = "useTextStorage";

    public string Read()
    {
        const string sql = """
                            SELECT "Content" FROM "TextStorages" WHERE "AppId" = @AppId AND "Name" = @Name
                            """;
        return connection.ExecuteScalar<string>(sql, new
        {
            AppId = appId,
            Name = name
        }) ?? string.Empty;
    }

    public void Write(string content)
    {
        const string sql = """
                            UPDATE "TextStorages" SET "Content" = @Content WHERE "AppId" = @AppId AND "Name" = @Name
                            """;

        connection.Execute(sql, new
        {
            AppId = appId,
            Name = name,
            Content = content
        });
    }
}
