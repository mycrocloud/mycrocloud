using System.Data.Common;
using Dapper;
using Jint;
using Npgsql;

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

public static class TextStorageExtension
{
    public static void UseTextStorage(this Engine engine, Runtime runtime)
    {
        var connection = new NpgsqlConnection(runtime.ConnectionString);
        engine.SetValue(TextStorage.HookName,
            new Func<string, object>(name =>
            {
                var adapter = new TextStorage(runtime.AppId, name, connection);

                return new
                {
                    read = new Func<string>(() => adapter.Read()),
                    write = new Action<string>(content => adapter.Write(content))
                };
            }));
    }
}
