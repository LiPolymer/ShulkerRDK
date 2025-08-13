using System.ComponentModel;
using ShulkerRDK.CoreExtension.Shared;
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
                    logger.WriteLine($"&7项目环境变量&8[&7{args[2]}&8]>[&7{envVar}&8]");
                } else {
                    logger.WriteLine($"&7未定义的项目环境变量&8[&c{args[2]}&8]",Terminal.MessageType.Warn);   
                }
                return;
            case "set":
                if (!Tools.CheckParamLength(args,3,logger)) return;
                if (sc.ProjectConfig!.DefaultEnvVars.TryGetValue(args[2],out string? _)) {
                    logger.WriteLine($"&7已将项目环境变量&8[&7{args[2]}&8]&7修改为&8[&7{args[3]}&8]");
                    sc.ProjectConfig!.DefaultEnvVars[args[2]] = args[3];
                } else {
                    logger.WriteLine($"&8[&7{args[2]}&8]&7定义为&8[&7{args[3]}&8]");
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
                logger.WriteLine("&7所有项目环境变量:");
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
    [Description("项目版本管理")]
    public static void VersionControl(string[] args,ShulkerContext sc) {
        ChainedTerminal logger = new ChainedTerminal("&e&oVer");
        if (!Tools.TryGetSub(["show","smajor","sminor","sfix","set"],args,1,logger)) return;
        switch (args[1]) {
            case "show":
                logger.WriteLine($"&7当前版本号&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "smajor":
                sc.ProjectConfig!.Version = Tools.VersionStepper(sc.ProjectConfig!.Version,2);
                logger.WriteLine($"&a项目版本更新为&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "sminor":
                sc.ProjectConfig!.Version = Tools.VersionStepper(sc.ProjectConfig!.Version,1);
                logger.WriteLine($"&a项目版本更新为&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "sfix":
                sc.ProjectConfig!.Version = Tools.VersionStepper(sc.ProjectConfig!.Version,0);
                logger.WriteLine($"&a项目版本更新为&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "set":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                sc.ProjectConfig!.Version = args[2];
                logger.WriteLine($"&a已将项目版本修改为&8[&7{args[2]}&8]");
                break;
        }
    }
    [Description("管理网络链接文件")]
    public static void NetFile(string[] args,ShulkerContext sc) {
        ChainedTerminal logger = new ChainedTerminal("&eNetFile");
        if (!Tools.TryGetSub(["create","clean","restore"],args,1,logger)) return;
        switch (args[1]) {
            case "create":
                if (!Tools.CheckParamLength(args,2,logger)) return;
                if (!Tools.CheckParamLength(args,3,logger)) return;
                NetworkFile.Create(args[2],args[3],logger);
                break;
            case "clean":
                logger.WriteLine("&7正在清理缓存文件...");
                if (Directory.Exists(NetworkFile.LocalPath)) {
                    Directory.Delete(NetworkFile.LocalPath,true);
                }
                logger.WriteLine("&7完成!");
                break;
            case "restore":
                string from = Tools.CheckParamLength(args,2) ? args[2] : sc.ProjectConfig!.RootPath;
                bool isOutMissing = !Tools.CheckParamLength(args,2);
                string to = !isOutMissing ? args[3] : from;
                logger.WriteLine("&6警告 此操作将转化链接文件为真实文件,不可撤销");
                Terminal.Write("&7是否继续? &8[&7y&8/&7n&8] &7");
                string? csr = Console.ReadLine();
                if (csr != "y") return;
                NetworkFile.Restore(from,to,logger,isOutMissing);
                break;
        }
    }
}