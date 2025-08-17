using ShulkerRDK.Shared;

namespace ShulkerRDK.Aseprite;

public class Extension : ExtensionBase {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("AsepriteDotNet/1.9.0");
        #endif
        
        Commands.Add("ase", Convertion.Command);
        LevitateMethods.Add("ase",Convertion.Method);
    }
    public override string Id { get => "shulker.ase"; }
    public override string Name { get => "AsepriteExtractor"; }
    public override string Description { get => "用于 Aseprite 格式的自动转化"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "Dev"; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public override string Donating { get => "https://afdian.tv/a/lipolymer"; }
}