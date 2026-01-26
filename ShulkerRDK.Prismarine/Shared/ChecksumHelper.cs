using System.Security.Cryptography;
using System.Text;

namespace ShulkerRDK.Prismarine.Shared;

public static class ChecksumHelper {
    public static string GetChecksum(FileStream file) {
        return GetSha1(file);
    }

    public static string GetChecksum(string str,Encoding? encoding = null) {
        return GetStringSha1(str,encoding);
    }

public static string GetFileChecksum(string filePath) {
        FileStream fs = new FileStream(filePath, FileMode.Open);
        string cs = GetChecksum(fs);
        fs.Close();
        return cs;
    }

    static string GetSha1(FileStream file) {
#pragma warning disable SYSLIB0021
        SHA1 sha1 = new SHA1CryptoServiceProvider();
#pragma warning restore SYSLIB0021
        byte[] rawHash = sha1.ComputeHash(file);
        StringBuilder sc = new StringBuilder();
        foreach (byte t in rawHash) {
            sc.Append(t.ToString("x2"));
        }
        return sc.ToString();
    }
    
    static string GetStringSha1(string str, Encoding? encoding = null) {
        encoding ??= Encoding.UTF8;
        byte[] byteArray = encoding.GetBytes(str);
        byte[] rawHash = SHA1.HashData(byteArray);
        StringBuilder sc = new StringBuilder();
        foreach (byte t in rawHash) {
            sc.Append(t.ToString("x2"));
        }
        return sc.ToString();
    }
}