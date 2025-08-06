using System.ComponentModel;
using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Command;

public static class Core {
    [Description("退出ShulkerRDK")]
    public static void Exit(string[] args,ShulkerContext sc) {
        Program.UnLoadExtensions();
        Terminal.WriteLine("","&7正在退出...");
        Environment.Exit(0);
    }
    [Description("重载ShulkerRDK(不推荐使用)")]
    public static void Reload(string[] args,ShulkerContext sc) {
        Terminal.WriteLine("","&c重载可能会导致未知的异常 请不要向开发者报告",Terminal.MessageType.Warn);
        Program.UnLoadExtensions();
        Terminal.WriteLine("","&c正在清理上下文...",Terminal.MessageType.Warn);
        Program.Context = new ShulkerContext();
        Terminal.WriteLine("","&c正在启动...",Terminal.MessageType.Warn);
        Program.Main(Tools.GetSubGroup(args,1));
    }
    [Description("环境变量")]
    public static void EnvVar(string[] args,ShulkerContext sc) {
        ChainedTerminal logger = new ChainedTerminal("&9Env");
        if (!Tools.TryGetSub(["get","set","remove","list"],args,1,logger)) return;
        switch (args[1]) {
            case "get":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                if (sc.ProjectConfig!.DefaultEnvVars.TryGetValue(args[2],out string? envVar)) {
                    logger.WriteLine($"项目环境变量&8[&7{args[2]}&8]>[&7{envVar}&8]");
                } else {
                    logger.WriteLine($"未定义的项目环境变量&8[&c{args[2]}&8]",Terminal.MessageType.Warn);   
                }
                return;
            case "set":
                if (!Tools.CheckParamLength(args,3,logger)) return;
                if (sc.ProjectConfig!.DefaultEnvVars.TryGetValue(args[2],out string? _)) {
                    logger.WriteLine($"已将项目环境变量&8[&7{args[2]}&8]&7修改&8为[&7{args[3]}&8]");
                    sc.ProjectConfig!.DefaultEnvVars[args[2]] = args[3];
                } else {
                    logger.WriteLine($"已将项目环境变量&8[&7{args[2]}&8]定义为[&7{args[3]}&8]");
                    sc.ProjectConfig!.DefaultEnvVars.Add(args[2],args[3]);
                }
                sc.ProjectConfig!.Save();
                return;
            case "remove":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                sc.ProjectConfig!.DefaultEnvVars.Remove(args[2]);
                sc.ProjectConfig!.Save();
                return;
            case "list":
                logger.WriteLine("所有项目环境变量:");
                foreach (KeyValuePair<string,string> kvp in sc.ProjectConfig!.DefaultEnvVars) {
                    Terminal.WriteLine("",$" - &8[&7{kvp.Key}&8]>[&7{kvp.Value}&8]");
                }
                return;
        }
    }
    [Description("清屏")]
    public static void Clear(string[] args,ShulkerContext sc) {
        Console.Clear();
    }
    [Description("显示指令帮助")]
    public static void Help(string[] args,ShulkerContext sc) {
        ChainedTerminal logger = new ChainedTerminal("&7Help");
        if (!Tools.TryGetSub(["commands","alias","c","a"],args,1,logger)) return;
        switch (args[1]) {
            case "commands" or "c":
                logger.WriteLine("&6所有指令");
                foreach (KeyValuePair<string,Action<string[],ShulkerContext>> kvp in sc.Commands) {
                    logger.WriteLine($"&7{kvp.Key} &8{Tools.GetDescriptionAttribute(kvp.Value)}");
                }
                break;
            case "alias" or "a":
                logger.WriteLine("&6所有别名");
                foreach (KeyValuePair<string,string> kvp in sc.CommandAliases) {
                    logger.WriteLine($"&7{kvp.Key} &8=> {kvp.Value}");
                }
                break;
        }
    }
    [Description("项目设定管理")]
    public static void Project(string[] args,ShulkerContext sc) {
        ChainedTerminal logger = new ChainedTerminal("&a&oProj");
        if (!Tools.TryGetSub(["info","chname","chroot","chout","i"],args,1,logger)) return;
        switch (args[1]) {
            case "info" or "i":
                logger.WriteLine($"&7{sc.ProjectConfig!.ProjectName}&8@{sc.ProjectConfig.Version}");
                logger.WriteLine($"&7项目资源根&8[&7{sc.ProjectConfig.RootPath}&8]");
                logger.WriteLine($"&7项目输出目录&8[&7{sc.ProjectConfig.OutPath}&8]");
                break;
            case "chname":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                sc.ProjectConfig!.ProjectName = args[2];
                logger.WriteLine($"&a已将项目名修改为&8[&7{args[2]}&8]");
                break;
            case "chroot":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                sc.ProjectConfig!.RootPath = args[2];
                logger.WriteLine($"&a已将项目资源根修改为&8[&7{args[2]}&8]");
                break;
            case "chout":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                sc.ProjectConfig!.OutPath = args[2];
                logger.WriteLine($"&a已将项目输出目录修改为&8[&7{args[2]}&8]");
                break;
        }
    }
}