using System.Reactive.Linq;
using System.Text.Json;
using ShulkerRDK.Prismarine.Shared;
using ShulkerRDK.Shared;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Accounts;
using TridentCore.Abstractions.Extensions;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Importers;
using TridentCore.Abstractions.Tasks;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Engines.Deploying;
using TridentCore.Core.Engines.Launching;
using TridentCore.Core.Exceptions;
using TridentCore.Core.Igniters;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Services.Profiles;
using TridentCore.Core.Utilities;
using TridentCore.Pref;

namespace ShulkerRDK.Prismarine.Services;

public static class InstanceService {
    static readonly Lock LogLock = new();

    public static string ResolveKey(string? key) =>
        !string.IsNullOrWhiteSpace(key) ? key.Trim() : PrismarineConfig.Instance.DefaultInstance;

    public static bool Exists(string key) =>
        TridentServices.ProfileManager.TryGetImmutable(key, out _);

    public static bool IsRunning(string key) =>
        TridentServices.InstanceManager.IsInUse(key);

    public static string? GetDirectory(string kind, string key) {
        string? path = kind.ToLowerInvariant() switch {
            "home" => PathDef.Default.DirectoryOfHome(key),
            "build" => PathDef.Default.DirectoryOfBuild(key),
            "import" => PathDef.Default.DirectoryOfImport(key),
            "persist" => PathDef.Default.DirectoryOfPersist(key),
            "rp" or "resourcepacks" => Path.Combine(PathDef.Default.DirectoryOfBuild(key), "resourcepacks"),
            "mods" => Path.Combine(PathDef.Default.DirectoryOfBuild(key), "mods"),
            _ => null
        };
        return path != null ? Path.GetFullPath(path) : null;
    }

    public static string? Create(
        string key,
        string? mcVersion,
        string? loaderInput,
        string? displayName,
        IChainedLikeTerminal ct) {
        if (string.IsNullOrWhiteSpace(key)) {
            ct.WriteLine("&c实例标识不能为空", Terminal.MessageType.Error);
            return null;
        }

        if (Exists(key)) {
            ct.WriteLine($"&c实例已存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return null;
        }

        Profile.Rice setup;
        string name;
        string? effectiveLoader;

        if (string.IsNullOrWhiteSpace(mcVersion)) {
            Profile template;
            try {
                template = PrismarineContext.LoadProfile();
            }
            catch (Exception ex) {
                ct.WriteLine($"&c加载模板档案失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
                return null;
            }

            setup = template.Setup.Clone();
            effectiveLoader = loaderInput ?? template.Setup.Loader;
            name = !string.IsNullOrWhiteSpace(displayName)
                ? displayName
                : (!string.IsNullOrWhiteSpace(template.Name) ? template.Name : key);
        } else {
            setup = new Profile.Rice { Version = mcVersion.Trim() };
            effectiveLoader = loaderInput;
            name = !string.IsNullOrWhiteSpace(displayName) ? displayName : key;
        }

        string? resolvedLoader;
        try {
            resolvedLoader = LoaderResolver.ResolveToLurlAsync(effectiveLoader, setup.Version).GetAwaiter().GetResult();
        }
        catch (Exception ex) {
            ct.WriteLine($"&c加载器解析失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return null;
        }

        setup.Loader = resolvedLoader;

        Profile profile = new() {
            Name = name,
            Setup = setup
        };

        ReservedKey reserved = TridentServices.ProfileManager.RequestKey(key);
        string actualKey = reserved.Key;
        TridentServices.ProfileManager.Add(reserved, profile);

        if (!string.Equals(actualKey, key, StringComparison.Ordinal)) {
            ct.WriteLine($"&e实例标识已规范化&8[&7{key} &8→ &b{actualKey}&8]", Terminal.MessageType.Warn);
        }

        ct.WriteLine(
            $"&a已创建实例 &8[&b{actualKey}&8] &7MC &b{profile.Setup.Version} &8/ &b{LoaderResolver.FormatDisplay(profile.Setup.Loader)}");
        return actualKey;
    }

    public static string? Ensure(
        string key,
        string? mcVersion,
        string? loaderInput,
        IChainedLikeTerminal ct) {
        if (Exists(key)) {
            ct.WriteLine($"&7实例已就绪 &8[&b{key}&8]");
            return key;
        }

        return Create(key, mcVersion, loaderInput, null, ct);
    }

    public static string? SyncPack(string packDir, string key, IChainedLikeTerminal ct) {
        if (Exists(key) && IsRunning(key)) {
            ct.WriteLine($"&c实例正在运行或部署中, 无法同步整合包&8[&7{key}&8]", Terminal.MessageType.Error);
            return null;
        }

        string profilePath = Path.Combine(packDir, "profile.json");
        if (!File.Exists(profilePath)) {
            ct.WriteLine($"&c档案文件不存在&8[&7{profilePath}&8]", Terminal.MessageType.Error);
            return null;
        }

        Profile? incoming;
        try {
            incoming = JsonSerializer.Deserialize<Profile>(File.ReadAllText(profilePath), FileHelper.SerializerOptions);
        }
        catch (Exception ex) {
            ct.WriteLine($"&c档案文件解析失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return null;
        }

        if (incoming == null || incoming.Setup == null || string.IsNullOrWhiteSpace(incoming.Setup.Version)) {
            ct.WriteLine($"&c档案文件内容无效&8[&7{profilePath}&8]", Terminal.MessageType.Error);
            return null;
        }

        try {
            incoming.Setup.Loader = LoaderResolver
                .ResolveToLurlAsync(incoming.Setup.Loader, incoming.Setup.Version)
                .GetAwaiter().GetResult();
        }
        catch (Exception ex) {
            ct.WriteLine($"&c加载器解析失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return null;
        }

        string actualKey = key;
        if (!Exists(key)) {
            ReservedKey reserved = TridentServices.ProfileManager.RequestKey(key);
            actualKey = reserved.Key;
            TridentServices.ProfileManager.Add(reserved, incoming);
            if (!string.Equals(actualKey, key, StringComparison.Ordinal)) {
                ct.WriteLine($"&e实例标识已规范化&8[&7{key} &8→ &b{actualKey}&8]", Terminal.MessageType.Warn);
            }
        } else {
            ProfileGuard guard = TridentServices.ProfileManager.GetMutable(key);
            guard.Value.Name = incoming.Name;
            guard.Value.Setup = incoming.Setup;
            guard.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        string srcImport = Path.Combine(packDir, "import");
        if (Directory.Exists(srcImport)) {
            string destImport = PathDef.Default.DirectoryOfImport(actualKey);
            MirrorStats stats = MirrorDirectory(srcImport, destImport, deleteStale: true);
            ct.WriteLine(
                $"&a已同步整合包 &8[&b{actualKey}&8] &7MC &b{incoming.Setup.Version} &8/ &b{LoaderResolver.FormatDisplay(incoming.Setup.Loader)} &8(&7复制 &b{stats.Copied}&7 / 跳过 &7{stats.Skipped}&7 / 清理 &7{stats.Deleted}&8)");
        } else {
            ct.WriteLine(
                $"&a已同步档案 &8[&b{actualKey}&8] &7MC &b{incoming.Setup.Version} &8/ &b{LoaderResolver.FormatDisplay(incoming.Setup.Loader)}");
        }

        return actualKey;
    }

    public static bool SyncImport(string srcDir, string key, bool clean, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (IsRunning(key)) {
            ct.WriteLine($"&c实例正在运行或部署中, 无法同步导入目录&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (!Directory.Exists(srcDir)) {
            ct.WriteLine($"&c源目录不存在&8[&7{srcDir}&8]", Terminal.MessageType.Error);
            return false;
        }

        string destImport = PathDef.Default.DirectoryOfImport(key);
        MirrorStats stats = MirrorDirectory(srcDir, destImport, deleteStale: clean);
        ct.WriteLine(
            $"&a已同步导入目录 &8[&b{key}&8] &8(&7复制 &b{stats.Copied}&7 / 跳过 &7{stats.Skipped}" +
            (clean ? $"&7 / 清理 &7{stats.Deleted}" : "") + "&8)");
        return true;
    }

    public static string? SyncResourcePack(string src, string? packName, string key, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return null;
        }

        string rpRoot = Path.Combine(PathDef.Default.DirectoryOfBuild(key), "resourcepacks");
        Directory.CreateDirectory(rpRoot);

        if (File.Exists(src)) {
            string targetName = !string.IsNullOrWhiteSpace(packName) ? packName.Trim() : Path.GetFileName(src);
            string destFile = Path.GetFullPath(Path.Combine(rpRoot, targetName));
            string? parent = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);

            bool copied = CopyFileIfChanged(src, destFile);
            ct.WriteLine(copied
                ? $"&a已同步资源包 &8[&b{targetName}&8 → &7{destFile}&8]"
                : $"&7资源包无变更 &8[&b{targetName}&8]");
            return destFile;
        }

        if (Directory.Exists(src)) {
            string trimmedSrc = Path.TrimEndingDirectorySeparator(Path.GetFullPath(src));
            string targetName = !string.IsNullOrWhiteSpace(packName) ? packName.Trim() : Path.GetFileName(trimmedSrc);
            string destDir = Path.GetFullPath(Path.Combine(rpRoot, targetName));

            MirrorStats stats = MirrorDirectory(
                trimmedSrc,
                destDir,
                deleteStale: true,
                preserveFileNames: ["reload.flag"]);

            ct.WriteLine(
                $"&a已同步资源包 &8[&b{targetName}&8] &8(&7复制 &b{stats.Copied}&7 / 跳过 &7{stats.Skipped}&7 / 清理 &7{stats.Deleted}&8)");
            return destDir;
        }

        ct.WriteLine($"&c资源包源路径不存在&8[&7{src}&8]", Terminal.MessageType.Error);
        return null;
    }

    public static bool AddPackage(string key, string pref, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (!PackageHelper.TryParse(pref, out _)) {
            ct.WriteLine($"&c无效的包引用&8[&7{pref}&8]", Terminal.MessageType.Error);
            return false;
        }

        string identity = PackageHelper.ExtractProjectIdentityIfValid(pref);
        ProfileGuard guard = TridentServices.ProfileManager.GetMutable(key);

        Profile.Rice.Entry? existing = guard.Value.Setup.Packages.FirstOrDefault(
            e => string.Equals(PackageHelper.ExtractProjectIdentityIfValid(e.Pref), identity, StringComparison.OrdinalIgnoreCase));

        if (existing != null) {
            if (string.Equals(existing.Pref, pref, StringComparison.Ordinal) && existing.Enabled) {
                guard.Discard();
                ct.WriteLine($"&7包已存在 &8[&b{pref}&8]");
                return true;
            }

            existing.Pref = pref;
            existing.Enabled = true;
            guard.DisposeAsync().AsTask().GetAwaiter().GetResult();
            ct.WriteLine($"&a已更新包 &8[&b{pref}&8] &7→ &b{key}");
            return true;
        }

        guard.Value.Setup.Packages.Add(new Profile.Rice.Entry {
            Pref = pref,
            Enabled = true
        });
        guard.DisposeAsync().AsTask().GetAwaiter().GetResult();
        ct.WriteLine($"&a已添加包 &8[&b{pref}&8] &7→ &b{key}");
        return true;
    }

    public static bool RemovePackage(string key, string prefOrId, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        ProfileGuard guard = TridentServices.ProfileManager.GetMutable(key);
        string targetIdentity = PackageHelper.ExtractProjectIdentityIfValid(prefOrId);

        List<Profile.Rice.Entry> matched = guard.Value.Setup.Packages.Where(e => {
            if (string.Equals(e.Pref, prefOrId, StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(PackageHelper.ExtractProjectIdentityIfValid(e.Pref), targetIdentity, StringComparison.OrdinalIgnoreCase))
                return true;
            if (PackageHelper.TryParse(e.Pref, out PackageIdentifier parsed) &&
                string.Equals(parsed.Identity, prefOrId, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }).ToList();

        if (matched.Count == 0) {
            guard.Discard();
            ct.WriteLine($"&e未找到匹配的包&8[&7{prefOrId}&8]", Terminal.MessageType.Warn);
            return false;
        }

        foreach (Profile.Rice.Entry entry in matched) {
            guard.Value.Setup.Packages.Remove(entry);
        }

        guard.DisposeAsync().AsTask().GetAwaiter().GetResult();
        ct.WriteLine($"&a已移除 &b{matched.Count}&a 个包 &8[&7{string.Join(", ", matched.Select(x => x.Pref))}&8]");
        return true;
    }

    public static void ListPackages(string key, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return;
        }

        Profile profile = TridentServices.ProfileManager.GetImmutable(key);
        IList<Profile.Rice.Entry> packages = profile.Setup.Packages;
        if (packages.Count == 0) {
            ct.WriteLine($"&7实例 &8[&b{key}&8] &7暂无依赖包");
            return;
        }

        ct.WriteLine($"&7实例 &8[&b{key}&8] &7依赖包列表 &8(&b{packages.Count}&8):");
        foreach (Profile.Rice.Entry entry in packages) {
            string state = entry.Enabled ? "&a●" : "&8○";
            string src = string.IsNullOrWhiteSpace(entry.Source) ? "" : $" &8(来源: {entry.Source})";
            ct.WriteLine($"  {state} &7{entry.Pref}{src}");
        }
    }

    public static string? ImportPack(string packFile, string? key, IChainedLikeTerminal ct) {
        string sourcePath = Path.GetFullPath(packFile);
        if (!File.Exists(sourcePath)) {
            ct.WriteLine($"&c整合包文件不存在&8[&7{sourcePath}&8]", Terminal.MessageType.Error);
            return null;
        }

        ct.WriteLine($"&7正在导入整合包 &8[&b{Path.GetFileName(sourcePath)}&8]...");

        try {
            using FileStream fileStream = new(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using MemoryStream memory = new();
            fileStream.CopyTo(memory);
            memory.Position = 0;

            using CompressedProfilePack pack = new(memory);
            ImportedProfileContainer container = TridentServices.ImporterAgent.ImportAsync(pack).GetAwaiter().GetResult();

            string requestedKey = !string.IsNullOrWhiteSpace(key)
                ? key.Trim()
                : (!string.IsNullOrWhiteSpace(container.Profile.Name)
                    ? container.Profile.Name
                    : Path.GetFileNameWithoutExtension(sourcePath));

            ReservedKey reserved = TridentServices.ProfileManager.RequestKey(requestedKey);
            string actualKey = reserved.Key;

            TridentServices.ImporterAgent.ExtractFilesAsync(actualKey, container, pack).GetAwaiter().GetResult();
            TridentServices.ProfileManager.Add(reserved, container.Profile);

            if (!string.IsNullOrWhiteSpace(key) && !string.Equals(actualKey, key.Trim(), StringComparison.Ordinal)) {
                ct.WriteLine($"&e实例标识已规范化&8[&7{key} &8→ &b{actualKey}&8]", Terminal.MessageType.Warn);
            }

            ct.WriteLine(
                $"&a导入完成 &8[&b{actualKey}&8] &7{container.Profile.Name} &8(&b{container.Profile.Setup.Version} &8/ &b{LoaderResolver.FormatDisplay(container.Profile.Setup.Loader)}&8)");
            return actualKey;
        }
        catch (Exception ex) {
            ct.WriteLine($"&c导入失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return null;
        }
    }

    public static bool Reset(string key, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (IsRunning(key)) {
            ct.WriteLine($"&c实例正在运行或部署中, 无法重置&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        string buildDir = PathDef.Default.DirectoryOfBuild(key);
        string lockFile = PathDef.Default.FileOfLockData(key);
        int cleaned = 0;

        try {
            if (Directory.Exists(buildDir)) {
                Directory.Delete(buildDir, true);
                cleaned++;
            }

            if (File.Exists(lockFile)) {
                File.Delete(lockFile);
                cleaned++;
            }
        }
        catch (Exception ex) {
            ct.WriteLine($"&c重置失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return false;
        }

        ct.WriteLine(cleaned > 0
            ? $"&a已重置实例构建与锁定数据 &8[&b{key}&8]"
            : $"&7实例无需要清理的构建文件 &8[&b{key}&8]");
        return true;
    }

    public static bool Delete(string key, bool confirm, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (IsRunning(key)) {
            ct.WriteLine($"&c实例正在运行或部署中, 无法删除&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (!confirm) {
            Terminal.GetCurrentProvider().Write($"&e确认删除实例 &8[&c{key}&8]&e? 所有文件将被移除 (y/N): &r");
            string? answer = Console.ReadLine()?.Trim();
            if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase)) {
                ct.WriteLine("&7已取消删除");
                return false;
            }
        }

        try {
            string bomb = PathDef.Default.FileOfBomb(key);
            string? bombDir = Path.GetDirectoryName(bomb);
            if (!string.IsNullOrEmpty(bombDir)) Directory.CreateDirectory(bombDir);
            File.WriteAllText(bomb, "delete requested by ShulkerRDK.Prismarine");

            TridentServices.ProfileManager.Remove(key);

            string homeDir = PathDef.Default.DirectoryOfHome(key);
            if (Directory.Exists(homeDir)) {
                try {
                    Directory.Delete(homeDir, true);
                }
                catch {
                    // 文件被占用时保留 bomb 标记，Trident 下次初始化会自动清理
                }
            }

            ct.WriteLine($"&a已删除实例 &8[&b{key}&8]");
            return true;
        }
        catch (Exception ex) {
            ct.WriteLine($"&c删除失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return false;
        }
    }

    public static bool Deploy(string key, bool fullCheck, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (IsRunning(key)) {
            ct.WriteLine($"&c实例正在运行或部署中&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        ct.WriteLine($"&7开始部署实例 &8[&b{key}&8]" + (fullCheck ? " &8(&e全量校验&8)" : ""));

        IObservable<InstanceActivity> activities;
        try {
            activities = TridentServices.InstanceManager.Deploy(key, new DeployOptions(fullCheck));
        }
        catch (Exception ex) {
            ct.WriteLine($"&c无法启动部署&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return false;
        }

        DeployStage? lastStage = null;
        int lastBucket = -1;
        DateTime lastReportAt = DateTime.MinValue;

        IDisposable sub = activities.Subscribe(activity => {
            if (activity is not InstanceActivity.Deploying dep) return;

            if (dep.CurrentStage != lastStage) {
                lastStage = dep.CurrentStage;
                lastBucket = -1;
                lastReportAt = DateTime.UtcNow;
                ct.WriteLine($"  &7阶段 &8[&b{FormatDeployStage(dep.CurrentStage)}&8]");
            }

            if (dep.Progress is ActivityProgress.Determinate det && det.Total > 0) {
                int percent = (int)Math.Clamp(Math.Round(det.Current / det.Total * 100.0), 0, 100);
                int bucket = percent / 20;
                DateTime now = DateTime.UtcNow;
                if (percent > 0 && (bucket > lastBucket || (now - lastReportAt).TotalSeconds >= 1.5)) {
                    lastBucket = bucket;
                    lastReportAt = now;
                    string detail = string.IsNullOrWhiteSpace(det.Stage) ? "" : $" &8({SanitizeForTerminal(det.Stage)})";
                    ct.WriteLine($"    &8[&b{percent,3}%&8] &7{(long)det.Current}/{(long)det.Total}{detail}");
                }
            }
        });

        bool cancelRequested = false;
        ConsoleCancelEventHandler cancelHandler = (_, e) => {
            e.Cancel = true;
            if (!cancelRequested) {
                cancelRequested = true;
                ct.WriteLine($"&e正在取消部署 &8[&7{key}&8]...", Terminal.MessageType.Warn);
                TridentServices.InstanceManager.Abort(key);
            }
        };

        Console.CancelKeyPress += cancelHandler;
        try {
            TridentServices.WaitForActivityAsync(activities).GetAwaiter().GetResult();
            ct.WriteLine($"&a部署完成 &8[&b{key}&8]");
            return true;
        }
        catch (OperationCanceledException) {
            ct.WriteLine($"&e部署已取消 &8[&7{key}&8]", Terminal.MessageType.Warn);
            return false;
        }
        catch (Exception ex) {
            ct.WriteLine($"&c部署失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return false;
        }
        finally {
            Console.CancelKeyPress -= cancelHandler;
            sub.Dispose();
        }
    }

    public static bool Launch(
        string key,
        LaunchMode mode,
        string? serverOverride,
        string? javaHomeOverride,
        uint? memoryOverride,
        string? usernameOverride,
        IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        if (IsRunning(key)) {
            ct.WriteLine($"&c实例正在运行或部署中&8[&7{key}&8]", Terminal.MessageType.Error);
            return false;
        }

        Profile profile = TridentServices.ProfileManager.GetImmutable(key);
        LaunchSettings cfg = PrismarineConfig.Instance.Launch;

        uint width = profile.GetOverride(Profile.OVERRIDE_WINDOW_WIDTH, cfg.WindowWidth);
        uint height = profile.GetOverride(Profile.OVERRIDE_WINDOW_HEIGHT, cfg.WindowHeight);
        uint maxMemory = memoryOverride ?? profile.GetOverride(Profile.OVERRIDE_JAVA_MAX_MEMORY, cfg.MaxMemory);
        string? server = serverOverride
                         ?? profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_CONNECT_SERVER, cfg.QuickConnectAddress);
        string? additionalArgs = profile.GetOverride(Profile.OVERRIDE_JAVA_ADDITIONAL_ARGUMENTS, cfg.AdditionalArguments);
        string? commandWrapper = profile.GetOverride(Profile.OVERRIDE_BEHAVIOR_COMMAND_WRAPPER, cfg.CommandWrapperTemplate);

        string? effectiveJava = javaHomeOverride ?? profile.GetOverride<string>(Profile.OVERRIDE_JAVA_HOME);
        IReadOnlyList<(uint? Major, string Home)> javaVault = PrismarineConfig.Instance.BuildJavaVault(effectiveJava);
        IAccount account = AccountResolver.ResolveOffline(usernameOverride);

        LaunchOptions launchOptions = new(
            brand: "ShulkerRDK.Prismarine",
            launchMode: mode,
            account: account,
            windowSize: (width, height),
            quickConnectAddress: string.IsNullOrWhiteSpace(server) ? null : server,
            maxMemory: maxMemory,
            additionalArguments: additionalArgs,
            commandWrapperTemplate: commandWrapper);

        ct.WriteLine(
            $"&7启动实例 &8[&b{key}&8] &7模式:&b{mode} &7玩家:&b{account.Username} &7内存:&b{maxMemory}MB" +
            (!string.IsNullOrWhiteSpace(server) ? $" &7服务器:&b{server}" : ""));

        IObservable<InstanceActivity> activities;
        try {
            activities = TridentServices.InstanceManager.Launch(key, launchOptions, javaVault);
        }
        catch (Exception ex) {
            ct.WriteLine($"&c无法启动实例&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return false;
        }

        IDisposable? scrapSub = null;
        IDisposable? stateSub = null;
        bool processAnnounced = false;

        if (mode == LaunchMode.Managed) {
            scrapSub = TridentServices.InstanceManager.Scraps
                .Where(x => x.Key == key)
                .Subscribe(x => WriteGameScrap(x.Scrap));

            stateSub = activities.Subscribe(activity => {
                if (!processAnnounced && activity is InstanceActivity.Running { ProcessId: { } pid }) {
                    processAnnounced = true;
                    ct.WriteLine($"&a游戏进程已启动 &8[&7PID &b{pid}&8] &7(按 Ctrl+C 停止游戏)");
                }
            });
        }

        bool cancelRequested = false;
        ConsoleCancelEventHandler cancelHandler = (_, e) => {
            e.Cancel = true;
            if (!cancelRequested) {
                cancelRequested = true;
                ct.WriteLine($"&e正在停止实例 &8[&7{key}&8]...", Terminal.MessageType.Warn);
                TridentServices.InstanceManager.Abort(key);
            }
        };

        Console.CancelKeyPress += cancelHandler;
        try {
            InstanceActivity final = TridentServices.WaitForActivityAsync(activities).GetAwaiter().GetResult();

            if (mode == LaunchMode.Debug) {
                string dumpPath = Path.GetFullPath(Path.Combine(PathDef.Default.DirectoryOfBuild(key), "trident.launch.dump.txt"));
                ct.WriteLine($"&a已生成调试启动转储 &8[&b{dumpPath}&8]");
                return true;
            }

            if (mode == LaunchMode.FireAndForget) {
                ct.WriteLine($"&a实例已在后台启动 &8[&b{key}&8]");
                return true;
            }

            if (final is InstanceActivity.Running running) {
                switch (running.Outcome) {
                    case LaunchOutcome.Detached:
                        ct.WriteLine($"&e实例已分离至后台 &8[&7{key}&8]", Terminal.MessageType.Warn);
                        break;
                    case LaunchOutcome.Aborted:
                        ct.WriteLine($"&e游戏已停止 &8[&7{key}&8]", Terminal.MessageType.Warn);
                        break;
                    default:
                        ct.WriteLine($"&a游戏已退出 &8[&b{key}&8]");
                        break;
                }
            } else {
                ct.WriteLine($"&a游戏已退出 &8[&b{key}&8]");
            }

            return true;
        }
        catch (OperationCanceledException) {
            ct.WriteLine($"&e游戏已停止 &8[&7{key}&8]", Terminal.MessageType.Warn);
            return true;
        }
        catch (ProcessFaultedException pfe) {
            ct.WriteLine($"&c游戏异常退出 &8[&c退出码 {pfe.ExitCode}&8]", Terminal.MessageType.Error);
            return false;
        }
        catch (LockUnavailableException) {
            ct.WriteLine($"&c实例尚未部署&8[&7{key}&8]&c, 请先运行 &binst deploy {key} &c或 &binst run {key}", Terminal.MessageType.Error);
            return false;
        }
        catch (Exception ex) {
            ct.WriteLine($"&c启动失败&8[&c{SanitizeForTerminal(ex.Message)}&8]", Terminal.MessageType.Error);
            return false;
        }
        finally {
            Console.CancelKeyPress -= cancelHandler;
            stateSub?.Dispose();
            scrapSub?.Dispose();
        }
    }

    public static bool Run(
        string key,
        LaunchMode mode,
        bool fullCheck,
        string? serverOverride,
        string? javaHomeOverride,
        uint? memoryOverride,
        string? usernameOverride,
        IChainedLikeTerminal ct) {
        if (!Deploy(key, fullCheck, ct)) return false;
        return Launch(key, mode, serverOverride, javaHomeOverride, memoryOverride, usernameOverride, ct);
    }

    public static bool Stop(string key, IChainedLikeTerminal ct) {
        if (!IsRunning(key)) {
            ct.WriteLine($"&e实例未在运行&8[&7{key}&8]", Terminal.MessageType.Warn);
            return false;
        }

        TridentServices.InstanceManager.Abort(key);
        ct.WriteLine($"&a已发送停止信号 &8[&b{key}&8]");
        return true;
    }

    public static bool Detach(string key, IChainedLikeTerminal ct) {
        if (!IsRunning(key)) {
            ct.WriteLine($"&e实例未在运行&8[&7{key}&8]", Terminal.MessageType.Warn);
            return false;
        }

        TridentServices.InstanceManager.Detach(key);
        ct.WriteLine($"&a已分离实例 &8[&b{key}&8]");
        return true;
    }

    public static void ListInstances(IChainedLikeTerminal ct) {
        List<(string Key, Profile Profile)> profiles = TridentServices.ProfileManager.Profiles
            .OrderBy(x => x.Item1, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (profiles.Count == 0) {
            ct.WriteLine($"&7暂无实例 &8(数据目录: &7{TridentServices.TridentHomePath}&8)");
            return;
        }

        string defaultKey = PrismarineConfig.Instance.DefaultInstance;
        ct.WriteLine($"&7实例列表 &8(&b{profiles.Count}&8) &8[&7默认: &b{defaultKey}&8]:");
        foreach ((string key, Profile profile) in profiles) {
            string marker = string.Equals(key, defaultKey, StringComparison.OrdinalIgnoreCase) ? "&b*" : " ";
            string status = IsRunning(key) ? " &a[运行中]" : "";
            string loader = LoaderResolver.FormatDisplay(profile.Setup.Loader);
            ct.WriteLine(
                $"  {marker} &b{key} &8- &7{SanitizeForTerminal(profile.Name)} &8[&7MC &b{profile.Setup.Version} &8/ &b{loader} &8/ &7{profile.Setup.Packages.Count} pkgs&8]{status}");
        }
    }

    public static void ShowInfo(string key, IChainedLikeTerminal ct) {
        if (!Exists(key)) {
            ct.WriteLine($"&c实例不存在&8[&7{key}&8]", Terminal.MessageType.Error);
            return;
        }

        Profile profile = TridentServices.ProfileManager.GetImmutable(key);
        bool deployed = File.Exists(PathDef.Default.FileOfLockData(key));
        string state = IsRunning(key)
            ? $"&a运行中 ({TridentServices.InstanceManager.ActivityOf(key)?.Kind})"
            : (deployed ? "&7已部署" : "&e未部署");

        ct.WriteLine($"&7实例详情 &8[&b{key}&8]:");
        ct.WriteLine($"  &8名称:   &7{SanitizeForTerminal(profile.Name)}");
        ct.WriteLine($"  &8版本:   &b{profile.Setup.Version}");
        ct.WriteLine($"  &8加载器: &b{LoaderResolver.FormatDisplay(profile.Setup.Loader)} &8({profile.Setup.Loader ?? "vanilla"})");
        ct.WriteLine($"  &8状态:   {state}");
        ct.WriteLine($"  &8依赖包: &b{profile.Setup.Packages.Count}");
        ct.WriteLine($"  &8主目录: &7{Path.GetFullPath(PathDef.Default.DirectoryOfHome(key))}");
        ct.WriteLine($"  &8构建层: &7{Path.GetFullPath(PathDef.Default.DirectoryOfBuild(key))}");
        ct.WriteLine($"  &8导入层: &7{Path.GetFullPath(PathDef.Default.DirectoryOfImport(key))}");
        ct.WriteLine($"  &8持久层: &7{Path.GetFullPath(PathDef.Default.DirectoryOfPersist(key))}");
    }

    static void WriteGameScrap(Scrap scrap) {
        (string levelLabel, string color) = scrap.Level switch {
            ScrapLevel.Error => ("ERR", "&c"),
            ScrapLevel.Warning => ("WRN", "&e"),
            ScrapLevel.Information => ("INF", "&a"),
            _ => ("MC ", "&b")
        };

        string time = string.IsNullOrWhiteSpace(scrap.Time) ? "--:--:--" : SanitizeForTerminal(scrap.Time);
        string thread = string.IsNullOrWhiteSpace(scrap.Thread) ? "" : $"/{SanitizeForTerminal(scrap.Thread)}";
        string sender = string.IsNullOrWhiteSpace(scrap.Sender) ? "" : $" &8[{SanitizeForTerminal(scrap.Sender)}]";
        string prefix = $"&8[&7{time} {color}{levelLabel}&8{thread}]{sender}&r ";

        // 使用 Terminal.Write 输出带色前缀，再直接用 Console.WriteLine 输出原始游戏日志，
        // 避免 AnsiTerminal / LegacyTerminal / MonoTerminal 吞掉或破坏日志中的 '&' 字符。
        lock (LogLock) {
            Terminal.GetCurrentProvider().Write(prefix);
            Console.WriteLine(scrap.Message);
        }
    }

    static string FormatDeployStage(DeployStage stage) => stage switch {
        DeployStage.LoadLock => "读取锁定 (LoadLock)",
        DeployStage.InstallVanilla => "安装原版 (InstallVanilla)",
        DeployStage.ProcessLoader => "处理加载器 (ProcessLoader)",
        DeployStage.ApplyLaunchPatch => "应用补丁 (ApplyLaunchPatch)",
        DeployStage.SyncPackages => "同步依赖包 (SyncPackages)",
        DeployStage.SelectRuntime => "选择 Java 运行时 (SelectRuntime)",
        DeployStage.PersistLock => "写入锁定 (PersistLock)",
        DeployStage.PlanDeployment => "规划部署 (PlanDeployment)",
        DeployStage.ExecuteDeployment => "执行部署 (ExecuteDeployment)",
        _ => stage.ToString()
    };

    static string SanitizeForTerminal(string? text) =>
        string.IsNullOrEmpty(text) ? string.Empty : text.Replace('&', '＆');

    readonly record struct MirrorStats(int Copied, int Skipped, int Deleted);

    static MirrorStats MirrorDirectory(
        string srcDir,
        string destDir,
        bool deleteStale,
        IReadOnlyCollection<string>? preserveFileNames = null) {
        Directory.CreateDirectory(destDir);

        int copied = 0, skipped = 0, deleted = 0;
        HashSet<string> sourceRelFiles = new(FileHelper.PathComparer);
        HashSet<string>? preserveSet = preserveFileNames != null
            ? new HashSet<string>(preserveFileNames, StringComparer.OrdinalIgnoreCase)
            : null;

        foreach (string srcFile in Directory.EnumerateFiles(srcDir, "*", SearchOption.AllDirectories)) {
            string rel = Path.GetRelativePath(srcDir, srcFile);
            sourceRelFiles.Add(rel);

            string destFile = Path.Combine(destDir, rel);
            string? parent = Path.GetDirectoryName(destFile);
            if (!string.IsNullOrEmpty(parent) && !Directory.Exists(parent)) {
                Directory.CreateDirectory(parent);
            }

            if (CopyFileIfChanged(srcFile, destFile)) {
                copied++;
            } else {
                skipped++;
            }
        }

        if (deleteStale) {
            foreach (string destFile in Directory.EnumerateFiles(destDir, "*", SearchOption.AllDirectories).ToList()) {
                string rel = Path.GetRelativePath(destDir, destFile);
                if (sourceRelFiles.Contains(rel)) continue;
                if (preserveSet != null && preserveSet.Contains(Path.GetFileName(destFile))) continue;

                File.Delete(destFile);
                deleted++;
            }

            // 自底向上清理空目录
            List<string> subDirs = Directory.EnumerateDirectories(destDir, "*", SearchOption.AllDirectories)
                .OrderByDescending(d => d.Length)
                .ToList();
            foreach (string dir in subDirs) {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any()) {
                    Directory.Delete(dir, false);
                }
            }
        }

        return new MirrorStats(copied, skipped, deleted);
    }

    static bool CopyFileIfChanged(string srcFile, string destFile) {
        FileInfo srcInfo = new(srcFile);
        if (File.Exists(destFile)) {
            FileInfo destInfo = new(destFile);
            // 若目标为符号链接，先删除再复制为普通文件（TridentCore 禁止 import/ 中包含符号链接）
            if (destInfo.LinkTarget != null) {
                File.Delete(destFile);
            } else if (srcInfo.Length == destInfo.Length &&
                       Math.Abs((srcInfo.LastWriteTimeUtc - destInfo.LastWriteTimeUtc).TotalSeconds) < 1.0) {
                return false;
            }
        }

        File.Copy(srcFile, destFile, true);
        try {
            File.SetLastWriteTimeUtc(destFile, srcInfo.LastWriteTimeUtc);
        }
        catch {
            // 部分文件系统不支持修改时间戳时忽略
        }

        return true;
    }
}
