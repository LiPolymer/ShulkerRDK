using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ShulkerRDK.Shared;

public static class NugetHelper {
    public static bool IsProgressBarEnabled { get; set; } = true;
    public static void DependencyVerify(string packageIdentifier,string libTarget = "net8.0",bool extractRuntime = true,string[]? checkFiles = null) {
        try {
            checkFiles ??= [packageIdentifier.Split('/')[0] + ".dll"];
            bool passed = true;
            foreach (string checkpoint in checkFiles) {
                if (checkpoint.Contains('|')) {
                    bool subPassed = checkpoint.Split("|").Any(subPoint => File.Exists(Path.Combine(StaticContext.Paths.LibsPath,subPoint)));
                    passed = subPassed;
                } else if (!File.Exists(Path.Combine(StaticContext.Paths.LibsPath,checkpoint))) {
                    passed = false;
                }
            }
            if (passed) {
                if (DependencyMetadata.Check(packageIdentifier)) {
                    return;
                }
            }
            if (!Directory.Exists(StaticContext.Paths.LibsPath)) {
                Directory.CreateDirectory(StaticContext.Paths.LibsPath);
            }
            ChainedTerminal logger = new ChainedTerminal("&9Nuget");
            logger.WriteLine($"&7正在获取包&8[&7{packageIdentifier}&8]");
            const string cache = "./shulker/local/cache/";
            string pkgCache = $"{cache}{packageIdentifier.Split('/')[0]}.nupkg";
            string extCache = $"{cache}{packageIdentifier.Split('/')[0]}/";
            FileDownloader.DownloadFile($"https://www.nuget.org/api/v2/package/{packageIdentifier}",pkgCache,IsProgressBarEnabled);
            if (Directory.Exists(extCache)) {
                Directory.Delete(extCache,true);
            }
            logger.WriteLine("&7正在安装...");
            ZipFile.ExtractToDirectory(pkgCache,extCache);
            string[] files = Directory.GetFiles(Path.Combine(extCache,$"lib/{libTarget}/"),"*.dll",SearchOption.TopDirectoryOnly);
            foreach (string file in files) {
                File.Copy(file,Path.Combine(StaticContext.Paths.LibsPath,Path.GetFileName(file)),true);
            }
            if (extractRuntime) {
                if (Directory.Exists(Path.Combine(extCache,$"runtimes/"))) {
                    // 获取运行平台
                    string? platform = null;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        platform = "win";
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                        platform = "linux";
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                        platform = "osx";
                    }
                    if (platform == null) throw new Exception("未知平台");

                    // 获取设备架构
                    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                    string arch = RuntimeInformation.ProcessArchitecture switch {
                        Architecture.X86 => "x86",
                        Architecture.X64 => "x64",
                        Architecture.Arm64 => "arm64",
                        _ => throw new Exception("未知架构")
                    };

                    string[] runtimes = Directory.GetFiles(Path.Combine(extCache,$"runtimes/{platform}-{arch}/native/"),"*.dll*",
                                                           SearchOption.TopDirectoryOnly);
                    foreach (string runtime in runtimes) {
                        File.Copy(runtime,Path.Combine(StaticContext.Paths.LibsPath,Path.GetFileName(runtime)),true);
                    }
                }
            }
            logger.WriteLine("&e依赖安装完成!");
            File.Delete(pkgCache);
            Directory.Delete(extCache,true);
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
    }

    public class DependencyMetadata {
        public static bool Check(string identifier) {
            DependencyMetadata dm = Get(identifier);
            return dm.Version == identifier.Split('/')[1];
        }

        public static void Overwrite(string identifier) {
            string path = Path.Combine(StaticContext.Paths.LibsPath,identifier.Split('/')[0] + ".depMeta.json");
            Tools.WriteAllText(path,JsonSerializer.Serialize(new DependencyMetadata {
                Version = identifier.Split('/')[1]
            },Tools.JsonSerializerOptions));
        }

        public static DependencyMetadata Get(string identifier) {
            string path = Path.Combine(StaticContext.Paths.LibsPath,identifier.Split('/')[0] + ".depMeta.json");
            if (!File.Exists(path)) {
                Tools.WriteAllText(path,JsonSerializer.Serialize(new DependencyMetadata {
                    Version = identifier.Split('/')[1]
                },Tools.JsonSerializerOptions));
            }
            DependencyMetadata meta = JsonSerializer.Deserialize<DependencyMetadata>(File.ReadAllText(path))!;
            meta.Name = identifier.Split('/')[0];
            return meta;
        }

        public void Write() {
            string path = Path.Combine(StaticContext.Paths.LibsPath,Name + ".dll.json");
            Tools.WriteAllText(path,JsonSerializer.Serialize(this,Tools.JsonSerializerOptions));
        }

        [JsonIgnore]
        public string Name = "";

        public required string Version { get; set; }
        // todo:实现平台记录与验证
        public string Platform { get; set; } = "";
    }
}
