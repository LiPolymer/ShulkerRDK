using ShulkerRDK.Shared;

namespace ShulkerRDK.RRT;

// ReSharper disable once UnusedType.Global
public class Extension : ExtensionBase {
    public Extension() {
        LevitateMethods.Add("rrt",TriggerReload.Method);
        LevitateMethods.Add("pw",ProjectWatcher.Method);
        Commands.Add("pw",ProjectWatcher.Command);
    }
    public override string Id { get => "shulker.rrt"; }
    public override string Name { get => "ShulkerRRT"; }
    public override string Description { get => "提供变更监视与实时构建重载支持"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "B0.10"; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public override string Donating { get => "https://afdian.tv/a/lipolymer"; }

    public override string? AsciiArt { get => """
                                              &9 _______ __           __ __               &b ______ ______ _______ 
                                              &9|     __|  |--.--.--.|  |  |--.-----.----.&b|   __ \   __ \_     _|
                                              &9|__     |     |  |  ||  |    <|  -__|   _|&b|      <      < |   |  
                                              &9|_______|__|__|_____||__|__|__|_____|__|  &b|___|__|___|__| |___|  
                                              """; }
    public override void Init(ShulkerContext context) { 
        ProjectWatcher.Context = context;
    }
}