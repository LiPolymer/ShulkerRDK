using System.Runtime.InteropServices;

namespace ShulkerRDK.Shared;

public class LevitateInterpreter {
    public LevitateInterpreter(ShulkerContext shulkerContext) {
        _shulkerContext = shulkerContext;
        IShulkerExtension core = _shulkerContext.Extensions["shulker.core"];
        Tools.MergeDict(Methods,core.LevitateMethods);
        Tools.MergeDict(Aliases,core.LevitateAliases);
        EnvVars.Add("project.src",shulkerContext.ProjectConfig!.RootPath);
        EnvVars.Add("project.name",shulkerContext.ProjectConfig!.ProjectName);
        EnvVars.Add("project.output",shulkerContext.ProjectConfig!.OutPath);
        EnvVars.Add("project.ver",shulkerContext.ProjectConfig!.Version);
        EnvVars.Add("project.cache","./shulker/local/cache/build");
        foreach (KeyValuePair<string,string> kvp in shulkerContext.ProjectConfig!.DefaultEnvVars) {
            if (EnvVars.ContainsKey(kvp.Key)) {
                Terminal.WriteLine("&9&oLevitate",$"自动环境变量&8[&7{kvp.Key}&8]&r被项目环境变量覆写");
                EnvVars[kvp.Key] = kvp.Value;
            } else {
                EnvVars.Add(kvp.Key,kvp.Value);
            }
        }
    }

    readonly ShulkerContext _shulkerContext;
    public readonly Dictionary<string,LevitateMethod> Methods = [];
    public readonly Dictionary<string,string> Aliases = [];
    public readonly Dictionary<string,string> EnvVars = [];
    readonly Dictionary<string,string> _vars = [];

    public void RunFromFile(string path) {
        Run(File.ReadAllLines(path),System.IO.Path.GetDirectoryName(path)!,System.IO.Path.GetFileNameWithoutExtension(path));
    }

    void Run(string[] sentences,string workingDir = "./",string name = "???",bool catchExecutionError = true) {
        CountExecution();
        int index = 0;
        foreach (string sentence in sentences) {
            try {
                index++;
                if (sentence.StartsWith('#') | sentence.StartsWith(' ') | sentence == "") continue;

                //// 处理插入内容
                // 注入变量
                string postVariable = Tools.EscapeDictResolver(sentence,_vars,"^");
                // 注入别名
                postVariable = Tools.AliasResolver(postVariable,Aliases);
                // 注入环境变量
                postVariable = Tools.EscapeDictResolver(postVariable,EnvVars,"%");
                // 注入表达式
                int myIndex = index;
                string expression = Tools.CrateReplacer(postVariable,"{","}",s =>
                                                            ExecuteMethod(s,new LevitateExecutionContext {
                                                                WorkingDir = workingDir,
                                                                CurrentLine = myIndex,
                                                                Logger = new LevitateLogger(myIndex,name),
                                                                ShulkerContext = _shulkerContext,
                                                                Interpreter = this,
                                                                EnvVars = EnvVars,
                                                                Vars = _vars
                                                            }) ?? "null");
                //// 执行
                ExecuteMethod(expression,new LevitateExecutionContext {
                    WorkingDir = workingDir,
                    CurrentLine = index,
                    Logger = new LevitateLogger(index,name),
                    ShulkerContext = _shulkerContext,
                    Interpreter = this,
                    EnvVars = EnvVars,
                    Vars = _vars
                });
            }
            catch (Exception e) {
                if (catchExecutionError) {
                    LevitateLogger ll = new LevitateLogger(index,name);
                    Tools.DisplayException(e,ll,Terminal.MessageType.Error);
                    ll.WriteLine($"&7由&8[&7{sentence}&8]&7引发",Terminal.MessageType.Error);
                    Terminal.WriteLine("&9&oLevitate","&c已中断脚本执行",Terminal.MessageType.Error);
                } else {
                    throw;
                }
                break;
            }
        }
    }
    
    string? ExecuteMethod(string methodString,LevitateExecutionContext ec) {
        //Tokenize并执行
        return ExecuteMethod(Tools.ResolveArgs(methodString),ec);
    }

    public string? ExecuteMethod(string[] command,LevitateExecutionContext ec) {
        if (command.Length == 0) return null;
        if (Methods.TryGetValue(command[0],out LevitateMethod? method)) {
            return method(command,ec);
        }
        ec.Logger.AddNode("&9&oLevitate");
        ec.Logger.WriteLine($"&c未能找到已注册的方法&8[&c{command[0]}&8]",Terminal.MessageType.Error);
        return null;
    }
    
    static readonly string CheckpointPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\ShulkerRDK\executionCount.dat";
    static void CountExecution() {
        try {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;
            int count = 0;
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(CheckpointPath))) {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(CheckpointPath)!);
            }
            string[] lines = ["0","0"];
            if (File.Exists(CheckpointPath)) {
                lines = File.ReadAllLines(CheckpointPath);
                count = Convert.ToInt32(lines[0]);
            }
            if (count < int.MaxValue - 10) {
                count++;
            }
            lines[0] = count.ToString();
            File.WriteAllLines(CheckpointPath,lines);
        }
        catch {
            //ignored
        }
    }
}

public class LevitateExecutionContext {
    public required string WorkingDir;
    public required int CurrentLine;
    public required LevitateLogger Logger;
    public required ShulkerContext ShulkerContext;
    public required LevitateInterpreter Interpreter;
    public required Dictionary<string,string> EnvVars;
    public required Dictionary<string,string> Vars;
}

public delegate string? LevitateMethod(string[] line,LevitateExecutionContext ec);

public class LevitateLogger(int index,string name) : IChainedLikeTerminal {
    readonly ChainedTerminal _instance = new ChainedTerminal($"&6{name}&8[&7{index}&8]");

    public void WriteLine(string content,Terminal.MessageType type = Terminal.MessageType.Info) {
        _instance.WriteLine(content,type);
    }

    public void AddNode(string node) {
        _instance.AddNode(node);
    }
}