using System.Security.Cryptography;
using System.Text;

namespace E2e.Bdd.Support;

internal static class WebhookSigner
{
    public static string Sign(string dataId, string requestId, string secret, string? ts = null)
    {
        ts ??= DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var manifest = $"id:{dataId.ToLowerInvariant()};request-id:{requestId};ts:{ts};";
        var hash = Convert.ToHexString(HMACSHA256.HashData(
                Encoding.UTF8.GetBytes(secret),
                Encoding.UTF8.GetBytes(manifest)))
            .ToLowerInvariant();
        return $"ts={ts},v1={hash}";
    }
}
