using ShulkerRDK.ResourceMagick.Command;
using ShulkerRDK.ResourceMagick.Levitate;
using ShulkerRDK.Shared;

namespace ShulkerRDK.ResourceMagick;

// ReSharper disable once UnusedType.Global
public class Extension : ExtensionBase {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("Magick.NET.Core/14.7.0");
        NugetHelper.DependencyVerify("Magick.NET-Q16-AnyCPU/14.7.0","net8.0",true,[
            "Magick.NET-Q16-AnyCPU.dll",
            "Magick.Native-Q16-x64.dll|Magick.Native-Q16-x86.dll|Magick.Native-Q16-arm64.dll" +
            "|Magick.Native-Q16-x64.dll.so|Magick.Native-Q16-arm64.dll.so" +
            "|Magick.Native-Q16-x64.dll.dylib|Magick.Native-Q16-arm64.dll.dylib"
        ]);
        #endif
        Commands.Add("png2psd",Formatter.Command);
        
        LevitateMethods.Add("psdcvt",PsdConverter.Method);
        LevitateMethods.Add("pbrex",PbrExtractor.Method);
        
        LevitateAliases.Add("^psdCvt$", "psdcvt \"%project.src%\" \"%project.cache%\"");
        LevitateAliases.Add("^pbrEx$", "pbrex \"%project.src%\" \"%project.cache%\"");
    }
    
    public override string Id { get => "shulker.magick"; }
    public override string Name { get => "ResourceMagick"; }
    public override string Description { get => "用于 ShulkerRDK 的 Image Magick 集成"; }
    public override string Author { get => "LiPolymer"; }
    public override string Version { get => "B0.10"; }
    public override string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public override string Donating { get => "https://afdian.tv/a/lipolymer"; }
    public override string AsciiArt { get => """
                                              &b _  _      _____&d _      ____  _____ _  ____ _  __
                                              &b/ \/ \__/|/  __/&d/ \__/|/  _ \/  __// \/   _Y |/ /
                                              &b| || |\/||| |  _&d| |\/||| / \|| |  _| ||  / |   / 
                                              &b| || |  ||| |_//&d| |  ||| |-||| |_//| ||  \_|   \ 
                                              &b\_/\_/  \|\____\&d\_/  \|\_/ \|\____\\_/\____|_|\_\
                                              """; }
}