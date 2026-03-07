using System.Security.Cryptography;
using System.Text;

namespace Api.Utils;

public static class TokenUtils
{
    public static string GenerateReadableToken(string prefix, int byteLength)
    {
        var randomBytes = new byte[byteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var base64 = Convert.ToBase64String(randomBytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");

        return $"{prefix}_{base64}";
    }

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}