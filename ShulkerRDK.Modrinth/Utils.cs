using System.Security.Cryptography;
using System.Text;

namespace ShulkerRDK.Modrinth;

public static class Utils {
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