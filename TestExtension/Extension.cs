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
                FileDownloader.DownloadFile(strings[1], "./downloader/test");
            }
        });
        
        Commands.Add("platform",(strings,_) => {
            Console.WriteLine(RuntimeInformation.OSArchitecture);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.WriteLine("当前平台是 Windows");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Console.WriteLine("当前平台是 Linux");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Console.WriteLine("当前平台是 macOS");
            }
            else
            {
                Console.WriteLine("未知平台");
            }
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