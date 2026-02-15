namespace Api.Domain.ValueObjects;

public class BuildMetadata
{
    public Dictionary<string, string> Data { get; set; } = new();

    public string? this[string key]
    {
        get => Data.TryGetValue(key, out var value) ? value : null;
        set
        {
            if (value != null)
                Data[key] = value;
            else
                Data.Remove(key);
        }
    }

    public string? Get(string key) => this[key];

    public void Set(string key, string? value)
    {
        if (value != null)
            Data[key] = value;
    }
}

public static class BuildMetadataKeys
{
    public const string CommitSha = "commitSha";
    public const string CommitMessage = "commitMessage";
    public const string Branch = "branch";
    public const string Author = "author";
}
