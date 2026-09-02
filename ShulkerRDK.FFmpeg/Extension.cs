using ShulkerRDK.Shared;

namespace ShulkerRDK.FFmpeg;

public class Extension : ExtensionBase {
    public Extension() {
        LevitateMethods.Add("a2ogg",Convert.Method);
    }
    
    public override string Id { get => "shulker.ffmpeg"; }
    public override string Name { get => "ShulkerFFmpeg"; }
    public override string Description { get => "FFmpeg集成"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "Dev"; }
    public override string? AsciiArt { get => """
                                              &a    ____________                          
                                              &a   / ____/ ____/___ ___  ____  ___  ____ _
                                              &a  / /_  / /_  / __ `__ \/ __ \/ _ \/ __ `/
                                              &a / __/ / __/ / / / / / / /_/ /  __/ /_/ / 
                                              &a/_/   /_/   /_/ /_/ /_/ .___/\___/\__, /  
                                              &a                     /_/         /____/  
                                              """; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
}