using System.Runtime.InteropServices;
using System.Windows.Input;
using ShulkerRDK;
using ShulkerRDK.Shared;

namespace TestExtension;

// ReSharper disable once UnusedType.Global
public class Extension : IShulkerExtension {
    public Extension() {
        Commands.Add("doSth",(strings,_) => {
            Terminal.WriteLine(strings.Length < 2 ? "&7你没有输入带参指令" : $"&7你的第一个参数是 [&e{strings[1]}&7]");
        });

        Commands.Add("down",(strings,_) => {
            if (strings.Length >= 2) {
                FileDownloader.DownloadFile(strings[1],"./downloader/test");
            }
        });

        Commands.Add("platform",(strings,_) => {
            Console.WriteLine(RuntimeInformation.OSArchitecture);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Console.WriteLine("当前平台是 Windows");
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                Console.WriteLine("当前平台是 Linux");
            } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                Console.WriteLine("当前平台是 macOS");
            } else {
                Console.WriteLine("未知平台");
            }
        });

        Commands.Add("mfs",(strings,context) => {
            //ChainedTerminal ct = new ChainedTerminal("mfs");
            //if (!Tools.CheckParamLength(strings,1,ct)) return;
            var watcher = new FolderWatcher();
            watcher.StartWatching("./src");
        });

        LevitateMethods.Add("doSth",(strings,_) => {
            Terminal.WriteLine(strings.Length < 2 ? "&7你没有使用带参方法" : $"&7你的第一个参数是 [&e{strings[1]}&7]");
            return strings.Length < 2 ? "&7你没有使用带参表达式方法" : $"&7我给你返回 [&e{strings[1]}&7]";
        });
    }

    public string Id { get => "ext.test"; }
    public string Name { get => "测试扩展"; }
    public string Description { get => "这是描述"; }
    public string Author { get => "LiPolymer"; }
    public string Version { get => "1.14.51"; }
    public string Link { get => "yaRiMasNe"; }
    public string Donating { get => "support me"; }
    public string Document { get => "doc"; }
    public string? AsciiArt { get => null; }
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

public class FolderWatcher {
    FileSystemWatcher _watcher;

    public void StartWatching(string folderPath) {
        _watcher = new FileSystemWatcher {
            Path = folderPath,
            IncludeSubdirectories = true, // 关键：监听子文件夹
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            Filter = "*.*" // 监听所有文件
        };

        // 订阅事件
        _watcher.Created += OnFileChanged;
        _watcher.Changed += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnRenamed;
        _watcher.Error += OnError;

        _watcher.EnableRaisingEvents = true; // 启动监听
        Console.WriteLine($"开始监听: {folderPath}");
    }

    void OnFileChanged(object sender,FileSystemEventArgs e) {
        Console.WriteLine($"[{DateTime.Now}] {e.ChangeType}: {e.FullPath}");
    }

    void OnRenamed(object sender,RenamedEventArgs e) {
        Console.WriteLine($"[{DateTime.Now}] 重命名: {e.OldFullPath} -> {e.FullPath}");
    }

    void OnError(object sender,ErrorEventArgs e) {
        Console.WriteLine($"错误: {e.GetException().Message}");
    }

    public void StopWatching() {
        _watcher?.Dispose();
    }
}