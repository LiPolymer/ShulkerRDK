using ShulkerRDK.Shared;

namespace ShulkerRDK.RRT;

// ReSharper disable once UnusedType.Global
public class Extension : IShulkerExtension {
    public Extension() {
        LevitateMethods.Add("rrt",TriggerReload.Method);
        LevitateMethods.Add("pw",ProjectWatcher.Method);
        Commands.Add("pw",ProjectWatcher.Command);
    }
    public string Id { get => "shulker.rrt"; }
    public string Name { get => "ShulkerRRT"; }
    public string Description { get => "提供变更监视与实时构建重载支持"; }
    public string Author { get => "LiPolymer"; }
    public string Version { get => "Dev"; }
    public string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public string? AsciiArt { get => """
                                     &9 _______ __           __ __               &b ______ ______ _______ 
                                     &9|     __|  |--.--.--.|  |  |--.-----.----.&b|   __ \   __ \_     _|
                                     &9|__     |     |  |  ||  |    <|  -__|   _|&b|      <      < |   |  
                                     &9|_______|__|__|_____||__|__|__|_____|__|  &b|___|__|___|__| |___|  
                                     """; }
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,LevitateMethod> LevitateMethods { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
    public Dictionary<string,string> LevitateAliases { get; } = [];
    public void Init(ShulkerContext context) { 
        ProjectWatcher.Context = context;
    }
    public void Shutdown(ShulkerContext context) { }
}