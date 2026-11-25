using System.Security.Cryptography;
using System.Text;
using ShulkerRDK.Prismarine.Shared;
using TridentCore.Abstractions.Accounts;
using TridentCore.Core.Accounts;

namespace ShulkerRDK.Prismarine.Services;

public static class AccountResolver {
    public static IAccount ResolveOffline(string? usernameOverride = null, string? uuidOverride = null) {
        LaunchSettings launch = PrismarineConfig.Instance.Launch;

        string username = !string.IsNullOrWhiteSpace(usernameOverride)
            ? usernameOverride.Trim()
            : !string.IsNullOrWhiteSpace(launch.Username)
                ? launch.Username.Trim()
                : "DevPlayer";

        string? rawUuid = !string.IsNullOrWhiteSpace(uuidOverride)
            ? uuidOverride.Trim()
            : launch.Uuid?.Trim();

        string uuid = !string.IsNullOrWhiteSpace(rawUuid) && Guid.TryParse(rawUuid, out Guid parsedGuid)
            ? parsedGuid.ToString("N")
            : GenerateOfflineUuid(username);

        return new OfflineAccount {
            Username = username,
            Uuid = uuid
        };
    }

    public static string GenerateOfflineUuid(string username) {
        byte[] hash = MD5.HashData(Encoding.UTF8.GetBytes($"OfflinePlayer:{username}"));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30); // Version 3 (MD5)
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // RFC 4122 variant
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
