using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static void WriteAllText(string path,string content) {
        if (!Directory.Exists(Path.GetDirectoryName(path)!)) {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        File.WriteAllText(path,content);
    }

    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
        WriteIndented = true
    };

    public static string GetSha1(string s) {
        FileStream file = new FileStream(s,FileMode.Open);
#pragma warning disable SYSLIB0021
        SHA1 sha1 = new SHA1CryptoServiceProvider();
#pragma warning restore SYSLIB0021
        byte[] rawHash = sha1.ComputeHash(file);
        file.Close();

        StringBuilder sc = new StringBuilder();
        foreach (byte t in rawHash) {
            sc.Append(t.ToString("x2"));
        }
        return sc.ToString();
    }
}
