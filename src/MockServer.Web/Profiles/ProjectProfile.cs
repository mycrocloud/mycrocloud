using AutoMapper;
using MockServer.Core.Models.Projects;
using MockServer.Web.Models.Projects;

namespace MockServer.Web.Profiles;

public class ProjectProfile : Profile
{
    public ProjectProfile()
    {
        CreateMap<CreateProjectViewModel, Core.Models.Projects.Project>().ReverseMap();
        CreateMap<Core.Models.Projects.Project, ProjectIndexItem>().ReverseMap();
    }
}
