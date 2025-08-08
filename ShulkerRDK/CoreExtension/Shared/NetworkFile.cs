using System.Text.Json;
using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Shared;

public class NetworkFile {
    public required string? Sha1 { get; set; }
    public required string Link { get; set; }

    public static void Create(string path, string url, IChainedLikeTerminal? ct = null) {
        const string cache = "./shulker/local/cache/netFile.analyzing";
        string opath = $"{path}.nfm";
        ct?.WriteLine($"&7正在获取文件&8[&7{url}&8]");
        FileDownloader.DownloadFile(url,cache);
        ct?.WriteLine("&7正在分析");
        string sha1 = Tools.GetSha1(cache);
        if (!File.Exists(Path.Combine(LocalPath,sha1))) {
            if (!Directory.Exists(LocalPath)) {
                Directory.CreateDirectory(LocalPath);
            }
            ct?.WriteLine("&7正在存入");
            File.Move(cache,Path.Combine(LocalPath,sha1));   
        }
        //todo: 修复根目录下操作时的崩溃问题
        Tools.WriteAllText(opath,JsonSerializer.Serialize(new NetworkFile {
            Sha1 = sha1,
            Link = url
        },Tools.JsonSerializerOptions));
        ct?.WriteLine("&7完成!");
    }
    
    public const string LocalPath = "./shulker/local/netFiles/";
    public static void Restore(string input,string output, IChainedLikeTerminal? ct = null,bool destroySource = false) {
        string[] files = Directory.GetFiles(input,"*.nfm",SearchOption.AllDirectories);
        ct?.WriteLine($"&7正在复原&8[&7{input}&8]");
        foreach (string filePath in files) {
            NetworkFile? file = JsonSerializer.Deserialize<NetworkFile>(File.ReadAllText(filePath));
            if (file == null) continue;
            ct?.WriteLine($"&7正在补全 &8[&7{filePath}&8] {file.Sha1}",Terminal.MessageType.Debug);
            string relativePath = Path.GetRelativePath(input,filePath);
            string destPath = Path.Combine(output,relativePath);
            destPath = Path.ChangeExtension(destPath,"");
            if (file.Sha1 != null) {
                if (!File.Exists(Path.Combine(LocalPath,file.Sha1))) {
                    ct?.WriteLine($"&7正在下载 &8[&7{file.Link}&8]");
                    if (!Directory.Exists(LocalPath)) {
                        Directory.CreateDirectory(LocalPath);
                    }
                    FileDownloader.DownloadFile(file.Link,Path.Combine(LocalPath,file.Sha1));
                }
                File.Copy(Path.Combine(LocalPath,file.Sha1),destPath);
            } else {
                ct?.WriteLine($"&7正在下载 &8[&7{file.Link}&8]");
                FileDownloader.DownloadFile(file.Link,destPath);
            }
            if (destroySource) File.Delete(filePath);
        }
        ct?.WriteLine("&7完成!");
    }
}