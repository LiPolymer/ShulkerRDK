using ImageMagick;
using ShulkerRDK.ResourceMagick.Levitate;
using ShulkerRDK.Shared;

namespace ShulkerRDK.ResourceMagick;

// ReSharper disable once UnusedType.Global
public class Extension : IShulkerExtension {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("Magick.NET.Core/14.6.0");
        NugetHelper.DependencyVerify("Magick.NET-Q16-AnyCPU/14.6.0","net8.0",true,[
            "Magick.NET-Q16-AnyCPU.dll",
            "Magick.Native-Q16-x64.dll|Magick.Native-Q16-x86.dll|Magick.Native-Q16-arm64.dll" +
            "|Magick.Native-Q16-x64.dll.so|Magick.Native-Q16-arm64.dll.so" +
            "|Magick.Native-Q16-x64.dll.dylib|Magick.Native-Q16-arm64.dll.dylib"
        ]);
        #endif
        LevitateMethods.Add("psdcvt",PsdConverter.Method);
        LevitateAliases.Add("^psdCvt$", "psdcvt %project.src% %project.cache%");
    }
    
    public string Id { get => "shulker.magick"; }
    public string Name { get => "ResourceMagick"; }
    public string Description { get => "Image Magick tool for ShulkerRDK"; }
    public string Author { get => "LiPolymer"; }
    public string Version { get => "Dev"; }
    public string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,LevitateMethod> LevitateMethods { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
    public Dictionary<string,string> LevitateAliases { get; } = [];
    public void Init(ShulkerContext context) { }
    public void Shutdown(ShulkerContext context) { }
}