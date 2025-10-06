using ShulkerRDK.Shared;

namespace ShulkerRDK.FFmpeg;

public class Extension : ExtensionBase {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("FFMpegCore/5.2.0","netstandard2.0");
        NugetHelper.DependencyVerify("Instances/3.0.1","netstandard2.0");
        NugetHelper.DependencyVerify("System.IO.Pipelines/10.0.0-preview.7.25380.108");
        NugetHelper.DependencyVerify("System.Text.Json/10.0.0-preview.7.25380.108");
        NugetHelper.DependencyVerify("System.Text.Encodings.Web/10.0.0-preview.7.25380.108","net8.0",false);
        #endif
        LevitateMethods.Add("a2ogg",Convert.Method);
    }
    
    public override string Id { get => "shulker.ffmpeg"; }
    public override string Name { get => "ShulkerFFmpeg"; }
    public override string Description { get => "FFmpeg集成"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "Dev"; }
    public override string Link { get => ""; }
}