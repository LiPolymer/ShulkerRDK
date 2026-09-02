using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;

namespace ShulkerRDK.Shared;

public class InstalledExtensionMeta {
    public required string Identifier { get; set; }
    public string? Sha256 { get; set; }
    public string InstalledAt { get; set; } = "";
}

public static class ExtensionInstaller {
    const string CacheDir = "./shulker/local/cache/extensions";
    const string MetaFile = "lock.json";

    public static void EnsureInstalled(ProjectConfig config,bool progressEnabled = true) {
        if (!Directory.Exists(StaticContext.Paths.ExtensionsPath)) {
            Directory.CreateDirectory(StaticContext.Paths.ExtensionsPath);
        }
        if (config.Extensions.Count > 0) {
            Terminal.WriteLine("&l&bExtension","&7正在解析声明式扩展...");
        }
        PruneUndeclared(config);
        foreach (string identifier in config.Extensions) {
            try {
                EnsureOne(identifier,progressEnabled);
            }
            catch (Exception e) {
                Tools.DisplayException(e,new ChainedTerminal("&l&bExtension"),Terminal.MessageType.Error);
            }
        }
    }

    static void EnsureOne(string identifier,bool progressEnabled) {
        ExtensionSource source;
        try {
            source = ExtensionSource.Parse(identifier);
        }
        catch (FormatException e) {
            Terminal.WriteLine("&l&bExtension",e.Message,Terminal.MessageType.Warn);
            return;
        }
        string target = Path.Combine(StaticContext.Paths.ExtensionsPath,source.Asm);
        InstalledExtensionMeta? meta = ReadMeta(target);
        if (meta != null) {
            if (meta.Identifier == source.Identifier) {
                return;
            }
        } else if (Directory.Exists(target)) {
            Terminal.WriteLine("&l&bExtension",
                               $"&e检测到手动安装的插件包&8[&7{source.Asm}&8]&e,已跳过声明&8[&7{identifier}&8]",Terminal.MessageType.Warn);
            return;
        }

        Terminal.WriteLine("&l&bExtension",$"&7正在获取&8[&7{source.Asm}@{source.Tag}&8]&7源&8[&7{ExtensionSource.PlatformHosts[source.Platform]}/{source.Repo}&8]");
        ReleaseInfo? release = ReleaseSource.Resolve(source.Platform).FetchRelease(source.Repo,source.Tag);
        if (release == null) {
            Terminal.WriteLine("&l&bExtension",
                               $"&c无法获取发布信息&8[&7{source.Asm}@{source.Tag}&8]&c,请检查网络连接或版本钉子是否有效",Terminal.MessageType.Warn);
            return;
        }

        ReleaseAssetInfo? asset = release.Assets.FirstOrDefault(a =>
            a.Name.Equals(source.Asm + ".zip",StringComparison.OrdinalIgnoreCase));
        if (asset == null) {
            string names = string.Join("&8, &7",release.Assets.Select(a => a.Name));
            Terminal.WriteLine("&l&bExtension",
                               $"&c发布&8[&7{source.Tag}&8]&c中未找到资产&8[&7{source.Asm}.zip&8],可用&8[&7{names}&8]",Terminal.MessageType.Warn);
            return;
        }

        if (!Directory.Exists(CacheDir)) {
            Directory.CreateDirectory(CacheDir);
        }
        string zipPath = Path.Combine(CacheDir,source.Asm + ".zip");
        FileDownloader.DownloadFile(asset.Url,zipPath,progressEnabled);
        if (!File.Exists(zipPath)) {
            Terminal.WriteLine("&l&bExtension",$"&c下载失败&8[&7{asset.Url}&8]",Terminal.MessageType.Warn);
            return;
        }

        string sha256;
        using (FileStream zipStream = File.OpenRead(zipPath)) {
            sha256 = Convert.ToHexString(SHA256.HashData(zipStream));
        }
        if (asset.Sha256 != null && !string.Equals(asset.Sha256,sha256,StringComparison.OrdinalIgnoreCase)) {
            File.Delete(zipPath);
            Terminal.WriteLine("&l&bExtension",
                               $"&c校验和不匹配&8[&7{source.Asm}.zip&8]&c,已放弃安装&8(&7期望&8[&7{asset.Sha256}&8]&7实际&8[&7{sha256}&8]&c)",Terminal.MessageType.Error);
            return;
        }

        string staging = Path.Combine(CacheDir,source.Asm + ".staging");
        if (Directory.Exists(staging)) {
            Directory.Delete(staging,true);
        }
        ZipFile.ExtractToDirectory(zipPath,staging);
        string root = staging;
        string[] topDirs = Directory.GetDirectories(staging);
        if (Directory.GetFiles(staging).Length == 0 && topDirs.Length == 1) {
            root = topDirs[0];
        }
        if (!File.Exists(Path.Combine(root,source.Asm + ".dll"))) {
            Directory.Delete(staging,true);
            File.Delete(zipPath);
            Terminal.WriteLine("&l&bExtension",
                               $"&c插件包中缺少入口程序集&8[&7{source.Asm}.dll&8]&c,已放弃安装",Terminal.MessageType.Warn);
            return;
        }

        if (Directory.Exists(target)) {
            Directory.Delete(target,true);
        }
        Directory.Move(root,target);
        File.Delete(zipPath);

        Tools.WriteAllText(Path.Combine(target,MetaFile),JsonSerializer.Serialize(new InstalledExtensionMeta {
            Identifier = source.Identifier,
            Sha256 = asset.Sha256 ?? sha256,
            InstalledAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        },Tools.JsonSerializerOptions));
        Terminal.WriteLine("&l&bExtension",$"&8[&7{source.Asm}@{source.Tag}&8]&a安装完成!");
    }

    static void PruneUndeclared(ProjectConfig config) {
        HashSet<string> declaredAsms = [];
        foreach (string identifier in config.Extensions) {
            try {
                declaredAsms.Add(ExtensionSource.Parse(identifier).Asm);
            }
            catch (FormatException) {
                //ignore
            }
        }
        if (!Directory.Exists(StaticContext.Paths.ExtensionsPath)) return;
        foreach (string directory in Directory.GetDirectories(StaticContext.Paths.ExtensionsPath)) {
            string asm = Path.GetFileName(directory);
            if (declaredAsms.Contains(asm)) continue;
            if (!File.Exists(Path.Combine(directory,MetaFile))) continue;
            InstalledExtensionMeta? meta = ReadMeta(directory);
            if (meta == null) {
                Terminal.WriteLine("&l&bExtension",
                                   $"&e插件包元数据损坏&8[&7{asm}&8]&e,已跳过清除,请手动处理",Terminal.MessageType.Warn);
                continue;
            }
            Directory.Delete(directory,true);
            Terminal.WriteLine("&l&bExtension",$"&7已移除未声明的插件包&8[&7{asm}&8]",Terminal.MessageType.Warn);
        }
    }

    static InstalledExtensionMeta? ReadMeta(string target) {
        string metaPath = Path.Combine(target,MetaFile);
        if (!File.Exists(metaPath)) return null;
        try {
            return JsonSerializer.Deserialize<InstalledExtensionMeta>(File.ReadAllText(metaPath),Tools.JsonSerializerOptions);
        }
        catch {
            return null;
        }
    }
}
