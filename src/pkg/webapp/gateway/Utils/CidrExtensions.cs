using System.Net;

namespace MycroCloud.WebApp.Gateway.Utils;

public static class CidrExtensions
{
    public static IPNetwork ParseCidr(this string cidr)
    {
        return IPNetwork.Parse(cidr);
    }
}
