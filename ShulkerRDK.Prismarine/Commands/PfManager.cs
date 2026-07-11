using System.ComponentModel;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using ShulkerRDK.Prismarine.Services;
using ShulkerRDK.Prismarine.Shared;
using ShulkerRDK.Shared;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Exporters;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Core.Utilities;
using TridentCore.Purl;

namespace ShulkerRDK.Prismarine.Commands;

public static class PfManager {
    const string Extension = ".prf.json";

    public static string? Method(string[] args,LevitateExecutionContext ec) {
        LevitateLogger ct = ec.Logger;
        ct.AddNode("&bPrismarineFile");
        if (!Tools.TryGetSub(["serialize","solidify","restore","update","lock","unlock","index","export"],
                             args,1,ct)) return null;
        switch (args[1]) {
            case "serialize":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                bool keep = args.Contains("--keep");
                Serialize(args[2],!keep,ct);
                break;
            case "solidify" or "restore":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                string? solidifyOut = Tools.CheckParamLength(args,3) && !args[3].StartsWith("--") ? args[3] : null;
                keep = args.Contains("--keep");
                Solidify(args[2],solidifyOut,keep,ct);
                break;
            case "update":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                Update(args[2],ct);
                break;
            case "lock":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                SetLock(args[2],true,ct);
                break;
            case "unlock":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                SetLock(args[2],false,ct);
                break;
            case "index":
                string dest = ec.EnvVars["project.cache"];
                if (Tools.CheckParamLength(args,2)) dest = args[2];
                BuildProfile(dest,ct);
                break;
            case "export":
                if (!Tools.CheckParamLength(args,2,ct)) return null;
                if (!Tools.CheckParamLength(args,3,ct)) return null;
                if (!Tools.CheckParamLength(args,4)) return null;
                Export(args[2],args[3],args[4],ct);
                break;
        }
        return null;
    }

    [Description("PrismarineFile 管理")]
    public static void Command(string[] args,ShulkerContext context) {
        ChainedTerminal ct = new ChainedTerminal("&bPrismarineFile");
        if (!Tools.TryGetSub([
                "create","search","serialize","solidify","update","lock","unlock",
                "c","f","s","r","u","l","ul"
            ],args,1,ct)) return;
        string path = context.ProjectConfig!.RootPath;
        switch (args[1]) {
            case "create" or "c":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                string? createOut = Tools.CheckParamLength(args,3) ? args[3] : null;
                Create(args[2],createOut,ct);
                break;
            case "search" or "f":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                string? sVer = Tools.CheckParamLength(args,3) ? args[3] : null;
                string? sLoader = Tools.CheckParamLength(args,4) ? args[4] : null;
                Search(args[2],new Filter(Version: sVer,Loader: sLoader,Kind: null),ct);
                break;
            case "serialize" or "s":
                if (Tools.CheckParamLength(args,2)) path = args[2];
                bool keep = args.Contains("--keep");
                Serialize(path,!keep,ct);
                break;
            case "solidify" or "r":
                if (Tools.CheckParamLength(args,2)) path = args[2];
                string? solidOut = Tools.CheckParamLength(args,3) && !args[3].StartsWith("--") ? args[3] : null;
                keep = args.Contains("--keep");
                Solidify(path,solidOut,keep,ct);
                break;
            case "update" or "u":
                if (Tools.CheckParamLength(args,2)) path = args[2];
                Update(path,ct);
                break;
            case "lock" or "l":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                SetLock(args[2],true,ct);
                break;
            case "unlock" or "ul":
                if (!Tools.CheckParamLength(args,2,ct)) return;
                SetLock(args[2],false,ct);
                break;
        }
    }


    static void Create(string purl,string? outputDir,IChainedLikeTerminal ct) {
        outputDir ??= "./";

        PrismarineFileMeta meta;
        try {
            meta = new PrismarineFileMeta(purl);
        }
        catch (Exception ex) {
            ct.WriteLine($"&cPURL 解析失败&8[&c{ex.Message}&8]",Terminal.MessageType.Error);
            return;
        }

        if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

        string fileName = GetFileNameFromPurl(purl);
        string filePath = Path.Combine(outputDir,fileName);

        ct.WriteLine($"&7创建 &8[&b{purl}&8]");

        try {
            if (meta.Limiter == null) {
                meta.Update(PrismarineContext.GetLimiter(meta.Type));
            }
        }
        catch (Exception ex) {
            ct.WriteLine($"&e解析时出现问题&8[&c{ex.Message}&8]",Terminal.MessageType.Warn);
        }

        PrismarineFileInstance.Create(filePath,meta);
        ct.WriteLine($"&a已创建 &8[&b{filePath}&8]");
    }

    static void Search(string query,Filter filter,IChainedLikeTerminal ct) {
        ct.WriteLine($"&7搜索 &8[&b{query}&8] &7在 &bmodrinth");

        RepositoryAgent agent = TridentServices.RepositoryAgent;
        IPaginationHandle<Exhibit> handle = agent.SearchAsync("modrinth",query,filter).GetAwaiter().GetResult();
        List<Exhibit> results = handle.FetchAsync(CancellationToken.None).GetAwaiter().GetResult().ToList();

        if (results.Count == 0) {
            ct.WriteLine("&7无结果");
            return;
        }

        ct.WriteLine($"&7找到 &b{handle.TotalCount}&7 个结果 &8[&7显示前{results.Count}条&8]");
        foreach (Exhibit exhibit in results) {
            ct.WriteLine($"  &b{exhibit.Name} &8[&7{exhibit.Kind}&8] &8by &7{exhibit.Author}");
            ct.WriteLine($"    &7{exhibit.Summary}");
            ct.WriteLine($"    &8purl: &7{PackageHelper.ToPurl(exhibit.Label,exhibit.Namespace,exhibit.Pid,null)}");
        }
    }

    static void Serialize(string directory,bool destroySource,IChainedLikeTerminal ct) {
        if (!Directory.Exists(directory)) {
            ct.WriteLine($"&c目录不存在&8[&7{directory}&8]",Terminal.MessageType.Error);
            return;
        }

        string[] files = Directory.GetFiles(directory,"*",SearchOption.AllDirectories)
            .Where(f => !f.EndsWith(Extension))
            .ToArray();

        if (files.Length == 0) {
            ct.WriteLine("&7目录内无可编入文件");
            return;
        }

        ct.WriteLine($"&7扫描到 &b{files.Length}&7 个文件, 正在识别...");

        RepositoryAgent agent = TridentServices.RepositoryAgent;
        int matched = 0,unmatched = 0;

        foreach (string file in files) {
            try {
                ct.WriteLine($"  &8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);

                Package resolved = agent.IdentityAsync(file).GetAwaiter().GetResult();
                string purl = PackageHelper.ToPurl(resolved);

                PrismarineFileMeta meta = new PrismarineFileMeta(resolved);
                string destPath = $"{file}{Extension}";

                PrismarineFileInstance.Create(destPath,meta);

                ct.WriteLine($"  &a识别 &8[&7{Path.GetFileName(file)} &8→ &b{purl}&8]");

                if (destroySource) File.Delete(file);

                matched++;
            }
            catch {
                unmatched++;
            }
        }

        ct.WriteLine(
                     $"&a完成! &8[&7识别 &b{matched}&8" + (unmatched > 0 ? $" &8/ &e未识别 &b{unmatched}" : "") + "&8]");
    }

    static void Solidify(string input,string? outputDir,bool keepSource,IChainedLikeTerminal ct) {
        outputDir ??= input;

        string[] pfmFiles;
        if (Directory.Exists(input))
            pfmFiles = Directory.GetFiles(input,$"*{Extension}",SearchOption.AllDirectories);
        else if (File.Exists(input))
            pfmFiles = [input];
        else {
            ct.WriteLine($"&c路径不存在&8[&7{input}&8]",Terminal.MessageType.Error);
            return;
        }

        if (pfmFiles.Length == 0) {
            ct.WriteLine($"&7未找到 {Extension} 文件");
            return;
        }

        ct.WriteLine($"&7固实化 &b{pfmFiles.Length}&7 个文件");

        RepositoryAgent agent = TridentServices.RepositoryAgent;
        int success = 0,failed = 0;

        List<(string File,string RelativePath,PackageIdentifier Id,Filter Filter)> items =
            new List<(string File,string RelativePath,PackageIdentifier Id,Filter Filter)>();
        foreach (string file in pfmFiles) {
            try {
                PrismarineFileInstance instance = PrismarineFileInstance.Load(file);
                PrismarineFileMeta meta = instance.Meta;

                if (!PackageHelper.TryParse(meta.Purl,out (string Label,string? Namespace,string Pid,string? Vid) pdi)) {
                    ct.WriteLine($"&c无法解析 PURL&8[&7{meta.Purl}&8]",Terminal.MessageType.Error);
                    failed++;
                    continue;
                }

                string relativePath = Path.GetRelativePath(input,file);
                Filter filter = meta.Limiter ?? Filter.None;
                PackageIdentifier id = new PackageIdentifier(pdi.Label,pdi.Namespace,pdi.Pid,
                                                             pdi.Vid == "unresolved" ? null : pdi.Vid);
                items.Add((file,relativePath,id,filter));
            }
            catch (Exception ex) {
                ct.WriteLine($"&c加载失败&8[&7{Path.GetFileName(file)}&8 - &c{ex.Message}&8]",Terminal.MessageType.Error);
                failed++;
            }
        }

        if (items.Count == 0) {
            ct.WriteLine($"&a完成! &8[&7成功 &b0&8" + (failed > 0 ? $" &8/ &c失败 &b{failed}" : "") + "&8]");
            return;
        }

        foreach (IGrouping<Filter,(string File,string RelativePath,PackageIdentifier Id,Filter Filter)> group in items.GroupBy(x => x.Filter)) {
            Filter filter = group.Key;
            List<PackageIdentifier> batch = group.Select(x => x.Id).ToList();
            IReadOnlyList<(PackageIdentifier,Package)> results;
            try {
                results = agent.ResolveBatchAsync(batch,filter).GetAwaiter().GetResult();
            }
            catch (Exception ex) {
                ct.WriteLine($"&c批量解析失败&8[&c{ex.Message}&8]",Terminal.MessageType.Error);
                foreach ((string File,string RelativePath,PackageIdentifier Id,Filter Filter) item in group) {
                    ct.WriteLine($"&c失败&8[&7{Path.GetFileName(item.File)}&8]",Terminal.MessageType.Error);
                }
                failed += group.Count();
                continue;
            }
            Dictionary<PackageIdentifier,Package> resolvedMap = new Dictionary<PackageIdentifier,Package>();
            foreach ((PackageIdentifier id,Package pkg) in results)
                resolvedMap[id] = pkg;
            foreach ((string file,string relativePath,PackageIdentifier id,Filter _) in group) {
                if (!resolvedMap.TryGetValue(id,out Package? resolved)) {
                    ct.WriteLine($"&c未解析&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Error);
                    failed++;
                    continue;
                }
                try {
                    string destPath = Path.Combine(outputDir,relativePath);
                    destPath = destPath[..^Extension.Length];

                    string? dir = Path.GetDirectoryName(destPath);
                    if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string extension = Path.GetExtension(resolved.FileName);
                    string cachePath = PathDef.Default.FileOfPackageObject(
                                                                           resolved.Label,resolved.Namespace,resolved.ProjectId,resolved.VersionId,
                                                                           extension);

                    bool cached = FileHelper.VerifyModified(cachePath,null,resolved.Sha1);
                    ct.WriteLine(cached
                                     ? $"  &7缓存命中 &8[&b{resolved.FileName} &8→ &7{Path.GetFileName(destPath)}&8]"
                                     : $"  &7下载 &8[&b{resolved.FileName} &8→ &7{Path.GetFileName(destPath)}&8]");

                    if (!cached) {
                        FileDownloader.DownloadFile(resolved.Download.ToString(),cachePath);
                    }

                    File.Copy(cachePath,destPath,true);

                    if (!keepSource) File.Delete(file);

                    success++;
                }
                catch (Exception ex) {
                    ct.WriteLine($"&c失败&8[&7{Path.GetFileName(file)}&8 - &c{ex.Message}&8]",Terminal.MessageType.Error);
                    failed++;
                }
            }
        }

        ct.WriteLine($"&a完成! &8[&7成功还原 &b{success}&8" + (failed > 0 ? $" &8/ &c失败 &b{failed}" : "") + "&8]");
    }

    static void Update(string input,IChainedLikeTerminal ct) {
        string[] pfmFiles;
        if (Directory.Exists(input))
            pfmFiles = Directory.GetFiles(input,$"*{Extension}",SearchOption.AllDirectories);
        else if (File.Exists(input))
            pfmFiles = [input];
        else {
            ct.WriteLine($"&c路径不存在&8[&7{input}&8]",Terminal.MessageType.Error);
            return;
        }

        if (pfmFiles.Length == 0) {
            ct.WriteLine($"&7未找到 {Extension} 文件");
            return;
        }

        ct.WriteLine($"&7更新 &b{pfmFiles.Length}&7 个文件");

        int updated = 0,skipped = 0,failed = 0,keeped = 0;

        foreach (string file in pfmFiles) {
            try {
                PrismarineFileInstance instance = PrismarineFileInstance.Load(file);
                PrismarineFileMeta meta = instance.Meta;
                if (meta.Locked) {
                    ct.WriteLine($"  &a跳过 &8[&7{Path.GetFileName(file)}&8]");
                    skipped++;
                    continue;
                }
                try {
                    string prevHash = meta.Sha1 ?? "";
                    meta.Update();
                    if (prevHash == meta.Sha1) {
                        ct.WriteLine($"  &a已最新 &8[&7{Path.GetFileName(file)}&8]");
                        keeped++;
                        continue;
                    }
                    string newFileName = (meta.FileName ?? GetFileNameFromPurl(meta.Purl)[..^Extension.Length]) + Extension;
                    string? dir = Path.GetDirectoryName(file);
                    string newPath = dir != null ? Path.Combine(dir,newFileName) : newFileName;

                    if (!string.Equals(file,newPath,StringComparison.OrdinalIgnoreCase)) {
                        instance.Save(newPath);
                        if (File.Exists(newPath))
                            File.Delete(file);
                        ct.WriteLine($"  &a更新 &8[&7{Path.GetFileName(file)} &8→ &7{newFileName}&8]");
                    } else {
                        instance.Save();
                        ct.WriteLine($"  &a更新 &8[&7{Path.GetFileName(file)}&8]");
                    }
                    updated++;
                }
                catch {
                    skipped++;
                }
            }
            catch (Exception ex) {
                ct.WriteLine($"&c失败&8[&7{Path.GetFileName(file)}&8 for &c{ex.Message}&8]",Terminal.MessageType.Error);
                failed++;
            }
        }

        ct.WriteLine($"&a完成! &8[&7更新 &b{updated}&8 / &7跳过 &b{skipped}&8 / &7保持 &b{keeped}&8" + (failed > 0 ? $" / &c失败 &b{failed}" : "") + "&8]");
    }

    static void SetLock(string input,bool lockState,IChainedLikeTerminal ct) {
        string[] pfmFiles;
        if (Directory.Exists(input))
            pfmFiles = Directory.GetFiles(input,$"*{Extension}",SearchOption.AllDirectories);
        else if (File.Exists(input))
            pfmFiles = [input];
        else {
            ct.WriteLine($"&c路径不存在&8[&7{input}&8]",Terminal.MessageType.Error);
            return;
        }

        int count = 0;
        foreach (string file in pfmFiles) {
            PrismarineFileInstance instance = PrismarineFileInstance.Load(file);
            instance.Meta.Locked = lockState;
            instance.Save();
            ct.WriteLine($"  &7{(lockState ? "锁定" : "解锁")} &8[&7{Path.GetFileName(file)}&8]");
            count++;
        }

        ct.WriteLine($"&a完成, {(lockState ? "锁定" : "解锁")} &b{count}&7 个项目");
    }

    static void Export(string format,string targetDir,string output,IChainedLikeTerminal ct) {
        string profilePath = Path.Combine(targetDir,"profile.json");
        if (!File.Exists(profilePath)) {
            ct.WriteLine($"&c档案文件不存在&8[&7{profilePath}&8]",Terminal.MessageType.Error);
            return;
        }
        Profile profile;
        try {
            profile = JsonSerializer.Deserialize<Profile>(File.ReadAllText(profilePath),Tools.JsonSerializerOptions)!;
        }
        catch (Exception ex) {
            ct.WriteLine($"&c档案文件解析失败&8[&c{ex.Message}&8]",Terminal.MessageType.Error);
            return;
        }
        string importDir = Path.Combine(targetDir,"import");
        ct.WriteLine($"&7导出: &b{Path.GetFileName(targetDir)} &7→ &b{format}");
        IProfileExporter? exporter = TridentServices.Provider
            .GetServices<IProfileExporter>()
            .FirstOrDefault(x => string.Equals(x.Label,format,StringComparison.OrdinalIgnoreCase));
        if (exporter == null) {
            ct.WriteLine($"&c未知格式&8[&7{format}&8][&7trident&8, &7modrinth&8, &7curseforge&8]",Terminal.MessageType.Error);
            return;
        }
        UncompressedProfilePack pack
            = new UncompressedProfilePack(
                                          "temp",profile,
                                          new PackData { IncludingSource = false,IncludingTags = true },
                                          profile.Name,profile.Setup.Source ?? "Unknown",profile.Setup.Version);

        PackedProfileContainer container;
        try {
            container = exporter.PackAsync(pack).GetAwaiter().GetResult();
        }
        catch (Exception ex) {
            ct.WriteLine($"&c导出失败&8[&c{ex.Message}&8]",Terminal.MessageType.Error);
            return;
        }
        string? outputDir = Path.GetDirectoryName(output);
        if (outputDir != null && !Directory.Exists(outputDir))
            Directory.CreateDirectory(outputDir);
        try {
            using FileStream stream = new FileStream(output,FileMode.Create);
            WritePack(stream,container,importDir);
        }
        catch (Exception ex) {
            ct.WriteLine($"&c写入文件失败&8[&c{ex.Message}&8]",Terminal.MessageType.Error);
            return;
        }
        container.Dispose();
        ct.WriteLine($"&a导出完成&8[&b{output}&8]");
    }

    static void WritePack(Stream writer,PackedProfileContainer container,string importDir) {
        using ZipArchive zip = new ZipArchive(writer,ZipArchiveMode.Create,true);
        HashSet<string> added = [];
        foreach ((string name,Stream attachment) in container.Attachments) {
            string entryPath = name.Replace('\\','/');
            ZipArchiveEntry entry = zip.CreateEntry(entryPath);
            using Stream entryStream = entry.Open();
            attachment.CopyTo(entryStream);
            added.Add(entryPath);
        }
        if (Directory.Exists(importDir)) {
            Queue<string> dirs = new Queue<string>();
            dirs.Enqueue(importDir);
            while (dirs.TryDequeue(out string? dir)) {
                foreach (string sub in Directory.GetDirectories(dir))
                    dirs.Enqueue(sub);
                foreach (string file in Directory.GetFiles(dir)) {
                    string relative = Path.GetRelativePath(importDir,file);
                    string entryPath = Path.Combine(container.OverrideDirectoryName,relative).Replace('\\','/');
                    if (added.Contains(entryPath)) continue;
                    ZipArchiveEntry entry = zip.CreateEntry(entryPath);
                    using Stream entryStream = entry.Open();
                    using FileStream fileStream = new FileStream(file,FileMode.Open,FileAccess.Read);
                    fileStream.CopyTo(entryStream);
                    added.Add(entryPath);
                }
            }
        }
        foreach ((string rel,string abs) in container.Files) {
            string relative = rel.Replace('\\','/');
            if (added.Contains(relative) || !File.Exists(abs)) continue;
            ZipArchiveEntry entry = zip.CreateEntry(relative);
            using Stream entryStream = entry.Open();
            using FileStream fileStream = new FileStream(abs,FileMode.Open,FileAccess.Read);
            fileStream.CopyTo(entryStream);
        }
    }

    static void BuildProfile(string dest,IChainedLikeTerminal? ct = null) {
        ct?.WriteLine("&7正在收集项目碎片");
        Profile profile = GenProfile(dest);
        ct?.WriteLine("&7正在导出",Terminal.MessageType.Debug);
        File.WriteAllText(Path.Combine(dest,"profile.json"),
                          JsonSerializer
                              .Serialize(profile,Tools.JsonSerializerOptions));
        ct?.WriteLine("&a索引完成");
    }

    static List<(Profile.Rice.Entry Entry,string Location)> CollectPrf(string path,bool destroySource = true) {
        if (!Directory.Exists(path)) return [];
        List<(Profile.Rice.Entry Entry,string Location)> table = [];
        foreach (string file
                 in Directory.GetFiles(path,"*.prf.json",
                                       SearchOption.AllDirectories)) {
            table.Add((PrismarineFileInstance.Load(file).Meta.ToEntry(),
                          Path.GetRelativePath(path,file)));
            if (destroySource) File.Delete(file);
        }
        return table;
    }

    static Profile GenProfile(string path,bool destroySource = true) {
        Profile profile = PrismarineContext.LoadProfile();
        foreach (Profile.Rice.Entry entry
                 in CollectPrf(Path.Combine(path,"import/resources"),destroySource)
                     .Concat(CollectPrf(Path.Combine(path,"import/mods"),destroySource))
                     .Concat(CollectPrf(Path.Combine(path,"import/shaderpacks"),destroySource))
                     .Select(x => x.Entry)) {
            profile.Setup.Packages.Add(entry);
        }
        foreach ((Profile.Rice.Entry Entry,string Location) meta in CollectPrf(path,destroySource)) {
            profile.Setup.Packages.Add(meta.Entry);
            profile.Setup.Rules.Add(new Profile.Rice.Rule {
                Selector = new Profile.Rice.Rule.RuleSelector {
                    Type = Profile.Rice.Rule.RuleSelector.SelectorType.Purl,
                    Purl = meta.Entry.Purl
                },
                Destination = Path.GetDirectoryName(meta.Location),
                Normalizing = false
            });
        }
        return profile;
    }

    static string GetFileNameFromPurl(string purl) {
        if (!PackageHelper.TryParse(purl,out (string Label,string? Namespace,string Pid,string? Vid) pdi))
            return $"{purl.Replace(':','_').Replace('/','_')}{Extension}";
        string ns = pdi.Namespace != null ? $"{pdi.Namespace}." : "";
        string vid = pdi.Vid ?? "unresolved";
        return $"{ns}{pdi.Pid}@{vid}{Extension}";
    }
}