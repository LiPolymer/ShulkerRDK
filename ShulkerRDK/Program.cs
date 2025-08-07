using System.Reflection;
using System.Text.Json;
using ShulkerRDK.CoreExtension;
using ShulkerRDK.Shared;

namespace ShulkerRDK;

static class Program {
    public static void Main(string[] args) {
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler;
        Terminal.Init(new LegacyTerminal());
        if (!File.Exists(StaticContext.Paths.ProjectConfig)) {
            InitProject();
            return;
        }
        if (!File.Exists(StaticContext.Paths.LocalConfig)) {
            if (args.Length > 0) {
                Context.LocalConfig = new LocalConfig() {
                    TerminalMode = "legacy"
                };
                NugetHelper.IsProgressBarEnabled = false;
            } else {
                InitLocalConfig();
                return;
            }
        } else {
            Context.LocalConfig = JsonSerializer.Deserialize<LocalConfig>(File.ReadAllText(StaticContext.Paths.LocalConfig));
        }
        
        Terminal.Init(Context.LocalConfig!.TerminalMode switch {
            "modern" => new AnsiTerminal(),
            "legacy" => new LegacyTerminal(),
            "mono" => new MonoTerminal(),
            _ => throw new ArgumentOutOfRangeException($"未知的终端实现类型[{Context.LocalConfig!.TerminalMode}]")
        });
        Terminal.WriteLine(Extension.AsciiArtStatic);
        Terminal.WriteLine($"[&l&5Shulker&6RDK&r] &7{Extension.VersionStatic}   &8正在启动");
        Context.ProjectConfig = ProjectConfig.Load();
        Terminal.WriteLine("&l&3Core",$"&3当前项目&8[&b{Context.ProjectConfig!.ProjectName}&8]");
        Terminal.WriteLine("&l&3Core","&7正在载入核心扩展");
        RegisterExtension(Context, new CoreExtension.Extension());
        Terminal.WriteLine("&l&bExtension","&7开始加载外置扩展");
        
        // 定位插件依赖于此处
        AppDomain.CurrentDomain.AssemblyResolve += (_, resolveEventArgs) => {
            string assemblyPath = Path.Combine(Path.GetFullPath("./shulker/local/libs"), new AssemblyName(resolveEventArgs.Name).Name + ".dll");
            return File.Exists(assemblyPath) ? Assembly.LoadFile(assemblyPath) : null;
        };
        
        LoadExtensions(Context);
        Terminal.WriteLine("&l&bExtension",$"&7完成!&8[&7{Context.Extensions.Count - 1}&8]&7个外置扩展已载入!");
        
        InjectAliasFromVarTable(Context.CommandAliases,"alias.command.",Context.ProjectConfig.DefaultEnvVars);
        InjectAliasFromVarTable(Context.StartActionAliases,"alias.startAction.",Context.ProjectConfig.DefaultEnvVars);

        Context.ProjectConfig.OnPropertyChanged += Context.ProjectConfig.Save;
        
        ActiveExtensions();
        
        if (args.Length > 0) {
            Terminal.WriteLine("&l&3Core","&eShulkerRDK载入完成!");
            args = Tools.AliasResolver(args,Context.StartActionAliases);
            if (Context.StartActions.TryGetValue(args[0],out Action<string[],ShulkerContext>? action)) {
                action(args,Context);
            } else {
                Terminal.WriteLine("&l&3Core",$"&c未能解析这个启动参数&8[&4{args[0]}&8]",Terminal.MessageType.Critical);
            }
        } else {
            Terminal.WriteLine("&l&3Core","&eShulkerRDK载入完成! &8使用&o help c &r&8查看所有可用命令");
            InteractLoop();
        }
    }

    static void GlobalExceptionHandler(object sender, UnhandledExceptionEventArgs ea) {
        Exception e = (Exception)ea.ExceptionObject;
        ChainedTerminal ct = new ChainedTerminal("&l&4CRASHED");
        Tools.DisplayException(e,ct);
        #if !DEBUG
        try {
            string logPath = $"./shulker/local/crash_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
            if (!Directory.Exists(Path.GetDirectoryName(logPath))) {
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            }
            string logEntry = $"ShulkerRDK CrashReport\n[{DateTime.Now:yyyy-MM-dd HH:mm:ss}][CRASHED] {e.GetType().Name}: {e.Message}\n{e.StackTrace}";
            File.WriteAllText(logPath, logEntry);
            ct.WriteLine($"&7详细错误信息已记录到&8[&7{logPath}&8]", Terminal.MessageType.Critical);
        } catch { 
            //ignored
        }
        #endif
        ct.WriteLine("&7按任意键退出...", Terminal.MessageType.Critical);
        Console.ReadKey();
        Environment.Exit(1);
    }

    static void InjectAliasFromVarTable(Dictionary<string,string> aliasDict, string prefix, Dictionary<string,string> table) {
        Dictionary<string,string> injection = [];
        foreach (KeyValuePair<string,string> kvp in table.Where(kvp => kvp.Key.StartsWith(prefix))) {
            injection.Add(kvp.Key[prefix.Length..],kvp.Value);
            Terminal.WriteLine("&7AliasInjector",$" &7{kvp.Key[prefix.Length..]}&8>&7{kvp.Value}",Terminal.MessageType.Debug);
        }
        Tools.MergeDict(aliasDict,injection);
    }
    
    static void InteractLoop() {
        while (true) {
            Terminal.Write("&6>&e");
            string cmd = Console.ReadLine()!;
            Terminal.Write("&r");
            string[] cmdArg = Tools.ResolveArgs(cmd);
            if (cmdArg.Length == 0) continue;
            if (!cmd.StartsWith("env")) {
                cmd = Tools.AliasResolver(cmd,Context.CommandAliases);
            }
            RunCommand(cmd,Context);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    static void RunCommand(string cmd,ShulkerContext context) {
        RunCommand(Tools.ResolveArgs(cmd),context);
    }
    public static void RunCommand(string[] args, ShulkerContext context) {
        if (Context.Commands.TryGetValue(args[0],out Action<string[],ShulkerContext>? act)) {
            act.Invoke(args,Context);
        } else {
            Terminal.WriteLine("",$"&7未知的指令&8[&c{args[0]}&8]", Terminal.MessageType.Warn);
        }
    }

    public static ShulkerContext Context = new ShulkerContext();

    static void InitProject() {
        Terminal.WriteLine("&4&l这不是一个Shulker项目&r &8如果您想初始化项目,请输入[&7init&8]");
        Terminal.Write("&8>&r");
        if (Console.ReadLine() != "init") Environment.Exit(0);
        Terminal.WriteLine("请输入您的项目名");
        Terminal.Write("&8>&r");
        string? projName = Console.ReadLine();
        if (projName == null | projName == "") projName = "New Project";
        Terminal.WriteLine("请输入您项目的内容根目录 &8(默认为[&7./src&8])");
        Terminal.Write("&8>&r");
        string? rootPath = Console.ReadLine();
        if (rootPath == null | rootPath == "") rootPath = "./src/";
        Terminal.WriteLine("正在为您创建项目配置...");
        ProjectConfig projConfig = new ProjectConfig {
            ProjectName = projName!,
            RootPath = rootPath!,
            OutPath = "./build/"
        };
        
        string projConfigContent = JsonSerializer.Serialize(projConfig,Tools.JsonSerializerOptions);
        Tools.WriteAllText(StaticContext.Paths.ProjectConfig, projConfigContent);
        Main([]);
    }
    static void InitLocalConfig() {
        Terminal.WriteLine("","&e您还没有配置本地设置,将引导您进行配置");
        Terminal.WriteLine("","&c请注意 在继续之前,请确认您信任扩展文件夹内的扩展,它们可以&e以ShulkerRDK的身份&c执行&e任何操作"
                           ,Terminal.MessageType.Warn);
        Terminal.WriteLine("","&c继续使用则视为您已确认您信任所有将会加载的扩展,&7ShulkerRDK开发者及贡献者&c将不对您的计算机系统安全负责"
                           ,Terminal.MessageType.Warn);
        Terminal.WriteLine("","&7否则请在进行下一步之前使用&8[&7Ctrl&8+&7C&8]&7或其他方法退出"
                           ,Terminal.MessageType.Warn);
        Terminal.WriteLine("","&7下面将输出一段彩色日志,请检查是否正确显示");
        ConsoleColor defaultColor = Console.ForegroundColor;
        Terminal.Init(new AnsiTerminal());
        Terminal.WriteLine("&3Tester","&9G&co&6o&9g&2l&ce");
        Terminal.Init(new LegacyTerminal());
        Terminal.Write("是否正常显示?&8[&7y&8/&7n&8]&6");
        string? input = Console.ReadLine();
        Console.ForegroundColor = defaultColor;
        LocalConfig localConfig = new LocalConfig {
            TerminalMode = input switch {
                "y" => "modern",
                _ => "legacy"
            }
        };
        Tools.WriteAllText(StaticContext.Paths.LocalConfig, JsonSerializer.Serialize(localConfig,Tools.JsonSerializerOptions));
        Main([]);
    }
    
    static Assembly LoadAssembly(string relativePath) {
        string extensionLocation = Path.GetFullPath(relativePath.Replace('\\',Path.DirectorySeparatorChar));
        ExtensionLoadContext loadContext = new ExtensionLoadContext(extensionLocation);
        return loadContext.LoadFromAssemblyName(AssemblyName.GetAssemblyName(extensionLocation));
    }
    static IShulkerExtension GetIExtension(Assembly assembly) {
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (Type type in assembly.GetTypes()) {
            if (!typeof(IShulkerExtension).IsAssignableFrom(type)) continue;
            return (Activator.CreateInstance(type) as IShulkerExtension)!;
        }
        throw new Exception("未能作为扩展加载程序集");
    }
    static void LoadExtensions(ShulkerContext context, string extensionsPath = "./shulker/extensions") {
        if (!Directory.Exists(extensionsPath)) {
            Directory.CreateDirectory(extensionsPath);
        }
        string[] files = Directory.GetFiles(extensionsPath);
        
        List<string> fileList = files.ToList();
        
        #if DEBUG
        fileList.Add(@"..\..\..\..\TestExtension\bin\Debug\net8.0\TestExtension.dll");
        fileList.Add(@"..\..\..\..\ShulkerRDK.ResourceMagick\bin\Debug\net8.0\ShulkerRDK.ResourceMagick.dll");
        fileList.Add(@"..\..\..\..\ShulkerRDK.Modrinth\bin\Debug\net8.0\ShulkerRDK.Modrinth.dll");
        fileList.Add(@"..\..\..\..\ShulkerRDK.RRT\bin\Debug\net8.0\ShulkerRDK.RRT.dll");
        #endif
        
        files = fileList.ToArray();
        
        foreach (string file in files) {
            try {
                if (!file.EndsWith(".dll")) continue;
                Terminal.WriteLine("&l&bExtension",$"&7正在载入&8[&7{Path.GetFileName(file)}&8]");
                IShulkerExtension iExtension = GetIExtension(LoadAssembly(file));
                RegisterExtension(context, iExtension);
                
                Terminal.WriteLine("&l&bExtension",$"&8[&7{iExtension.Name}&8@{iExtension.Version}]&a载入完成!");
            }
            catch (Exception e) {
                Terminal.WriteLine("&l&bExtension",e.Message,Terminal.MessageType.Error);
            }
        }
    }
    static void RegisterExtension(ShulkerContext context,IShulkerExtension extension) {
        context.Extensions.Add(extension.Id,extension);
        Tools.MergeDict(context.StartActions,extension.StartActions);
        Tools.MergeDict(context.Commands,extension.Commands);
        Tools.MergeDict(context.CommandAliases,extension.CommandAliases);
        Tools.MergeDict(context.StartActionAliases,extension.StartActionAliases);
    }   

    static void ActiveExtensions() {
        Terminal.WriteLine("&l&bExtension","&7正在激活扩展...");
        foreach (IShulkerExtension ext in Context.Extensions.Values) {
            ext.Init(Context);
        }
    }
    
    public static void UnLoadExtensions() {
        Terminal.WriteLine("&l&bExtension","&7正在关闭扩展...");
        foreach (IShulkerExtension ext in Context.Extensions.Values) {
            ext.Shutdown(Context);
        }
    }
}