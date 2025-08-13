using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension;

public class Extension : ExtensionBase {
    public Extension() {
        ////指令
        //核心
        Commands.Add("exit",Command.Core.Exit);
        Commands.Add("env",Command.Core.EnvVar);
        Commands.Add("clear",Command.Core.Clear);
        Commands.Add("help",Command.Core.Help);
        Commands.Add("proj",Command.Core.Project);
        Commands.Add("verm",Command.Core.VersionControl);
        Commands.Add("netfile",Command.Core.NetFile);
        //独立
        Commands.Add("ext",Command.ExtensionManager.Command);
        Commands.Add("task",Command.TaskManager.Command);
        //这个位置是故意的
        Commands.Add("reload",Command.Core.Reload);
        
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
        LevitateMethods.Add("verm",Levitate.Core.VersionControl);
        LevitateMethods.Add("netfile",Levitate.Core.NetFile);
        LevitateMethods.Add("check",Levitate.Core.Check);
        LevitateMethods.Add("path",Levitate.Core.PathUtil);
        LevitateMethods.Add("list",Levitate.Core.ListResolver);
        LevitateMethods.Add("regex",Levitate.Core.RegexResolver);
        LevitateMethods.Add("not",Levitate.Core.Not);
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
        LevitateAliases.Add("^makePkg$","pkgr zip make \"%project.cache%\" \"%project.output%%project.name%_%project.ver%.zip\"");
    }
    
    public override string Id { get => "shulker.core"; }
    public override string Name { get => "ShulkerRDK"; }
    public override string Description { get => "built-in toolset of ShulkerRDK"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => VersionStatic; }
    public static string VersionStatic { get => "B0.12"; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public override string Donating { get => DonatingStatic; }
    public static string DonatingStatic { get => "https://afdian.tv/a/lipolymer"; }
    public override string AsciiArt { get => AsciiArtStatic; }

    public static string AsciiArtStatic { get => "&l" + """
                                                         &5 _____ _       _ _          &6 _____ ____  _____ 
                                                         &5|   __| |_ _ _| | |_ ___ ___&6| __  |    \|  |  |
                                                         &5|__   |   | | | | '_| -_|  _&6|    -|  |  |    -|
                                                         &5|_____|_|_|___|_|_,_|___|_| &6|__|__|____/|__|__|
                                                         """ + "&r";}
}