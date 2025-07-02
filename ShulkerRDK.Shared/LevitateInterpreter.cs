namespace ShulkerRDK.Shared;

public class LevitateInterpreter {
    public LevitateInterpreter(ShulkerContext shulkerContext) {
        _shulkerContext = shulkerContext;
        IShulkerExtension core = _shulkerContext.Extensions["shulker.core"];
        Tools.MergeDict(Methods,core.LevitateMethods);
        Tools.MergeDict(Aliases,core.LevitateAliases);
        _envVars.Add("project.src",shulkerContext.ProjectConfig!.RootPath);
        _envVars.Add("project.name",shulkerContext.ProjectConfig!.ProjectName);
        _envVars.Add("project.output",shulkerContext.ProjectConfig!.OutPath);
        _envVars.Add("project.cache","./shulker/local/cache/build");
        foreach (KeyValuePair<string,string> kvp in shulkerContext.ProjectConfig!.DefaultEnvVars) {
            if (_envVars.ContainsKey(kvp.Key)) {
                Terminal.WriteLine("&9&oLevitate",$"自动环境变量&8[&7{kvp.Key}&8]&r被项目环境变量覆写");
                _envVars[kvp.Key] = kvp.Value;
            } else {
                _envVars.Add(kvp.Key, kvp.Value);   
            }
        }
    }

    readonly ShulkerContext _shulkerContext;
    public readonly Dictionary<string,LevitateMethod> Methods = [];
    public readonly Dictionary<string,string> Aliases = [];
    readonly Dictionary<string,string> _envVars = [];
    readonly Dictionary<string,string> _vars = [];

    public void RunFromFile(string path) {
        Run(File.ReadAllLines(path),Path.GetDirectoryName(path)!,Path.GetFileNameWithoutExtension(path));
    }

    void Run(string[] sentences,string workingDir = "./",string name = "???") {
        int index = 0;
        foreach (string sentence in sentences) {
            index++;
            if (sentence.StartsWith('#') | sentence.StartsWith(' ') | sentence == "") continue;
            
            //// 处理插入内容
            // 注入变量
            string postVariable = Tools.EscapeDictResolver(sentence,_vars,"^");
            // 注入别名
            postVariable = Tools.AliasResolver(postVariable,Aliases);
            // 注入环境变量
            postVariable = Tools.EscapeDictResolver(postVariable,_envVars,"%");
            // 注入表达式
            int myIndex = index;
            string expression = Tools.CrateReplacer(postVariable,"{","}",s =>
                                                         ExecuteMethod(s,new LevitateExecutionContext {
                                                             WorkingDir = workingDir,
                                                             CurrentLine = myIndex,
                                                             Logger = new LevitateLogger(myIndex,name),
                                                             ShulkerContext = _shulkerContext,
                                                             Interpreter = this,
                                                             EnvVars = _envVars,
                                                             Vars = _vars
                                                         }) ?? "null");
            //// 执行
            ExecuteMethod(expression,new LevitateExecutionContext {
                WorkingDir = workingDir,
                CurrentLine = index,
                Logger = new LevitateLogger(index,name),
                ShulkerContext = _shulkerContext,
                Interpreter = this,
                EnvVars = _envVars,
                Vars = _vars
            });
        }
    }

    string? ExecuteMethod(string methodString,LevitateExecutionContext ec) {
        //Tokenize并执行
        string[] command = Tools.ResolveArgs(methodString);
        if (command.Length == 0) return null;
        if (Methods.TryGetValue(command[0],out LevitateMethod? method)) {
            return method(command,ec);
        }
        ec.Logger.AddNode("&9&oLevitate");
        ec.Logger.WriteLine($"&c未能找到已注册的方法&8[&c{command[0]}&8]",Terminal.MessageType.Error);
        return null;
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