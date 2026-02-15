namespace Api.Domain.Entities;

public class Variable : BaseEntity
{
    public int Id { get; set; }
    public int AppId { get; set; }
    public string Name { get; set; }
    public string Value { get; set; }
    public bool IsSecret { get; set; }
    public VariableTarget Target { get; set; } = VariableTarget.Runtime;
    public App App { get; set; }
}

/// <summary>
/// Specifies where the variable is used
/// </summary>
public enum VariableTarget
{
    Runtime,    // Available at runtime only
    Build,      // Available during build only
    All         // Available both at build and runtime
}