using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension;

public class Extension : IShulkerExtension {
    public Extension() {
        ////指令
        //核心
        Commands.Add("exit",Command.Core.Exit);
        Commands.Add("reload",Command.Core.Reload);
        Commands.Add("env",Command.Core.EnvVar);
        Commands.Add("clear",Command.Core.Clear);
        //独立
        Commands.Add("ext",Command.ExtensionManager.Command);
        Commands.Add("task",Command.TaskManager.Command);
        
        ////Levitate方法
        //核心
        LevitateMethods.Add("run",Levitate.Core.Run);
        LevitateMethods.Add("import",Levitate.Core.Import);
        LevitateMethods.Add("echo",Levitate.Core.Echo);
        LevitateMethods.Add("input",Levitate.Core.Input);
        LevitateMethods.Add("var",Levitate.Core.Var);
        LevitateMethods.Add("env",Levitate.Core.EnvVars);
        LevitateMethods.Add("copy",Levitate.Core.Copy);
        LevitateMethods.Add("delete",Levitate.Core.Delete);
        LevitateMethods.Add("sh",Levitate.Core.Shell);
        LevitateMethods.Add("flat",Levitate.Core.Flatten);
        //独立
        LevitateMethods.Add("pkgr",Levitate.Packager.Method);
        
        ////启动行动
        StartActions.Add("c",StartupAction.Core.Commander);
        
        ////别名
        //启动行动
        StartActionAliases.Add("^task","c task ");
        StartActionAliases.Add("^build","c build ");
        StartActionAliases.Add("^publish","c publish ");
        StartActionAliases.Add("^run","c run ");
        
        //指令
        CommandAliases.Add("^build","task build ");
        CommandAliases.Add("^publish","task publish ");
        CommandAliases.Add("^run","task run ");
        CommandAliases.Add("^dev","task dev ");
        
        //Levitate方法
        LevitateAliases.Add("^makeCleanup$","delete \"%project.cache%\"");
        LevitateAliases.Add("^makeCopy","copy \"%project.src%\" \"%project.cache%\" true ");
        LevitateAliases.Add("^makePkg$","pkgr zip make \"%project.cache%\" \"%project.output%%project.name%.zip\"");
    }
    
    public string Id { get => "shulker.core"; }
    public string Name { get => "ShulkerRDK"; }
    public string Description { get => "built-in toolset of ShulkerRDK"; }
    public string Author { get => "LiPolymer"; }
    public string Version { get => VersionStatic; }
    public static string VersionStatic { get => "Dev"; }
    public string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public string AsciiArt { get => AsciiArtStatic; }

    public static string AsciiArtStatic { get => "&l" + """
                                                         &5 _____ _       _ _          &6 _____ ____  _____ 
                                                         &5|   __| |_ _ _| | |_ ___ ___&6| __  |    \|  |  |
                                                         &5|__   |   | | | | '_| -_|  _&6|    -|  |  |    -|
                                                         &5|_____|_|_|___|_|_,_|___|_| &6|__|__|____/|__|__|
                                                         """ + "&r";}
    
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,LevitateMethod> LevitateMethods { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
    public Dictionary<string,string> LevitateAliases { get; } = [];
    public void Init(ShulkerContext context) {
        
    }
    public void Shutdown(ShulkerContext context) {
        
    }
}