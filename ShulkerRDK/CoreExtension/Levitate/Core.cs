using System.Diagnostics;
using System.Text.RegularExpressions;
using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Levitate;

public static class Core {
    static void AddAttribute(LevitateExecutionContext ec) {
        ec.Logger.AddNode("&9&oLevitate");
    }

    public static string? Run(string[] args,LevitateExecutionContext ec) {
        AddAttribute(ec);
        if (args.Length > 1) {
            string path = Path.Combine(ec.WorkingDir,args[1]);
            switch (args.Length) {
                case 3 when args[2] == "new":
                    new LevitateInterpreter(ec.ShulkerContext).RunFromFile(path);
                    break;
                case 3:
                    ec.Logger.WriteLine($"&8[&e{ec.CurrentLine}&8]&7未知的参数&8[&7{args[0]} &7{args[1]} &c{args[2]}&8]",Terminal.MessageType.Error);
                    break;
                case 2:
                    ec.Interpreter.RunFromFile(path);
                    break;
                default:
                    ec.Logger.WriteLine($"&8[&e{ec.CurrentLine}&8]&7参数错误[{args}]",Terminal.MessageType.Error);
                    break;
            }
        } else {
            ec.Logger.WriteLine($"&8[&e{ec.CurrentLine}&8]&7参数不全&8[&7{args[0]} &c__&8]",Terminal.MessageType.Error);
        }
        return null;
    }

    public static string? Import(string[] args,LevitateExecutionContext ec) {
        AddAttribute(ec);
        ec.Logger.AddNode("&7import");
        if (args.Length > 1) {
            string key = args[1];
            if (ec.ShulkerContext.Extensions.TryGetValue(key,out IShulkerExtension? ext)) {
                Tools.MergeDict(ec.Interpreter.Methods,ext.LevitateMethods);
                Tools.MergeDict(ec.Interpreter.Aliases,ext.LevitateAliases);
            } else {
                ec.Logger.WriteLine($"&c未能找到已注册的扩展&8[&c{args[1]}&8],请检查是否正确安装",Terminal.MessageType.Error);
            }
        } else {
            ec.Logger.WriteLine("&7未提供扩展ID",Terminal.MessageType.Error);
        }
        return null;
    }

    public static string? Echo(string[] args,LevitateExecutionContext ec) {
        string content = string.Empty;
        if (args.Length > 1) {
            content = args[1];
        }
        ec.Logger.WriteLine(content);
        return null;
    }

    public static string? Input(string[] args,LevitateExecutionContext ec) {
        string content = string.Empty;
        if (args.Length > 1) {
            content = args[1];
        }
        ConsoleColor color = Console.ForegroundColor;
        Terminal.Write(content);
        string? input = Console.ReadLine();
        Terminal.Write("&r");
        Console.ForegroundColor = color;
        return input;
    }

    public static string? Var(string[] args,LevitateExecutionContext ec) {
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        if (!Tools.CheckParamLength(args,2,ec)) return null;
        if (ec.Vars.TryGetValue(args[1],out string? _)) {
            ec.Vars[args[1]] = args[2];
        } else {
            ec.Vars.Add(args[1],args[2]);
        }
        return null;
    }

    public static string? EnvVars(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&9Env");
        if (!Tools.TryGetSub(["get","set"],args,1,ec)) return null;
        if (!Tools.CheckParamLength(args,2,ec)) return null;
        switch (args[1]) {
            case "get":
                if (ec.EnvVars.TryGetValue(args[2],out string? envVar)) {
                    return envVar;
                }
                ec.Logger.WriteLine($"未定义的环境变量&8[&c{args[2]}&8]",Terminal.MessageType.Warn);
                break;
            case "set":
                if (ec.EnvVars.TryGetValue(args[2],out string? _)) {
                    ec.Logger.WriteLine($"已将环境变量&8[&7{args[2]}&8]修改为&8[&7{args[3]}&8]");
                    ec.EnvVars[args[2]] = args[3];
                } else {
                    ec.Logger.WriteLine($"已将环境变量&8[&7{args[2]}&8]定义为&8[&7{args[3]}&8]");
                    ec.EnvVars.Add(args[2],args[3]);
                }
                break;
        }
        return null;
    }

    public static string? Copy(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&9Copy");
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        string src = args[1];
        bool isOverwrite = true;
        string? ignoreRegex = null;
        if (args.Length > 3) {
            if (!Tools.TryGetSub(["true","false"],args,3,ec)) return null;
            isOverwrite = args[3] switch {
                "true" => true,
                "false" => false,
                _ => isOverwrite
            };
            if (args.Length > 4) {
                ignoreRegex = args[4];
            }
        }
        string dest;
        if (Directory.Exists(src)) {
            dest = Path.GetDirectoryName(src)!;
            if (args.Length > 2) {
                dest = args[2];
            }
            ec.Logger.WriteLine($"&7正在复制&8[&7{src}&8]>[&7{dest}&8]");
            string[] files = Directory.GetFiles(src,"*",SearchOption.AllDirectories);
            foreach (string file in files) {
                if (ignoreRegex != null) {
                    try {
                        if (new Regex(ignoreRegex).IsMatch(file)) {
                            ec.Logger.WriteLine($"&7忽略&8[&7{file}&8]",Terminal.MessageType.Debug);
                            continue;
                        }
                    }
                    catch (Exception e) {
                        ec.Logger.WriteLine($"&c解析表达式时遇到问题&8[&7{e.Message}&8]");
                        return null;
                    }
                }
                ec.Logger.WriteLine($"&7正在复制&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(src,file);
                string destPath = Path.Combine(dest,relativePath);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory!);
                }
                File.Copy(file,destPath,isOverwrite);
            }
        } else if (File.Exists(src)) {
            if (ignoreRegex != null) {
                if (new Regex(ignoreRegex).IsMatch(src)) {
                    ec.Logger.WriteLine($"&7忽略&8[&7{src}&8]",Terminal.MessageType.Debug);
                    return null;
                }
            }
            ec.Logger.WriteLine($"&7正在移动&8[&7{Path.GetFileName(src)}&8]");
            dest = Path.Combine(Path.GetDirectoryName(src)!,Path.GetFileNameWithoutExtension(src) + ".png");
            if (args.Length > 2) {
                dest = args[2];
            }
            try {
                File.Copy(src,dest,true);
            } catch(Exception e) {
                if (e.HResult == -2147024816) {
                    ec.Logger.WriteLine($"&7跳过存在的&8[&7{src}&8]",Terminal.MessageType.Debug);
                } else {
                    ec.Logger.WriteLine(e.Message,Terminal.MessageType.Error);
                }
            }
        } else {
            ec.Logger.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
        return null;
    }

    public static string? Delete(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&9Del");
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        string target = args[1];
        string? targetRegex = null;
        if (args.Length > 2) {
            targetRegex = args[2];
        }
        if (Directory.Exists(target)) {
            ec.Logger.WriteLine($"&7正在删除&8[&7{target}&8]");
            if (targetRegex != null) {
                string[] files = Directory.GetFiles(target,"*",SearchOption.AllDirectories);
                foreach (string file in files) {
                    try {
                        if (new Regex(targetRegex).IsMatch(file)) {
                            ec.Logger.WriteLine($"&7忽略&8[&7{file}&8]",Terminal.MessageType.Debug);
                            continue;
                        }
                    }
                    catch (Exception e) {
                        ec.Logger.WriteLine($"&c解析表达式时遇到问题&8[&7{e.Message}&8]");
                        return null;
                    }
                    ec.Logger.WriteLine($"&7正在删除&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                    File.Delete(file);
                }
            } else {
                Directory.Delete(target,true);
            }
        } else if (File.Exists(target)) {
            ec.Logger.WriteLine($"&7正在删除&8[&7{Path.GetFileName(target)}&8]");
            File.Delete(target);
        } else {
            ec.Logger.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
        return null;
    }

    public static string? Shell(string[] args,LevitateExecutionContext ec) {
        ec.Logger.WriteLine("&cShell&8>>&r正在启动&8[&7" + args[1] + " " + string.Join(" ",Tools.GetSubGroup(args,2)) + "&8]");
        ProcessStartInfo i = new ProcessStartInfo(args[1], string.Join(" ",Tools.GetSubGroup(args,2))) {
            RedirectStandardOutput = true
        };
        Process? p = Process.Start(i);
        if (p == null) {
            ec.Logger.WriteLine("&cShell&8>>&c启动失败!",Terminal.MessageType.Error);
        }
        else {
            StreamReader sr = p.StandardOutput;
            while (!p.HasExited) {
                while (!sr.EndOfStream) {
                    ec.Logger.WriteLine(sr.ReadLine() ?? string.Empty);
                }
            }
            ec.Logger.WriteLine($"&cShell&8>>&r执行完成! 耗时&8[&7{(p.ExitTime - p.StartTime).TotalMilliseconds}&8]&7ms");
        }
        return null;
    }
    
    public static string? Flatten(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&9&oFlatten");
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        if (!Tools.CheckParamLength(args,2,ec)) return null;
        string src = args[1];
        string dest = args[2];
        bool isOverwrite = true;
        string? ignoreRegex = null;
        if (args.Length > 3) {
            if (!Tools.TryGetSub(["true","false"],args,3,ec)) return null;
            isOverwrite = args[3] switch {
                "true" => true,
                "false" => false,
                _ => isOverwrite
            };
            if (args.Length > 4) {
                ignoreRegex = args[4];
            }
        }
        if (Directory.Exists(src)) {
            ec.Logger.WriteLine($"&7正在平整&8[&7{src}&8]>[&7{dest}&8]");
            string[] files = Directory.GetFiles(src,"*",SearchOption.AllDirectories);
            foreach (string file in files) {
                if (ignoreRegex != null) {
                    try {
                        if (new Regex(ignoreRegex).IsMatch(file)) {
                            ec.Logger.WriteLine($"&7忽略&8[&7{file}&8]",Terminal.MessageType.Debug);
                            continue;
                        }
                    }
                    catch (Exception e) {
                        ec.Logger.WriteLine($"&c解析表达式时遇到问题&8[&7{e.Message}&8]");
                        return null;
                    }
                }
                ec.Logger.WriteLine($"&7正在复制&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                if (!Directory.Exists(dest)) {
                    Directory.CreateDirectory(dest);
                }
                try {
                    File.Copy(file,Path.Combine(dest,Path.GetFileName(file)),isOverwrite);
                }
                catch(Exception e) {
                    if (e.HResult == -2147024816) {
                        ec.Logger.WriteLine($"&7跳过存在的&8[&7{src}&8]",Terminal.MessageType.Debug);
                    } else {
                        ec.Logger.WriteLine(e.Message,Terminal.MessageType.Error);
                    }
                }
            }
        } else {
            ec.Logger.WriteLine("&c无效的目录",Terminal.MessageType.Error);
        }
        return null;
    }
    
    public static string? VersionControl(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&e&oVer");
        ShulkerContext sc = ec.ShulkerContext;
        if (!Tools.TryGetSub(["smajor","sminor","sfix","set","get"],args,1,ec.Logger)) return null;
        switch (args[1]) {
            case "smajor":
                sc.ProjectConfig!.Version = Tools.VersionStepper(sc.ProjectConfig!.Version,2);
                ec.Logger.WriteLine($"&a项目版本更新为&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "sminor":
                sc.ProjectConfig!.Version = Tools.VersionStepper(sc.ProjectConfig!.Version,1);
                ec.Logger.WriteLine($"&a项目版本更新为&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "sfix":
                sc.ProjectConfig!.Version = Tools.VersionStepper(sc.ProjectConfig!.Version,0);
                ec.Logger.WriteLine($"&a项目版本更新为&8[&7{sc.ProjectConfig!.Version}&8]");
                break;
            case "set":
                if (!Tools.CheckParamLength(args,2,ec.Logger)) return null;
                sc.ProjectConfig!.Version = args[2];
                ec.Logger.WriteLine($"&a已将项目版本修改为&8[&7{args[2]}&8]");
                break;
            case "get":
                if (!Tools.CheckParamLength(args,2,ec.Logger)) return null;
                try {
                    return sc.ProjectConfig!.Version.Split('.')[^(Convert.ToInt32(args[2]) + 1)];
                }
                catch(Exception e) {
                    ec.Logger.WriteLine($"&c{e.Message}",Terminal.MessageType.Error);
                    return null;
                }
        }
        ec.EnvVars["project.ver"] = sc.ProjectConfig!.Version;
        return null;
    }

    public static string? NetFile(string[] args,LevitateExecutionContext ec) {
        return null;
    }
}