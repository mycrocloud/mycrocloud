using MockServer.Web.Models.ProjectSettings;
using MockServer.Web.Models.ProjectSettings.Auth;

namespace MockServer.Web.Services.Interfaces;

public interface IProjectSettingsWebService
{
    Task<IndexModel> GetIndexModel(int projectId);
    Task<AuthIndexModel> GetAuthIndexModel(int projectId);
    Task SaveAuthIndexModel(int projectId, AuthIndexModel model);
    Task CreateJwtBearerAuthentication(int projectId, JwtBearerAuthModel model);
    Task EditJwtBearerAuthentication(int id, JwtBearerAuthModel model);
    Task CreateApiKeyAuthentication(string name, ApiKeyAuthModel model);
    Task<JwtBearerAuthModel> GetJwtBearerAuthModel(string name, int id);
    Task<ApiKeyAuthModel> GetApiKeyAuthModel(string name, int id);
    Task<JwtBearerTokenGenerateModel> GenerateJwtBearerToken(string projectName, int schemeId, JwtBearerTokenGenerateModel model);
}