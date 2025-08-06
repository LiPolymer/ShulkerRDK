using System.ComponentModel;
using ShulkerRDK.Shared;

namespace ShulkerRDK.RRT;

public static class ProjectWatcher {
    [Description("文件夹监测")]
    public static void Command(string[] args,ShulkerContext sc) {
        Transition(args,sc,Ct);
    }

    public static string? Method(string[] args, LevitateExecutionContext ec) {
        ec.Logger.AddNode("&9RRT");
        ec.Logger.AddNode("&b&oPW");
        Transition(args,ec.ShulkerContext,ec.Logger);
        return null;
    }

    static void Transition(string[] args,ShulkerContext sc, IChainedLikeTerminal ct) {
        if (!Tools.TryGetSub(["start","stop"],args,1,ct)) return;
        string target = sc.ProjectConfig!.RootPath;
        string filter = "*";
        if (Tools.CheckParamLength(args,2)) {
            filter = args[2];
        }
        if (Tools.CheckParamLength(args,3)) {
            target = args[3];
        }
        switch (args[1]) {
            case "start":
                StartWatching(target,filter,ct);
                break;
            case "stop":
                StopWatching(ct);
                break;
        }
    }
    
    static readonly ChainedTerminal Ct = new ChainedTerminal("&9RRT").Chain("&b&oPW");
    
    static FileSystemWatcher? _watcher;
    public static ShulkerContext? Context { private get; set; }

    static void StartWatching(string folderPath, string filter = "*.*", IChainedLikeTerminal? ct = null) {
        StopWatching();
        _watcher = new FileSystemWatcher {
            Path = folderPath,
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = filter
        };

        _watcher.Created += OnFileChanged;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;

        _watcher.Created += OnChangeCaptured;
        _watcher.Changed += OnChangeCaptured;
        _watcher.Deleted += OnChangeCaptured;
        _watcher.Renamed += OnChangeCaptured;
        
        _watcher.EnableRaisingEvents = true;
        ct?.WriteLine($"&7开始监视&8[&7{folderPath}&8]");
    }

    static void OnFileChanged(object sender,FileSystemEventArgs e) {
        Console.WriteLine();
        Ct.WriteLine($"&8{e.ChangeType}: {e.FullPath}",Terminal.MessageType.Debug);
    }
    static void OnRenamed(object sender,RenamedEventArgs e) {
        Console.WriteLine();
        Ct.WriteLine($"&8重命名: {e.OldFullPath} -> {e.FullPath}",Terminal.MessageType.Debug);
    }
    static void OnError(object sender,ErrorEventArgs e) {
        Console.WriteLine();
        Ct.WriteLine($"&c错误: {e.GetException().Message}",Terminal.MessageType.Error);
        Terminal.Write("&8[&c结束&7>&e");
    }

    static void OnChangeCaptured(object sender,RenamedEventArgs e) {
        OnChangeCaptured();
    }
    static void OnChangeCaptured(object sender,FileSystemEventArgs e) {
        OnChangeCaptured();
    }
    static void OnChangeCaptured(string? path = null) {
        TriggerState = true;
    }
    static bool _triggerState;
    static bool TriggerState {
        set {
            if (_triggerState == value) return;
            _triggerState = value;
            if (value) new Thread(OnChangeHandling).Start();
        }
    }

    const string LvtPath = "./shulker/tasks/hotDeploy.lvt";
    static void OnChangeHandling() {
        Thread.Sleep(800);
        LevitateInterpreter li = new LevitateInterpreter(Context!);
        if (File.Exists(LvtPath)) {
            li.RunFromFile(LvtPath);
            Terminal.Write("&8[&a执行结束&7>&e");
        } else {
            Tools.WriteAllText(LvtPath, string.Empty);
        }
        TriggerState = false;
    }

    static void StopWatching(IChainedLikeTerminal? ct = null) {
        if (_watcher == null) return;
        ct?.WriteLine("&7停止监视...");
        _watcher?.Dispose();
        _watcher = null;
    }
}