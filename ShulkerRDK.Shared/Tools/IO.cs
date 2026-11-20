using System.Security.Cryptography;
using System.Text.Json;

namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static void WriteAllText(string path,string content) {
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }
        File.WriteAllText(path,content);
    }

    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
        WriteIndented = true
    };

    public static string GetSha1(string s) {
        using FileStream file = File.OpenRead(s);
        byte[] rawHash = SHA1.HashData(file);
        return Convert.ToHexString(rawHash).ToLowerInvariant();
    }
}
