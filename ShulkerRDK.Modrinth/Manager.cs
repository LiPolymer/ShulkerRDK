using System.ComponentModel;
using System.Text.Json;
using Modrinth;
using Modrinth.Models;
using Modrinth.Models.Enums;
using Modrinth.Models.Enums.Project;
using ShulkerRDK.Shared;
using File = System.IO.File;
using Version = Modrinth.Models.Version;

namespace ShulkerRDK.Modrinth;

public class Manager {
    static readonly Manager Instance = new Manager();
    public static ShulkerContext? Context;
    readonly ModrinthClient _client = new ModrinthClient();
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        LevitateLogger ct = ec.Logger;
        ct.AddNode("&aModrinth");
        bool destroySource = true;
        if (!Tools.TryGetSub(["r","s","e","a","u","add","update","export","lock","unlock"],args,1,ct)) return null;
        switch (args[1]) {
            case "a" or "add":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                if (args[2] == "-f") {
                    if (!Tools.CheckParamLength(args,3,ct)) return null;
                    string? levBatchOutputDir = Tools.CheckParamLength(args,4) ? args[4] : null;
                    Instance.AddBatch(args[3],levBatchOutputDir,ct);
                } else {
                    string? levAddVersion = Tools.CheckParamLength(args,3) ? args[3] : null;
                    string? levAddOutputDir = Tools.CheckParamLength(args,4) ? args[4] : null;
                    Instance.Add(args[2],levAddVersion,levAddOutputDir,ct);
                }
                return null;
            case "u" or "update":
                string updateDir = Tools.CheckParamLength(args,2) ? args[2] : ".";
                Instance.Update(updateDir,ct);
                return null;
            case "lock":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                Instance.SetLock(args[2],true,ct);
                return null;
            case "unlock":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                Instance.SetLock(args[2],false,ct);
                return null;
        }
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
        if (!Tools.TryGetSub(["restore","serialize","clean","export","add","update","lock","unlock","r","s","e","a","u"],args,1,ct)) return;
        switch (args[1]) {
            case "a" or "add":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                if (args[2] == "-f") {
                    if (!Tools.CheckParamLength(args,3,ct)) return;
                    string? batchOutputDir = Tools.CheckParamLength(args,4) ? args[4] : null;
                    Instance.AddBatch(args[3],batchOutputDir,ct);
                } else {
                    string? addVersion = Tools.CheckParamLength(args,3) ? args[3] : null;
                    string? addOutputDir = Tools.CheckParamLength(args,4) ? args[4] : null;
                    Instance.Add(args[2],addVersion,addOutputDir,ct);
                }
                break;
            case "u" or "update":
                string dir = Tools.CheckParamLength(args,2) ? args[2] : ".";
                Instance.Update(dir,ct);
                break;
            case "lock":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                Instance.SetLock(args[2],true,ct);
                break;
            case "unlock":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                Instance.SetLock(args[2],false,ct);
                break;
            default:
                string from = Tools.CheckParamLength(args,2) ? args[2] : shulkerContext.ProjectConfig!.RootPath;
                bool isOutMissing = !Tools.CheckParamLength(args,2);
                string to = !isOutMissing ? args[3] : from;
                TransitionLayer(args[1],from,to,isOutMissing,ct);
                break;
        }
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

    static (string slugOrId, string? versionId) ParseModrinthInput(string input) {
        if (!input.StartsWith("http",StringComparison.OrdinalIgnoreCase)) return (input,null);
        Uri uri = new Uri(input);
        string[] segments = uri.AbsolutePath.Trim('/').Split('/');
        // https://modrinth.com/{type}/{slug} or https://modrinth.com/{type}/{slug}/version/{vid}
        if (segments.Length < 2) throw new FormatException($"无法解析Modrinth URL: {input}");
        string slug = segments[1];
        string? versionId = null;
        if (segments.Length >= 4 && segments[2] == "version") {
            versionId = segments[3];
        }
        return (slug,versionId);
    }

    static global::Modrinth.Models.File GetPrimaryFile(Version version) {
        return version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.First();
    }

    static string GetProjectSubfolder(Project project) {
        return project.ProjectType switch {
            ProjectType.Mod => "src/mods",
            ProjectType.Resourcepack => "src/resourcepacks",
            ProjectType.Shader => "src/shaderpacks",
            ProjectType.Datapack => "src/datapacks",
            ProjectType.Modpack => "src/modpacks",
            _ => "src/mods"
        };
    }

    static string GetMrfFileName(global::Modrinth.Models.File file) {
        string raw = !string.IsNullOrEmpty(file.FileName)
            ? file.FileName
            : Path.GetFileName(new Uri(file.Url).AbsolutePath);
        return $"{System.Net.WebUtility.UrlDecode(raw)}.mrf";
    }

    static (List<string> loaders, List<string> gameVersions) LoadMrpackDependencies() {
        const string path = "./shulker/mrpack.template.json"; //todo: 支持从外部定义模板路径
        if (!File.Exists(path)) return ([],[]);
        MrPack mrpack = MrPack.Load(path);
        List<string> loaders = [];
        List<string> gameVersions = [];
        foreach (KeyValuePair<string,string> dep in mrpack.Dependencies) {
            if (dep.Key == "minecraft") {
                gameVersions.Add(dep.Value);
            } else if (dep.Key.EndsWith("-loader")) {
                loaders.Add(dep.Key[..^"-loader".Length]);
            } else if (dep.Key is "forge" or "neoforge" or "quilt" or "fabric") {
                loaders.Add(dep.Key);
            }
        }
        return (loaders,gameVersions);
    }

    void Add(string input,string? versionNumber,string? outputDir,IChainedLikeTerminal ct) {
        ct.WriteLine("&aModrinth &7添加资源");
        (string slugOrId,string? versionId) = ParseModrinthInput(input);
        ct.WriteLine($"&7正在获取项目信息 &8[&7{slugOrId}&8]");

        Task<Project> projectTask = _client.Project.GetAsync(slugOrId);
        projectTask.Wait();
        Project project = projectTask.Result;
        ct.WriteLine($"&a{project.Title} &8({project.ProjectType})");

        string destDir = outputDir ?? GetProjectSubfolder(project);
        (List<string> loaders,List<string> gameVersions) = LoadMrpackDependencies();

        Version version;
        if (versionId != null) {
            Task<Version> verTask = _client.Version.GetAsync(versionId);
            verTask.Wait();
            version = verTask.Result;
        } else if (versionNumber != null) {
            Task<Version> verTask = _client.Version.GetByVersionNumberAsync(slugOrId,versionNumber);
            verTask.Wait();
            version = verTask.Result;
        } else {
            bool isMod = project.ProjectType == ProjectType.Mod;
            Task<Version[]> listTask = _client.Version.GetProjectVersionListAsync(
                project.Id,
                isMod && loaders.Count > 0 ? loaders.ToArray() : null,
                isMod && gameVersions.Count > 0 ? gameVersions.ToArray() : null);
            listTask.Wait();
            if (listTask.Result.Length == 0) {
                Task<Version[]> allTask = _client.Version.GetProjectVersionListAsync(project.Id);
                allTask.Wait();
                version = allTask.Result.FirstOrDefault(v => v.ProjectVersionType == ProjectVersionType.Release) ?? allTask.Result.First();
            } else {
                version = listTask.Result.FirstOrDefault(v => v.ProjectVersionType == ProjectVersionType.Release) ?? listTask.Result.First();
            }
        }

        ct?.WriteLine($"&7版本 &a{version.Name} &8({version.VersionNumber})");

        global::Modrinth.Models.File file = GetPrimaryFile(version);
        string cachePath = Path.Combine(LocalPath,file.Hashes.Sha1);
        string displayName = !string.IsNullOrEmpty(file.FileName) ? System.Net.WebUtility.UrlDecode(file.FileName) : file.Hashes.Sha1;
        ct?.WriteLine($"&7正在下载 &8{displayName}");
        FileDownloader.DownloadFile(file.Url,cachePath);

        string mrfPath = Path.Combine(destDir,GetMrfFileName(file));
        Tools.WriteAllText(mrfPath,JsonSerializer.Serialize(new MrHostedFile {
            Sha1 = file.Hashes.Sha1,
            VersionId = version.Id,
            ClientSide = project.ClientSide,
            ServerSide = project.ServerSide
        },Tools.JsonSerializerOptions));
        ct?.WriteLine($"&a已添加 &7{project.Title} &8@&7{version.VersionNumber} &8-> &7{mrfPath}");
    }

    void AddBatch(string filePath,string? outputDir,IChainedLikeTerminal ct) {
        ct.WriteLine($"&aModrinth &7批量添加资源");
        if (!File.Exists(filePath)) {
            ct.WriteLine($"&c文件不存在: {filePath}",Terminal.MessageType.Error);
            return;
        }
        string[] lines = File.ReadAllLines(filePath);
        List<string> entries = [];
        entries.AddRange(lines
                             .Select(line => line.Trim())
                             .Where(trimmed => !string.IsNullOrEmpty(trimmed) 
                                               && !trimmed.StartsWith('#')));
        ct.WriteLine($"&7共 &8[{entries.Count}] &7个资源");
        int success = 0, failed = 0;
        foreach (string entry in entries) {
            try {
                string[] parts = entry.Split(' ',2,StringSplitOptions.RemoveEmptyEntries);
                string input = parts[0];
                string? version = parts.Length > 1 ? parts[1] : null;
                Add(input,version,outputDir,ct);
                success++;
            } catch (Exception e) {
                ct.WriteLine($"&c失败 &8{entry}&7: {e.Message}",Terminal.MessageType.Error);
                failed++;
            }
        }
        ct.WriteLine($"&a完成! &7成功 &8[{success}] &7个" + (failed > 0 ? $", &c失败 &8[{failed}] &7个" : ""));
    }

    void Update(string directory,IChainedLikeTerminal ct) {
        ct.WriteLine("&aModrinth &7更新资源");
        string[] mrfFiles = Directory.Exists(directory)
            ? Directory.GetFiles(directory,"*.mrf",SearchOption.AllDirectories)
            : [];
        if (mrfFiles.Length == 0) {
            ct.WriteLine("&7未找到.mrf文件");
            return;
        }
        ct.WriteLine($"&7找到 &8[{mrfFiles.Length}] &7个资源文件");

        (List<string> loaders,List<string> gameVersions) = LoadMrpackDependencies();

        List<(string path,MrHostedFile mrf)> entries = [];
        entries.AddRange(from file in mrfFiles 
                         let mrf = JsonSerializer.Deserialize<MrHostedFile>(File.ReadAllText(file))! 
                         select (file,mrf));

        Dictionary<string,Version> currentVersions = [];
        try {
            Task<IDictionary<string,Version>> hashTask = _client.VersionFile
                .GetMultipleVersionsByHashAsync(entries
                                                    .Select(e => e.mrf.Sha1)
                                                    .Distinct().ToArray());
            hashTask.Wait();
            foreach (KeyValuePair<string,Version> kv in hashTask.Result) currentVersions[kv.Key] = kv.Value;
        } catch {
            ct.WriteLine("&7哈希查询失败,尝试逐个查询...");
            foreach ((string path, MrHostedFile mrf) entry in entries) {
                try {
                    Task<Version> verTask = _client.Version.GetAsync(entry.mrf.VersionId);
                    verTask.Wait();
                    currentVersions[entry.mrf.Sha1] = verTask.Result;
                } catch (Exception e) {
                    ct.WriteLine($"&7获取版本时出现异常 &8[&7{e}&8] {entry.path}");
                }
            }
        }

        Dictionary<string,Version[]> projectLatestVersions = [];
        foreach (string projectId in currentVersions
                     .Select(kv => kv.Value.ProjectId)
                     .Distinct()) {
            try {
                Task<Version[]> listTask = _client.Version
                    .GetProjectVersionListAsync(projectId,
                                                loaders.Count > 0 
                                                    ? loaders.ToArray() 
                                                    : null,
                                                gameVersions.Count > 0 
                                                    ? gameVersions.ToArray() 
                                                    : null);
                listTask.Wait();
                projectLatestVersions[projectId] = listTask.Result;
            } catch (Exception e) {
                ct.WriteLine($"&7无法获取项目 &8{projectId} &7的版本列表 [{e.Message}]");
            }
        }

        int updated = 0;
        int skipped = 0;
        foreach ((string path, MrHostedFile mrf) entry in entries) {
            if (entry.mrf.Locked) {
                skipped++;
                continue;
            }
            if (!currentVersions.TryGetValue(entry.mrf.Sha1,out Version? currentVer)) continue;
            if (!projectLatestVersions.TryGetValue(currentVer.ProjectId,out Version[]? latestVersions)) continue;

            Version? latest = latestVersions.FirstOrDefault(v => v.ProjectVersionType == ProjectVersionType.Release) ?? latestVersions.FirstOrDefault();
            if (latest == null || latest.Id == currentVer.Id) continue;

            global::Modrinth.Models.File newFile = GetPrimaryFile(latest);
            string cachePath = Path.Combine(LocalPath,newFile.Hashes.Sha1);
            ct.WriteLine($"&7正在更新 &8[&7{currentVer.Name}&8] &c{currentVer.VersionNumber} &7-> &a{latest.VersionNumber}");
            FileDownloader.DownloadFile(newFile.Url,cachePath);

            Tools.WriteAllText(entry.path,JsonSerializer.Serialize(new MrHostedFile {
                Sha1 = newFile.Hashes.Sha1,
                VersionId = latest.Id
            },Tools.JsonSerializerOptions));
            updated++;
        }
        ct.WriteLine($"&a完成! &7已更新 &8[{updated}] &7个资源" + (skipped > 0 ? $", &e跳过 &8[{skipped}] &7个已锁定" : ""));
    }

    void SetLock(string input,bool lockState,IChainedLikeTerminal ct) {
        string[] mrfFiles = Directory.Exists(".")
            ? Directory.GetFiles(".","*.mrf",SearchOption.AllDirectories)
            : [];

        // 先尝试按文件名匹配
        int count = 0;
        foreach (string file in mrfFiles) {
            string fileName = Path.GetFileName(file);
            if (!fileName.Contains(input,StringComparison.OrdinalIgnoreCase)) continue;
            MrHostedFile mrf = JsonSerializer.Deserialize<MrHostedFile>(File.ReadAllText(file))!;
            mrf.Locked = lockState;
            Tools.WriteAllText(file,JsonSerializer.Serialize(mrf,Tools.JsonSerializerOptions));
            ct.WriteLine($"&7{(lockState ? "已锁定" : "已解锁")} &8{file}");
            count++;
        }
        if (count > 0) {
            ct.WriteLine($"&a完成! &7{(lockState ? "已锁定" : "已解锁")} &8[{count}] &7个资源");
            return;
        }

        
        // 文件名无匹配,尝试按项目ID/slug匹配
        (string slugOrId,_) = ParseModrinthInput(input);
        ct?.WriteLine($"&7文件名无匹配,正在获取项目信息 &8[&7{slugOrId}&8]");
        Task<Project> projectTask = _client.Project.GetAsync(slugOrId);
        projectTask.Wait();
        Project project = projectTask.Result;
        ct?.WriteLine($"&a{project.Title} &8({project.Id})");

        foreach (string file in mrfFiles) {
            MrHostedFile mrf = JsonSerializer.Deserialize<MrHostedFile>(File.ReadAllText(file))!;
            Task<Version> verTask = _client.Version.GetAsync(mrf.VersionId);
            verTask.Wait();
            if (verTask.Result.ProjectId != project.Id) continue;
            mrf.Locked = lockState;
            Tools.WriteAllText(file,JsonSerializer.Serialize(mrf,Tools.JsonSerializerOptions));
            ct?.WriteLine($"&7{(lockState ? "已锁定" : "已解锁")} &8{file}");
            count++;
        }
        ct?.WriteLine($"&a完成! &7{(lockState ? "已锁定" : "已解锁")} &8[{count}] &7个资源");
    }
}
