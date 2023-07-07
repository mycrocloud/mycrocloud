using Ocelot.Configuration.File;
using System.Collections.Generic;

namespace Ocelot.Configuration.Creator
{
    public interface IRoutesCreator
    {
        List<Route> Create(FileConfiguration fileConfiguration);
        List<Route> Create(IEnumerable<MockServer.Domain.WebApplications.Route> routes);
    }
}
