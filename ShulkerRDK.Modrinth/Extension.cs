using Modrinth;
using ShulkerRDK.Shared;
using Version = Modrinth.Models.Version;

namespace ShulkerRDK.Modrinth;

// ReSharper disable once UnusedType.Global
public class Extension : IShulkerExtension {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("Modrinth.Net/3.5.1");
        #endif
        
        Commands.Add("mrp",Manager.Command);
        LevitateMethods.Add("mrp",Manager.Method);
    }
    public string Id { get => "shulker.modrinth"; }
    public string Name { get => "ModrinthPSK"; }
    public string Description { get => "添加Modrinth平台支持"; }
    public string Author { get => "LiPolymer"; }
    public string Version { get => "Dev.Inf"; }
    public string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public string Donating { get => "https://afdian.tv/a/lipolymer"; }
    public string? Document { get => null; }

    public string AsciiArt { get => """
                                    &a|V| _  _| __ o __ _|_|_ 
                                    &a| |(_)(_| |  | | | |_| | &ePSK
                                    """; }
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,LevitateMethod> LevitateMethods { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
    public Dictionary<string,string> LevitateAliases { get; } = [];
    public void Init(ShulkerContext context) {
        Manager.Context = context;
    }
    public void Shutdown(ShulkerContext context) { }
}