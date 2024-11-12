namespace WebApp.RestApi.Models;

public class AppBuildMessage
{
    public string Id { get; set; }
    public string RepoFullName { get; set; }
    public string CloneUrl { get; set; }
    public string Directory { get; set; }
    public string OutDir { get; set; }

    public string InstallCommand { get; set; }

    public string BuildCommand { get; set; }
}