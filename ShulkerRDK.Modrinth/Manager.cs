using System.ComponentModel;
using System.Text.Json;
using Modrinth;
using Modrinth.Models;
using Modrinth.Models.Enums;
using ShulkerRDK.Shared;
using File = System.IO.File;
using Version = Modrinth.Models.Version;

namespace ShulkerRDK.Modrinth;

public class Manager {
    public static readonly Manager Instance = new Manager();
    public static ShulkerContext? Context;
    readonly ModrinthClient _client = new ModrinthClient();
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        LevitateLogger ct = ec.Logger;
        ct.AddNode("&aModrinth");
        bool destroySource = true;
        if (!Tools.TryGetSub(["r","s","e"],args,1,ct)) return null;
        if (!Tools.CheckParamLength(args,2,ct)) return null;
        string to = args[2];
        if (Tools.CheckParamLength(args,3)) {
            to = args[3];
            destroySource = false;
        } else if (args[1] == "e") {
            destroySource = false;
            if (!Tools.CheckParamLength(args,3,ct)) return null;
        }
        if (Tools.CheckParamLength(args,4)) {
            if (args[4] == "true") {
                destroySource = true;
            }
        }
        TransitionLayer(args[1],args[2],to,destroySource,ct);
        return null;
    }

    [Description("Modrinth平台操作")]
    public static void Command(string[] args,ShulkerContext shulkerContext) {
        ChainedTerminal ct = new ChainedTerminal("&aModrinth");
        if (!Tools.TryGetSub(["restore","serialize","clean","r","s"],args,1,ct)) return;
        string from = Tools.CheckParamLength(args,2) ? args[2] : shulkerContext.ProjectConfig!.RootPath;
        bool isOutMissing = !Tools.CheckParamLength(args,2);
        string to = !isOutMissing ? args[3] : from;
        TransitionLayer(args[1],from,to,isOutMissing,ct);
    }

    static void TransitionLayer(string act,string from,string to,bool destroySource,IChainedLikeTerminal ct) {
        switch (act) {
            case "r" or "restore":
                Instance.Restore(from,to,ct,destroySource);
                break;
            case "s" or "serialize":
                Instance.Serialize(from,to,ct,destroySource);
                break;
            case "e" or "export":
                Instance.Indexer("./shulker/mrpack.template.json",from,to,ct,destroySource);
                break;
            case "clean":
                Cleanup(ct);
                break;
        }
    }

    static void Cleanup(IChainedLikeTerminal? ct = null) {
        ct?.WriteLine("&7正在清理缓存文件...");
        if (Directory.Exists(LocalPath)) Directory.Delete(LocalPath,true);
        ct?.WriteLine("&7完成!");
    }
    void Serialize(string input,string output,IChainedLikeTerminal? ct = null,bool destroySource = false) {
        string[] files = Directory.GetFiles(input,"*",SearchOption.AllDirectories);
        ct?.WriteLine($"&7正在编入&8[&7{input}&8]");
        Dictionary<string,List<string>> reverseMap = [];
        foreach (string file in files) {
            string sha1 = Tools.GetSha1(file);
            if (reverseMap.TryGetValue(sha1,out List<string>? value)) {
                ct?.WriteLine($"&7链接文件&8[&7{file}&8]&7>>&8[&7{sha1}&8]",Terminal.MessageType.Debug);
                value.Add(file);
            } else {
                ct?.WriteLine($"&7创建表项&8[&7{file}&8]&7>>&8[&7{sha1}&8]",Terminal.MessageType.Debug);
                reverseMap.Add(sha1,[file]);
            }
        }
        ct?.WriteLine($"&7正在与Modrinth通讯... &o&8[{reverseMap.Count}]个文件");
        Task<IDictionary<string,Version>> task = _client.VersionFile.GetMultipleVersionsByHashAsync(reverseMap.Keys.ToArray());
        task.Wait();
        foreach (KeyValuePair<string,Version> rawResult in task.Result) {
            foreach (string target in reverseMap[rawResult.Key]) {
                ct?.WriteLine($"{rawResult.Value.Name} &7{rawResult.Value.ProjectId}@{rawResult.Value.VersionNumber} &8{target}",
                              Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(input,target);
                string destPath = Path.Combine(output,relativePath);
                destPath = $"{destPath}.mrf";
                Tools.WriteAllText(destPath,JsonSerializer.Serialize(new MrHostedFile {
                    Sha1 = rawResult.Key,
                    VersionId = rawResult.Value.Id
                },Tools.JsonSerializerOptions));
                ManagedFileImport(target,rawResult.Key);
                if (destroySource) File.Delete(target);
            }
        }
        ct?.WriteLine("&7完成!");
    }
    void Restore(string input,string output,IChainedLikeTerminal? ct = null,bool destroySource = false) {
        string[] files = Directory.GetFiles(input,"*.mrf",SearchOption.AllDirectories);
        ct?.WriteLine($"&7正在复原&8[&7{input}&8]");
        foreach (string file in files) {
            string relativePath = Path.GetRelativePath(input,file);
            string destPath = Path.Combine(output,relativePath);
            destPath = Path.ChangeExtension(destPath,"");
            ManagedFileExport(file,destPath,true,ct);
            if (destroySource) File.Delete(file);
        }
        ct?.WriteLine("&7完成!");
    }

    const string LocalPath = "./shulker/local/mrf/";
    static void ManagedFileImport(string input,string index,bool overwrite = false) {
        if (!Directory.Exists(LocalPath)) Directory.CreateDirectory(LocalPath);
        if (File.Exists(Path.Combine(LocalPath,index)) & !overwrite) return;
        File.Copy(input,Path.Combine(LocalPath,index),overwrite);
    }
    void ManagedFileExport(string input,string output,bool overwrite = true,IChainedLikeTerminal? ct = null) {
        if (!Directory.Exists(LocalPath)) Directory.CreateDirectory(LocalPath);
        if (!Directory.Exists(Path.GetDirectoryName(output))) Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        MrHostedFile mrf = JsonSerializer.Deserialize<MrHostedFile>(File.ReadAllText(input))!;
        if (File.Exists(output) & !overwrite) return;
        ct?.WriteLine($"&7正在补全 &8V_&7{mrf.VersionId}&8[{mrf.Sha1}]",Terminal.MessageType.Debug);
        if (!File.Exists(Path.Combine(LocalPath,mrf.Sha1))) {
            Task<Version> getTask = _client.Version.GetAsync(mrf.VersionId);
            getTask.Wait();
            foreach (global::Modrinth.Models.File file in getTask.Result.Files) {
                if (file.Hashes.Sha1 != mrf.Sha1) continue;
                ct?.WriteLine($"&7正在下载 &8P_&7{getTask.Result.ProjectId}&8@V_&7{getTask.Result.Id}&8(&7{getTask.Result.Name}&8)");
                FileDownloader.DownloadFile(file.Url,Path.Combine(LocalPath,mrf.Sha1));
                break;
            }
        }
        File.Copy(Path.Combine(LocalPath,mrf.Sha1),output,overwrite);
    }
    public void Indexer(string basement,string input,string output,IChainedLikeTerminal? ct = null,bool destroySource = false) {
        if (!File.Exists(basement)) {
            new MrPack {
                Dependencies = [],
                VersionId = "0.0.0",
                Name = Context!.ProjectConfig!.ProjectName,
                Description = "ShulkerRDK Generated Basement Template"
            }.Export(basement);
            ct?.WriteLine($"&7未找到mrpack元数据基底文件,已自动创建,请打开编辑后再次执行打包&8[&7{basement}&8]",Terminal.MessageType.Error);
            return;
        }

        string[] files = Directory.GetFiles(input,"*.mrf",SearchOption.AllDirectories);
        ct?.WriteLine($"&7正在编制mrpack索引&8[&7{input}&8]");
        Dictionary<string,string> map = [];
        Dictionary<string,MrHostedFile> mrfMap = [];
        foreach (string file in files) {
            string destPath = Path.GetRelativePath(input,file)[..^4];
            MrHostedFile mrf = JsonSerializer.Deserialize<MrHostedFile>(File.ReadAllText(file))!;
            ct?.WriteLine($"&7创建表项&8[&7{destPath}&8]&7>>&8[&7{mrf.Sha1}&8]",Terminal.MessageType.Debug);
            map.Add(destPath,mrf.Sha1);
            mrfMap.Add(mrf.Sha1,mrf);
            if (destroySource) File.Delete(file);
        }
        ct?.WriteLine($"&7正在向Modrinth请求版本信息... &o&8[{map.Count}]个文件");
        Task<IDictionary<string,Version>> verTask = _client.VersionFile.GetMultipleVersionsByHashAsync(map.Values.Distinct().ToArray());
        verTask.Wait();
        IDictionary<string,Version> verResult = verTask.Result;

        List<string> versions = [];
        foreach (KeyValuePair<string,Version> pair in verResult) {
            if (versions.Contains(pair.Value.ProjectId)) continue;
            versions.Add(pair.Value.ProjectId);
        }
        ct?.WriteLine($"&7正在向Modrinth请求项目信息... &o&8[{versions.Count}]个项目");
        Task<Project[]> projTask = _client.Project.GetMultipleAsync(versions.ToArray());
        projTask.Wait();
        Dictionary<string,Project> projResult = [];
        foreach (Project p in projTask.Result) {
            projResult.Add(p.Id,p);
        }

        MrPack mrpack = MrPack.Load(basement);
        foreach (KeyValuePair<string,string> t in map) {
            Version mrVer = verResult[t.Value];
            global::Modrinth.Models.File? file = null;
            foreach (global::Modrinth.Models.File f in mrVer.Files) {
                if (f.Hashes.Sha1 != t.Value) continue;
                file = f;
            }
            if (file == null) {
                ct?.WriteLine("&c未找到对应文件",Terminal.MessageType.Error);
                continue;
            }
            MrPack.FileObject.EnvTable env = new MrPack.FileObject.EnvTable {
                Client = mrfMap[file.Hashes.Sha1].ClientSide switch {
                    null => SidesMerger(projResult[mrVer.ProjectId].ServerSide,projResult[mrVer.ProjectId].ClientSide),
                    _ => SideToStringConverter(mrfMap[file.Hashes.Sha1].ClientSide!.Value,false)
                },
                Server = mrfMap[file.Hashes.Sha1].ServerSide switch {
                    null => SideToStringConverter(projResult[mrVer.ProjectId].ServerSide),
                    _ => SideToStringConverter(mrfMap[file.Hashes.Sha1].ServerSide!.Value,false)
                }
            };
            mrpack.Files.Add(new MrPack.FileObject {
                Path = t.Key.Replace('\\','/'),
                Hashes = new MrPack.FileObject.HashesTable {
                    Sha1 = file.Hashes.Sha1,
                    Sha512 = file.Hashes.Sha512
                },
                Envs = env,
                Downloads = [file.Url],
                FileSize = file.Size
            });
        }

        ct?.WriteLine($"&8 {mrpack.Files.Count} Objs Parsed",Terminal.MessageType.Debug);
        mrpack.Export(output);
        ct?.WriteLine("&a索引编制完成!");
    }

    static string SideToStringConverter(Side side, bool isMandatoryMode = true) {
        return side switch {
            Side.Required => "required",
            Side.Optional => isMandatoryMode ? "required" :  "optional",
            Side.Unsupported => "unsupported",
            Side.Unknown => "required",
            _ => throw new ArgumentOutOfRangeException(nameof(side),side,null)
        };
    }

    static string SidesMerger(Side serverSide,Side clientSide) {
        return clientSide switch {
            Side.Required => "required",
            Side.Optional => "required",
            Side.Unsupported => SideToStringConverter(serverSide),
            Side.Unknown => "required",
            _ => throw new ArgumentOutOfRangeException(nameof(clientSide),clientSide,null)
        };
    }

    // Levitate方法入口：处理本地文件到overrides目录
    public static string? OverridesMethod(string[] args, LevitateExecutionContext ec) {
        LevitateLogger logger = ec.Logger;
        logger.AddNode("&aOverrides");
        if (!Tools.CheckParamLength(args, 1, logger)) return null;
        if (!Tools.CheckParamLength(args, 2, logger)) return null;
        string input = args[1];
        string output = args[2];
        PrepareOverrides(input, output, logger);
        return null;
    }

    // 命令入口：处理本地文件到overrides目录
    [Description("处理本地文件到overrides目录")]
    public static void OverridesCommand(string[] args, ShulkerContext shulkerContext) {
        ChainedTerminal logger = new ChainedTerminal("&aOverrides");
        string input = Tools.CheckParamLength(args, 1) ? args[1] : shulkerContext.ProjectConfig!.RootPath;
        string output = Tools.CheckParamLength(args, 2) ? args[2] : "./shulker/cache";
        PrepareOverrides(input, output, logger);
    }

    // 处理本地文件到overrides目录
    static void PrepareOverrides(string input, string output, IChainedLikeTerminal logger) {
        logger.WriteLine($"&7正在处理本地文件&8[&7{input}&8]&7>>&8[&7{output}&8]");

        // overrides目录（客户端和服务端通用）
        Dictionary<string, string> overridesDirs = new Dictionary<string, string> {
            { "config", "overrides/config" }
        };

        // client-overrides目录（仅客户端）
        Dictionary<string, string> clientOverridesDirs = new Dictionary<string, string> {
            { "resourcepacks", "client-overrides/resourcepacks" },
            { "shaderpacks", "client-overrides/shaderpacks" }
        };

        // 处理overrides目录
        foreach (KeyValuePair<string, string> dir in overridesDirs) {
            string srcDir = Path.Combine(input, dir.Key);
            if (Directory.Exists(srcDir)) {
                string destDir = Path.Combine(output, dir.Value);
                CopyDirectory(srcDir, destDir, logger, null);
            }
        }

        // 处理client-overrides目录
        foreach (KeyValuePair<string, string> dir in clientOverridesDirs) {
            string srcDir = Path.Combine(input, dir.Key);
            if (Directory.Exists(srcDir)) {
                string destDir = Path.Combine(output, dir.Value);
                CopyDirectory(srcDir, destDir, logger, null);
            }
        }

        // 处理mods目录中的本地文件（非.mrf文件）
        string modsDir = Path.Combine(input, "mods");
        if (Directory.Exists(modsDir)) {
            string destModsDir = Path.Combine(output, "overrides/mods");
            CopyDirectory(modsDir, destModsDir, logger, ".mrf"); // 过滤.mrf文件
        }

        logger.WriteLine("&a完成!");
    }

    // 复制目录，可选过滤特定扩展名
    static void CopyDirectory(string srcDir, string destDir, IChainedLikeTerminal logger, string? filterExtension) {
        if (!Directory.Exists(destDir)) {
            Directory.CreateDirectory(destDir);
        }

        string[] files = Directory.GetFiles(srcDir, "*", SearchOption.AllDirectories);
        foreach (string file in files) {
            // 过滤指定扩展名的文件
            if (filterExtension != null && Path.GetExtension(file) == filterExtension) {
                logger.WriteLine($"&7跳过托管文件&8[&7{Path.GetFileName(file)}&8]", Terminal.MessageType.Debug);
                continue;
            }

            string relativePath = Path.GetRelativePath(srcDir, file);
            string destPath = Path.Combine(destDir, relativePath);
            string? destDirectory = Path.GetDirectoryName(destPath);
            if (!Directory.Exists(destDirectory)) {
                Directory.CreateDirectory(destDirectory!);
            }

            logger.WriteLine($"&7复制&8[&7{relativePath}&8]", Terminal.MessageType.Debug);
            File.Copy(file, destPath, true);
        }
    }
}