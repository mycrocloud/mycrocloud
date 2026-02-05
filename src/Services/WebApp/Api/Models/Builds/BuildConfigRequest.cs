namespace Api.Models.Builds;

public class BuildConfigRequest
{
    public string Branch { get; set; }
    public string Directory { get; set; }
    public string InstallCommand { get; set; }
    public string BuildCommand { get; set; }
    public string OutDir { get; set; }
    public string NodeVersion { get; set; }
    public string Framework { get; set; }
}