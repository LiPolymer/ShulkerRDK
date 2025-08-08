using System.Text.Json;
using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Shared;

public class NetworkFile {
    public required string? Sha1 { get; set; }
    public required string Link { get; set; }

    public static void Create(string path, string url, IChainedLikeTerminal? ct = null) {
        const string cache = "./shulker/local/cache/netFile.analyzing";
        string opath = $"{path}.nfm";
        FileDownloader.DownloadFile(url,cache);
        string sha1 = Tools.GetSha1(cache);
        File.Delete(cache);
        Tools.WriteAllText(opath,JsonSerializer.Serialize(new NetworkFile {
            Sha1 = sha1,
            Link = url
        },Tools.JsonSerializerOptions));
    }

    public static void Restore(string input,string output, IChainedLikeTerminal? ct = null,bool destroySource = false) {
        string[] files = Directory.GetFiles(input,"*.mrf",SearchOption.AllDirectories);
        ct?.WriteLine($"&7正在复原&8[&7{input}&8]");
        foreach (string file in files) {
            string relativePath = Path.GetRelativePath(input,file);
            string destPath = Path.Combine(output,relativePath);
            destPath = Path.ChangeExtension(destPath,"");
            //todo:完成复原逻辑
            if (destroySource) File.Delete(file);
        }
        ct?.WriteLine("&7完成!");
    }
}