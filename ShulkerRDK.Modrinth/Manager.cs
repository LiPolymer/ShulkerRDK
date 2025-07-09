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

    public static void Command(string[] args,ShulkerContext shulkerContext) {
        ChainedTerminal ct = new ChainedTerminal("&aModrinth");
        if (!Tools.TryGetSub(["r","s","clean"],args,1,ct)) return;
        string from = Tools.CheckParamLength(args,2) ? args[2] : shulkerContext.ProjectConfig!.RootPath;
        bool isOutMissing = !Tools.CheckParamLength(args,2);
        string to = !isOutMissing ? args[3] : from;
        TransitionLayer(args[1],from,to,isOutMissing,ct);
    }

    static void TransitionLayer(string act,string from,string to,bool destroySource,IChainedLikeTerminal ct) {
        switch (act) {
            case "r":
                Instance.Restore(from,to,ct,destroySource);
                break;
            case "s":
                Instance.Serialize(from,to,ct,destroySource);
                break;
            case "e":
                Instance.Indexer("./shulker/mrpack.template.json",from,to,ct,destroySource);
                break;
            case "clean":
                Cleanup(ct);
                break;
        }
    }

    static void Cleanup(IChainedLikeTerminal? ct = null) {
        ct?.WriteLine("正在清理缓存文件...");
        if (Directory.Exists(LocalPath)) Directory.Delete(LocalPath,true);
        ct?.WriteLine("完成!");
    }
    void Serialize(string input,string output,IChainedLikeTerminal? ct = null,bool destroySource = false) {
        string[] files = Directory.GetFiles(input,"*",SearchOption.AllDirectories);
        ct?.WriteLine($"正在编入[{input}]");
        Dictionary<string,List<string>> reverseMap = [];
        foreach (string file in files) {
            string sha1 = Utils.GetSha1(file);
            if (reverseMap.TryGetValue(sha1,out List<string>? value)) {
                ct?.WriteLine($"链接文件&8[&7{file}&8]&7>>&8[&7{sha1}&8]",Terminal.MessageType.Debug);
                value.Add(file);
            } else {
                ct?.WriteLine($"创建表项&8[&7{file}&8]&7>>&8[&7{sha1}&8]",Terminal.MessageType.Debug);
                reverseMap.Add(sha1,[file]);
            }
        }
        ct?.WriteLine($"正在与Modrinth通讯... &o&8[{reverseMap.Count}]个文件");
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
                    VersionId = rawResult.Value.Id,
                },Tools.JsonSerializerOptions));
                ManagedFileImport(target,rawResult.Key);
                if (destroySource) File.Delete(target);
            }
        }
        ct?.WriteLine("完成!");
    }
    void Restore(string input,string output,IChainedLikeTerminal? ct = null,bool destroySource = false) {
        string[] files = Directory.GetFiles(input,"*.mrf",SearchOption.AllDirectories);
        ct?.WriteLine($"正在复原[{input}]");
        foreach (string file in files) {
            string relativePath = Path.GetRelativePath(input,file);
            string destPath = Path.Combine(output,relativePath);
            destPath = Path.ChangeExtension(destPath,"");
            ManagedFileExport(file,destPath,true,ct);
            if (destroySource) File.Delete(file);
        }
        ct?.WriteLine("完成!");
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
        ct?.WriteLine($"正在补全&8V_&7{mrf.VersionId}&8[{mrf.Sha1}]",Terminal.MessageType.Debug);
        if (!File.Exists(Path.Combine(LocalPath,mrf.Sha1))) {
            Task<Version> getTask = _client.Version.GetAsync(mrf.VersionId);
            getTask.Wait();
            foreach (global::Modrinth.Models.File file in getTask.Result.Files) {
                if (file.Hashes.Sha1 != mrf.Sha1) continue;
                ct?.WriteLine($"正在下载&8P_&7{getTask.Result.ProjectId}&8@V_&7{getTask.Result.Id}&8(&7{getTask.Result.Name}&8)");
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
            ct?.WriteLine("<UNK>");
            return;
        }
        
        string[] files = Directory.GetFiles(input,"*.mrf",SearchOption.AllDirectories);
        ct?.WriteLine($"正在编制mrpack索引&8[&7{input}&8]");
        Dictionary<string,string> map = [];
        foreach (string file in files) {
            string destPath = Path.GetRelativePath(input,file)[..^4];
            MrHostedFile mrf = JsonSerializer.Deserialize<MrHostedFile>(File.ReadAllText(file))!;
            ct?.WriteLine($"创建表项&8[&7{destPath}&8]&7>>&8[&7{mrf.Sha1}&8]",Terminal.MessageType.Debug);
            map.Add(destPath,mrf.Sha1);
            if (destroySource) File.Delete(file);
        }
        ct?.WriteLine($"正在向Modrinth请求版本信息... &o&8[{map.Count}]个文件");
        Task<IDictionary<string,Version>> verTask = _client.VersionFile.GetMultipleVersionsByHashAsync(map.Values.Distinct().ToArray());
        verTask.Wait();
        IDictionary<string,Version> verResult = verTask.Result;
        
        List<string> versions = [];
        foreach (KeyValuePair<string,Version> pair in verResult) {
            if (versions.Contains(pair.Value.ProjectId)) continue;
            versions.Add(pair.Value.ProjectId);
        }
        ct?.WriteLine($"正在向Modrinth请求项目信息... &o&8[{versions.Count}]个项目");
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
                ct?.WriteLine("<UNK>",Terminal.MessageType.Error);
                continue;
            }
            mrpack.Files.Add(new MrPack.FileObject {
                Path = t.Key.Replace('\\','/'),
                Hashes = new MrPack.FileObject.HashesTable {
                    Sha1 = file.Hashes.Sha1,
                    Sha512 = file.Hashes.Sha512
                },
                Envs = new MrPack.FileObject.EnvTable {
                    Client = SidesMerger(projResult[mrVer.ProjectId].ServerSide,projResult[mrVer.ProjectId].ClientSide),
                    Server = SideToStringConverter(projResult[mrVer.ProjectId].ServerSide)
                },
                Downloads = [file.Url],
                FileSize = file.Size
            });
        }
        
        ct?.WriteLine($"{mrpack.Files.Count} Objs Parsed",Terminal.MessageType.Debug);
        mrpack.Export(output);
        ct?.WriteLine("&a索引编制完成!");
    }

    static string SideToStringConverter(Side side) {
        return side switch {
            Side.Required => "required",
            Side.Optional => "optional",
            Side.Unsupported => "unsupported",
            Side.Unknown => "required",
            _ => throw new ArgumentOutOfRangeException(nameof(side),side,null)
        };
    }

    static string SidesMerger(Side serverSide,Side clientSide) {
        return clientSide switch {
            Side.Required => "required",
            Side.Optional => "optional",
            Side.Unsupported => SideToStringConverter(serverSide),
            Side.Unknown => "required",
            _ => throw new ArgumentOutOfRangeException(nameof(clientSide),clientSide,null)
        };
    }
}