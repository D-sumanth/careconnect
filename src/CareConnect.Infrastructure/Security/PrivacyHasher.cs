using System.Security.Cryptography;
using System.Text;

namespace CareConnect.Infrastructure.Security;

internal static class PrivacyHasher
{
    public static string? Hash(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()));
        return Convert.ToHexString(bytes);
    }
}
