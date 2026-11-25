using System.ComponentModel;
using ShulkerRDK.Prismarine.Services;
using ShulkerRDK.Prismarine.Shared;
using ShulkerRDK.Shared;
using TridentCore.Abstractions;
using TridentCore.Core.Igniters;
using TridentCore.Core.Utilities;

namespace ShulkerRDK.Prismarine.Commands;

public static class InstanceCommand {
    [Description("Minecraft 调试实例管理")]
    public static void Command(string[] args, ShulkerContext context) {
        ChainedTerminal ct = new("&bInstance");
        if (!Tools.TryGetSub([
                "create", "c",
                "ensure", "e",
                "list", "ls",
                "info", "i",
                "use",
                "sync",
                "pkg",
                "deploy", "d",
                "launch", "l",
                "run", "r",
                "stop",
                "detach",
                "import",
                "reset",
                "delete", "rm",
                "dir",
                "config", "cfg",
                "java"
            ], args, 1, ct)) return;

        switch (args[1]) {
            case "create" or "c":
                HandleCreate(args, ct);
                break;
            case "ensure" or "e":
                HandleEnsure(args, ct);
                break;
            case "list" or "ls":
                InstanceService.ListInstances(ct);
                break;
            case "info" or "i":
                InstanceService.ShowInfo(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct);
                break;
            case "use":
                HandleUse(args, ct);
                break;
            case "sync":
                HandleSync(args, ct);
                break;
            case "pkg":
                HandlePkg(args, ct);
                break;
            case "deploy" or "d":
                HandleDeploy(args, ct);
                break;
            case "launch" or "l":
                HandleLaunch(args, ct);
                break;
            case "run" or "r":
                HandleRun(args, ct);
                break;
            case "stop":
                InstanceService.Stop(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct);
                break;
            case "detach":
                InstanceService.Detach(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct);
                break;
            case "import":
                HandleImport(args, ct);
                break;
            case "reset":
                InstanceService.Reset(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct);
                break;
            case "delete" or "rm":
                HandleDelete(args, ct);
                break;
            case "dir":
                HandleDir(args, ct, printToTerminal: true);
                break;
            case "config" or "cfg":
                HandleConfig(args, ct);
                break;
            case "java":
                HandleJava(args, ct);
                break;
        }
    }

    public static string? Method(string[] args, LevitateExecutionContext ec) {
        LevitateLogger ct = ec.Logger;
        ct.AddNode("&bInstance");

        if (!Tools.TryGetSub([
                "ensure", "create", "sync", "pkg",
                "deploy", "launch", "run",
                "stop", "detach",
                "import", "reset",
                "dir", "exists", "running", "current"
            ], args, 1, ct)) return null;

        return args[1] switch {
            "ensure" => HandleEnsure(args, ct),
            "create" => HandleCreate(args, ct),
            "sync" => HandleSync(args, ct),
            "pkg" => HandlePkg(args, ct),
            "deploy" => HandleDeploy(args, ct) ? "true" : "false",
            "launch" => HandleLaunch(args, ct) ? "true" : "false",
            "run" => HandleRun(args, ct) ? "true" : "false",
            "stop" => InstanceService.Stop(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct) ? "true" : "false",
            "detach" => InstanceService.Detach(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct) ? "true" : "false",
            "import" => HandleImport(args, ct),
            "reset" => InstanceService.Reset(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null), ct) ? "true" : "false",
            "dir" => HandleDir(args, ct, printToTerminal: false),
            "exists" => InstanceService.Exists(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null)) ? "true" : "false",
            "running" => InstanceService.IsRunning(InstanceService.ResolveKey(args.Length > 2 ? args[2] : null)) ? "true" : "false",
            "current" => PrismarineConfig.Instance.DefaultInstance,
            _ => null
        };
    }

    static string? HandleCreate(string[] args, IChainedLikeTerminal ct) {
        if (!Tools.CheckParamLength(args, 2, ct)) return null;
        string key = args[2];
        string? mcVer = args.Length > 3 ? args[3] : null;
        string? loader = args.Length > 4 ? args[4] : null;
        return InstanceService.Create(key, mcVer, loader, null, ct);
    }

    static string? HandleEnsure(string[] args, IChainedLikeTerminal ct) {
        string key = InstanceService.ResolveKey(args.Length > 2 ? args[2] : null);
        string? mcVer = args.Length > 3 ? args[3] : null;
        string? loader = args.Length > 4 ? args[4] : null;
        return InstanceService.Ensure(key, mcVer, loader, ct);
    }

    static void HandleUse(string[] args, IChainedLikeTerminal ct) {
        if (!Tools.CheckParamLength(args, 2, ct)) return;
        string raw = args[2].Trim();
        if (!InstanceService.TryCanonicalizeKey(raw, out string key)) {
            ct.WriteLine($"&c无效的实例标识&8[&c{raw}&8]", Terminal.MessageType.Error);
            return;
        }
        PrismarineConfig.Instance.DefaultInstance = key;
        PrismarineConfig.Instance.Save();
        if (!string.Equals(raw, key, StringComparison.Ordinal)) {
            ct.WriteLine($"&e实例标识已规范化&8[&7{raw} &8→ &b{key}&8]", Terminal.MessageType.Warn);
        }
        ct.WriteLine($"&a已切换默认实例 &8[&b{key}&8]");
        if (!InstanceService.Exists(key)) {
            ct.WriteLine($"&e提示: 实例尚未创建, 可运行 &binst create {key} &e或 &binst ensure {key}", Terminal.MessageType.Warn);
        }
    }

    static string? HandleSync(string[] args, IChainedLikeTerminal ct) {
        if (!Tools.CheckParamLength(args, 2, ct)) return null;

        if (string.Equals(args[2], "import", StringComparison.OrdinalIgnoreCase)) {
            if (!TryParseFlags(args, 3, ct, out ParsedArgs parsed)) return null;
            if (parsed.Positionals.Count < 1) {
                ct.WriteLine("&c缺少源目录参数 &8[&7inst sync import <srcDir> [key] [--clean]&8]", Terminal.MessageType.Error);
                return null;
            }

            string srcDir = parsed.Positionals[0];
            string key = InstanceService.ResolveKey(parsed.Positionals.Count > 1 ? parsed.Positionals[1] : null);
            return InstanceService.SyncImport(srcDir, key, parsed.Clean, ct) ? key : null;
        }

        if (string.Equals(args[2], "rp", StringComparison.OrdinalIgnoreCase)) {
            if (!TryParseFlags(args, 3, ct, out ParsedArgs parsed)) return null;
            if (parsed.Positionals.Count < 1) {
                ct.WriteLine("&c缺少资源包路径参数 &8[&7inst sync rp <src> [packName] [key]&8]", Terminal.MessageType.Error);
                return null;
            }

            string src = parsed.Positionals[0];
            string? packName = parsed.Positionals.Count > 1 ? parsed.Positionals[1] : null;
            string key = InstanceService.ResolveKey(parsed.Positionals.Count > 2 ? parsed.Positionals[2] : null);
            return InstanceService.SyncResourcePack(src, packName, key, ct);
        }

        if (!TryParseFlags(args, 2, ct, out ParsedArgs packParsed)) return null;
        if (packParsed.Positionals.Count < 1) {
            ct.WriteLine("&c缺少整合包目录参数 &8[&7inst sync <packDir> [key]&8]", Terminal.MessageType.Error);
            return null;
        }

        string packDir = packParsed.Positionals[0];
        string targetKey = InstanceService.ResolveKey(packParsed.Positionals.Count > 1 ? packParsed.Positionals[1] : null);
        return InstanceService.SyncPack(packDir, targetKey, ct);
    }

    static string? HandlePkg(string[] args, IChainedLikeTerminal ct) {
        if (!Tools.TryGetSub(["add", "remove", "rm", "list", "ls"], args, 2, ct)) return null;

        switch (args[2]) {
            case "add": {
                if (!Tools.CheckParamLength(args, 3, ct)) return null;
                string key;
                string pref;
                if (args.Length == 4) {
                    key = InstanceService.ResolveKey(null);
                    pref = args[3];
                } else {
                    key = InstanceService.ResolveKey(args[3]);
                    pref = args[4];
                }
                return InstanceService.AddPackage(key, pref, ct) ? "true" : "false";
            }
            case "remove" or "rm": {
                if (!Tools.CheckParamLength(args, 3, ct)) return null;
                string key;
                string target;
                if (args.Length == 4) {
                    key = InstanceService.ResolveKey(null);
                    target = args[3];
                } else {
                    key = InstanceService.ResolveKey(args[3]);
                    target = args[4];
                }
                return InstanceService.RemovePackage(key, target, ct) ? "true" : "false";
            }
            case "list" or "ls": {
                string key = InstanceService.ResolveKey(args.Length > 3 ? args[3] : null);
                InstanceService.ListPackages(key, ct);
                return null;
            }
        }

        return null;
    }

    static bool HandleDeploy(string[] args, IChainedLikeTerminal ct) {
        if (!TryParseFlags(args, 2, ct, out ParsedArgs parsed)) return false;
        string key = InstanceService.ResolveKey(parsed.Positionals.Count > 0 ? parsed.Positionals[0] : null);
        return InstanceService.Deploy(key, parsed.FullCheck, ct);
    }

    static bool HandleLaunch(string[] args, IChainedLikeTerminal ct) {
        if (!TryParseFlags(args, 2, ct, out ParsedArgs parsed)) return false;
        string key = InstanceService.ResolveKey(parsed.Positionals.Count > 0 ? parsed.Positionals[0] : null);
        return InstanceService.Launch(
            key,
            parsed.Mode,
            parsed.Server,
            parsed.JavaHome,
            parsed.Memory,
            parsed.User,
            ct);
    }

    static bool HandleRun(string[] args, IChainedLikeTerminal ct) {
        if (!TryParseFlags(args, 2, ct, out ParsedArgs parsed)) return false;
        string key = InstanceService.ResolveKey(parsed.Positionals.Count > 0 ? parsed.Positionals[0] : null);
        return InstanceService.Run(
            key,
            parsed.Mode,
            parsed.FullCheck,
            parsed.Server,
            parsed.JavaHome,
            parsed.Memory,
            parsed.User,
            ct);
    }

    static string? HandleImport(string[] args, IChainedLikeTerminal ct) {
        if (!Tools.CheckParamLength(args, 2, ct)) return null;
        string packFile = args[2];
        string? key = args.Length > 3 ? args[3] : null;
        return InstanceService.ImportPack(packFile, key, ct);
    }

    static void HandleDelete(string[] args, IChainedLikeTerminal ct) {
        if (!TryParseFlags(args, 2, ct, out ParsedArgs parsed)) return;
        if (parsed.Positionals.Count < 1) {
            ct.WriteLine("&c请指定要删除的实例标识 &8[&7inst delete <key> [--yes]&8]", Terminal.MessageType.Error);
            return;
        }

        InstanceService.Delete(InstanceService.ResolveKey(parsed.Positionals[0]), parsed.Yes, ct);
    }

    static string? HandleDir(string[] args, IChainedLikeTerminal ct, bool printToTerminal) {
        if (!Tools.CheckParamLength(args, 2, ct)) return null;
        string kind = args[2];
        string key = InstanceService.ResolveKey(args.Length > 3 ? args[3] : null);
        string? dir = InstanceService.GetDirectory(kind, key);
        if (dir == null) {
            ct.WriteLine($"&c未知目录类型&8[&c{kind}&8] &7可用项&8[&7home, build, import, persist, rp, mods&8]", Terminal.MessageType.Error);
            return null;
        }

        if (printToTerminal) {
            ct.WriteLine($"&7{dir}");
        }

        return dir;
    }

    static void HandleConfig(string[] args, IChainedLikeTerminal ct) {
        string sub = args.Length > 2 ? args[2].ToLowerInvariant() : "show";
        PrismarineConfig cfg = PrismarineConfig.Instance;

        switch (sub) {
            case "show":
                ct.WriteLine("&7Prismarine 本地实例配置:");
                ct.WriteLine($"  &8instance: &b{cfg.DefaultInstance}");
                ct.WriteLine($"  &8username: &b{cfg.Launch.Username}");
                ct.WriteLine($"  &8uuid:     &7{cfg.Launch.Uuid ?? "(自动离线 UUID)"}");
                ct.WriteLine($"  &8memory:   &b{cfg.Launch.MaxMemory} &7MB");
                ct.WriteLine($"  &8window:   &b{cfg.Launch.WindowWidth}x{cfg.Launch.WindowHeight}");
                ct.WriteLine($"  &8java:     &7{cfg.Launch.JavaHome ?? "(自动选择)"}");
                ct.WriteLine($"  &8server:   &7{cfg.Launch.QuickConnectAddress ?? "(未设置)"}");
                ct.WriteLine($"  &8args:     &7{(string.IsNullOrEmpty(cfg.Launch.AdditionalArguments) ? "(无)" : cfg.Launch.AdditionalArguments)}");
                ct.WriteLine($"  &8wrapper:  &7{(string.IsNullOrEmpty(cfg.Launch.CommandWrapperTemplate) ? "(无)" : cfg.Launch.CommandWrapperTemplate)}");
                ct.WriteLine($"  &8cfkey:    &7{(string.IsNullOrEmpty(cfg.CurseForgeApiKey) ? "(未设置)" : "********")}");
                ct.WriteLine($"  &8trident:  &7{TridentServices.TridentHomePath}");
                break;

            case "set": {
                if (!Tools.CheckParamLength(args, 3, ct)) return;
                if (!Tools.CheckParamLength(args, 4, ct)) return;
                string field = args[3].ToLowerInvariant();
                string value = args.Length > 5 ? string.Join(' ', args[4..]) : args[4];

                switch (field) {
                    case "instance" or "default":
                        if (!InstanceService.TryCanonicalizeKey(value, out string normalizedInstance)) {
                            ct.WriteLine($"&c无效的实例标识&8[&c{value}&8]", Terminal.MessageType.Error);
                            return;
                        }
                        cfg.DefaultInstance = normalizedInstance;
                        value = normalizedInstance;
                        break;
                    case "username" or "user":
                        cfg.Launch.Username = value.Trim();
                        break;
                    case "uuid":
                        cfg.Launch.Uuid = value.Trim();
                        break;
                    case "memory" or "mem":
                        if (!uint.TryParse(value, out uint mem) || mem == 0) {
                            ct.WriteLine($"&c无效的内存大小(MB)&8[&c{value}&8]", Terminal.MessageType.Error);
                            return;
                        }
                        cfg.Launch.MaxMemory = mem;
                        break;
                    case "width":
                        if (!uint.TryParse(value, out uint width) || width == 0) {
                            ct.WriteLine($"&c无效的窗口宽度&8[&c{value}&8]", Terminal.MessageType.Error);
                            return;
                        }
                        cfg.Launch.WindowWidth = width;
                        break;
                    case "height":
                        if (!uint.TryParse(value, out uint height) || height == 0) {
                            ct.WriteLine($"&c无效的窗口高度&8[&c{value}&8]", Terminal.MessageType.Error);
                            return;
                        }
                        cfg.Launch.WindowHeight = height;
                        break;
                    case "java":
                        cfg.Launch.JavaHome = value.Trim();
                        break;
                    case "server":
                        cfg.Launch.QuickConnectAddress = value.Trim();
                        break;
                    case "args":
                        cfg.Launch.AdditionalArguments = value;
                        break;
                    case "wrapper":
                        cfg.Launch.CommandWrapperTemplate = value;
                        break;
                    case "cfkey":
                        cfg.CurseForgeApiKey = value.Trim();
                        break;
                    default:
                        ct.WriteLine(
                            $"&c未知配置项&8[&c{field}&8] &7可用项&8[&7instance, username, uuid, memory, width, height, java, server, args, wrapper, cfkey&8]",
                            Terminal.MessageType.Error);
                        return;
                }

                cfg.Save();
                string displayVal = field == "cfkey" ? "********" : value;
                ct.WriteLine($"&a已更新配置 &8[&b{field} &8= &7{displayVal}&8]");
                break;
            }

            case "unset": {
                if (!Tools.CheckParamLength(args, 3, ct)) return;
                string field = args[3].ToLowerInvariant();
                switch (field) {
                    case "instance" or "default":
                        cfg.DefaultInstance = "dev";
                        break;
                    case "username" or "user":
                        cfg.Launch.Username = "DevPlayer";
                        break;
                    case "uuid":
                        cfg.Launch.Uuid = null;
                        break;
                    case "memory" or "mem":
                        cfg.Launch.MaxMemory = 4096;
                        break;
                    case "width":
                        cfg.Launch.WindowWidth = 1270;
                        break;
                    case "height":
                        cfg.Launch.WindowHeight = 720;
                        break;
                    case "java":
                        cfg.Launch.JavaHome = null;
                        break;
                    case "server":
                        cfg.Launch.QuickConnectAddress = null;
                        break;
                    case "args":
                        cfg.Launch.AdditionalArguments = string.Empty;
                        break;
                    case "wrapper":
                        cfg.Launch.CommandWrapperTemplate = string.Empty;
                        break;
                    case "cfkey":
                        cfg.CurseForgeApiKey = null;
                        break;
                    default:
                        ct.WriteLine(
                            $"&c未知配置项&8[&c{field}&8] &7可用项&8[&7instance, username, uuid, memory, width, height, java, server, args, wrapper, cfkey&8]",
                            Terminal.MessageType.Error);
                        return;
                }

                cfg.Save();
                ct.WriteLine($"&a已重置配置项 &8[&b{field}&8]");
                break;
            }

            default:
                ct.WriteLine($"&c未知操作&8[&c{sub}&8] &7可用项&8[&7show, set, unset&8]", Terminal.MessageType.Error);
                break;
        }
    }

    static void HandleJava(string[] args, IChainedLikeTerminal ct) {
        string sub = args.Length > 2 ? args[2].ToLowerInvariant() : "list";
        PrismarineConfig cfg = PrismarineConfig.Instance;

        switch (sub) {
            case "scan": {
                ct.WriteLine("&7正在扫描本机 Java 运行时...");
                IReadOnlyList<JavaHelper.JavaRuntimeCandidate> candidates =
                    JavaHelper.ScanJavaRuntimesAsync().GetAwaiter().GetResult();

                if (candidates.Count == 0) {
                    ct.WriteLine("&7未扫描到本机 Java, 部署时将自动下载 Mojang 捆绑运行时");
                    return;
                }

                ct.WriteLine($"&7发现 &b{candidates.Count}&7 个 Java 运行时:");
                foreach (JavaHelper.JavaRuntimeCandidate item in candidates) {
                    string majorStr = item.Major?.ToString() ?? "?";
                    string ver = item.Version ?? "unknown";
                    string vendor = string.IsNullOrWhiteSpace(item.Vendor) ? "" : $" &8({item.Vendor})";
                    ct.WriteLine($"  &bJava {majorStr,-2} &7{ver}{vendor}");
                    ct.WriteLine($"    &8{item.Home}");
                }
                break;
            }

            case "list": {
                ct.WriteLine("&7Java 运行时配置:");
                ct.WriteLine($"  &8默认覆盖: &7{cfg.Launch.JavaHome ?? "(未设置, 优先按主版本/捆绑运行时自动匹配)"}");
                if (cfg.Launch.JavaVault.Count > 0) {
                    ct.WriteLine("  &8主版本映射:");
                    foreach (KeyValuePair<uint, string> kv in cfg.Launch.JavaVault.OrderBy(x => x.Key)) {
                        ct.WriteLine($"    &bJava {kv.Key,-2} &8→ &7{kv.Value}");
                    }
                }

                string runtimeDir = PathDef.Default.CacheRuntimeDirectory;
                if (Directory.Exists(runtimeDir)) {
                    List<string> bundled = Directory.GetDirectories(runtimeDir)
                        .Select(Path.GetFileName)
                        .Where(n => !string.IsNullOrEmpty(n))
                        .OrderBy(n => n)
                        .ToList()!;
                    if (bundled.Count > 0) {
                        ct.WriteLine($"  &8已缓存捆绑运行时: &b{string.Join("&8, &b", bundled)} &8({Path.GetFullPath(runtimeDir)})");
                    }
                }
                break;
            }

            case "set": {
                if (!Tools.CheckParamLength(args, 3, ct)) return;

                if (args.Length >= 5 && uint.TryParse(args[3], out uint major)) {
                    string path = args.Length > 5 ? string.Join(' ', args[4..]) : args[4];
                    JavaHelper.JavaRuntimeInfo? info = JavaHelper.ProbeHomeAsync(path).GetAwaiter().GetResult();
                    if (info == null) {
                        ct.WriteLine($"&e警告: 未能在 &7{path} &e探测到有效 Java 可执行文件, 仍将保存配置", Terminal.MessageType.Warn);
                    }
                    cfg.Launch.JavaVault[major] = path;
                    cfg.Save();
                    ct.WriteLine($"&a已设置 Java &b{major} &a路径 &8[&7{path}&8]");
                } else {
                    string path = args.Length > 4 ? string.Join(' ', args[3..]) : args[3];
                    JavaHelper.JavaRuntimeInfo? info = JavaHelper.ProbeHomeAsync(path).GetAwaiter().GetResult();
                    if (info == null) {
                        ct.WriteLine($"&e警告: 未能在 &7{path} &e探测到有效 Java 可执行文件, 仍将保存配置", Terminal.MessageType.Warn);
                    }
                    cfg.Launch.JavaHome = path;
                    cfg.Save();
                    ct.WriteLine($"&a已设置默认 Java 路径 &8[&7{path}&8]");
                }
                break;
            }

            case "clear": {
                if (args.Length > 3) {
                    if (!uint.TryParse(args[3], out uint major)) {
                        ct.WriteLine($"&c无效的 Java 主版本号&8[&c{args[3]}&8]", Terminal.MessageType.Error);
                        return;
                    }
                    if (cfg.Launch.JavaVault.Remove(major)) {
                        cfg.Save();
                        ct.WriteLine($"&a已清除 Java &b{major} &a映射");
                    } else {
                        ct.WriteLine($"&7未找到 Java &b{major} &7映射");
                    }
                } else {
                    cfg.Launch.JavaHome = null;
                    cfg.Launch.JavaVault.Clear();
                    cfg.Save();
                    ct.WriteLine("&a已清除全部自定义 Java 配置");
                }
                break;
            }

            default:
                ct.WriteLine($"&c未知操作&8[&c{sub}&8] &7可用项&8[&7scan, list, set, clear&8]", Terminal.MessageType.Error);
                break;
        }
    }

    sealed class ParsedArgs {
        public List<string> Positionals { get; } = [];
        public LaunchMode Mode { get; set; } = LaunchMode.Managed;
        public bool FullCheck { get; set; }
        public bool Clean { get; set; }
        public bool Yes { get; set; }
        public string? Server { get; set; }
        public uint? Memory { get; set; }
        public string? JavaHome { get; set; }
        public string? User { get; set; }
    }

    static bool TryParseFlags(string[] args, int startIndex, IChainedLikeTerminal ct, out ParsedArgs result) {
        result = new ParsedArgs();

        for (int i = startIndex; i < args.Length; i++) {
            string token = args[i];

            if (!token.StartsWith('-')) {
                result.Positionals.Add(token);
                continue;
            }

            string flag = token;
            string? inlineValue = null;
            int eqIdx = token.IndexOf('=');
            if (eqIdx > 0) {
                flag = token[..eqIdx];
                inlineValue = token[(eqIdx + 1)..];
            }

            switch (flag.ToLowerInvariant()) {
                case "--bg":
                    result.Mode = LaunchMode.FireAndForget;
                    break;
                case "--debug":
                    result.Mode = LaunchMode.Debug;
                    break;
                case "--full-check":
                    result.FullCheck = true;
                    break;
                case "--clean":
                    result.Clean = true;
                    break;
                case "--yes" or "-y":
                    result.Yes = true;
                    break;
                case "--server":
                    if (!TryReadOptionValue(args, ref i, inlineValue, "--server", ct, out string? server)) return false;
                    result.Server = server;
                    break;
                case "--java":
                    if (!TryReadOptionValue(args, ref i, inlineValue, "--java", ct, out string? java)) return false;
                    result.JavaHome = java;
                    break;
                case "--user":
                    if (!TryReadOptionValue(args, ref i, inlineValue, "--user", ct, out string? user)) return false;
                    result.User = user;
                    break;
                case "--memory": {
                    if (!TryReadOptionValue(args, ref i, inlineValue, "--memory", ct, out string? memRaw)) return false;
                    if (!uint.TryParse(memRaw, out uint mem) || mem == 0) {
                        ct.WriteLine($"&c无效的 --memory 参数值&8[&c{memRaw}&8]", Terminal.MessageType.Error);
                        return false;
                    }
                    result.Memory = mem;
                    break;
                }
                default:
                    ct.WriteLine($"&c未知选项&8[&c{token}&8]", Terminal.MessageType.Error);
                    return false;
            }
        }

        return true;
    }

    static bool TryReadOptionValue(
        string[] args,
        ref int index,
        string? inlineValue,
        string optionName,
        IChainedLikeTerminal ct,
        out string? value) {
        if (inlineValue != null) {
            value = inlineValue;
            return true;
        }

        if (index + 1 >= args.Length) {
            ct.WriteLine($"&c选项 {optionName} 缺少参数值", Terminal.MessageType.Error);
            value = null;
            return false;
        }

        value = args[++index];
        return true;
    }
}
