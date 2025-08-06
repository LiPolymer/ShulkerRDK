using Modrinth;
using ShulkerRDK.Shared;
using Version = Modrinth.Models.Version;

namespace ShulkerRDK.Modrinth;

// ReSharper disable once UnusedType.Global
public class Extension : ExtensionBase {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("Modrinth.Net/3.5.1");
        #endif
        
        Commands.Add("mrp",Manager.Command);
        LevitateMethods.Add("mrp",Manager.Method);
    }
    public override string Id { get => "shulker.modrinth"; }
    public override string Name { get => "ModrinthPSK"; }
    public override string Description { get => "添加Modrinth平台支持"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "Dev.Inf"; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public override string Donating { get => "https://afdian.tv/a/lipolymer"; }

    public override string AsciiArt { get => """
                                             &a|V| _  _| __ o __ _|_|_ 
                                             &a| |(_)(_| |  | | | |_| | &ePSK
                                             """; }
    public override void Init(ShulkerContext context) {
        Manager.Context = context;
    }
}