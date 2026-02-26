using System.Net;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace MycroCloud.WebApp.Gateway.Utils;

public static class CidrExtensions
{
    public static IPNetwork ParseCidr(this string cidr)
    {
        var parts = cidr.Split('/');
        if (parts.Length != 2)
            throw new ArgumentException($"Invalid CIDR: {cidr}");

        return new IPNetwork(
            IPAddress.Parse(parts[0]),
            int.Parse(parts[1])
        );
    }
}
