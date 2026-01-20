using System.Security.Cryptography;

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
}