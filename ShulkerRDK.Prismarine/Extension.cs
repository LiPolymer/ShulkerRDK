using ShulkerRDK.Prismarine.Commands;
using ShulkerRDK.Shared;

namespace ShulkerRDK.Prismarine;

public class Extension : ExtensionBase {
    public Extension() {
        Commands.Add("pfm", PfManager.Command);
        LevitateMethods.Add("pfm", PfManager.Method);
    }

    public override string Id { get => "shulker.prismarine"; }
    public override string Name { get => "Prismarine"; }
    public override string Description { get => "next generation modpack developing backend"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "B0.20"; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public override string Donating { get => "https://afdian.tv/a/lipolymer"; }

    public override string AsciiArt { get => """
                                             &b┌──────┐┌──────┐┌──────┐┌───┐──┐┌──────┐
                                             &b│      ││       └──────┐│      ││       
                                             &b│──────┘│       │      ││      ││       
                                             &b└       └       └──────┘└      ┘└      
                                             """; }

    public override void Init(ShulkerContext context) {
        Services.TridentServices.Initialize();
    }

    public override void Shutdown(ShulkerContext context) {
        Services.TridentServices.Shutdown();
    }
}