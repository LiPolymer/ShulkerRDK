using System.Text.Json;
using Modrinth;
using ShulkerRDK.Shared;
using Version = Modrinth.Models.Version;

namespace ShulkerRDK.Modrinth;

public class Manager {
    static readonly Manager Instance = new Manager();
    readonly ModrinthClient _client = new ModrinthClient();
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        LevitateLogger ct = ec.Logger;
        ct.AddNode("&aModrinth");
        bool destroySource = true;
        if (!Tools.TryGetSub(["r","s"],args,1,ct)) return null;
        if (!Tools.CheckParamLength(args,2,ct)) return null;
        string to = args[2];
        if (Tools.CheckParamLength(args,3)) {
            to = args[3];
            destroySource = false;
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
}