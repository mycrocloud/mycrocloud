namespace WebApp.RestApi.Models;

public class AppBuildMessage
{
    public string Id { get; set; }
    public string RepoFullName { get; set; }
    public string CloneUrl { get; set; }
}