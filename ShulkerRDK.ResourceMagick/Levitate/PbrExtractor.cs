using ImageMagick;
using ShulkerRDK.Shared;

namespace ShulkerRDK.ResourceMagick.Levitate;

public static class PbrExtractor {
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&dMagick");
        ec.Logger.AddNode("&dpE");
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        bool includeBasicTexture = false;
        string psdPath = args[1];
        string imagePath;
        if (args.Length > 3) {
            includeBasicTexture = args[3] switch {
                "true" => true,
                "false" => false,
                _ => includeBasicTexture
            };
        }
        if (Directory.Exists(psdPath)) {
            imagePath = psdPath;
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ec.Logger.WriteLine($"&7正在分离&8[&7{psdPath}&8]>[&7{imagePath}&8]");
            string[] psdFiles = Directory.GetFiles(psdPath,"*.psd",SearchOption.AllDirectories);
            foreach (string file in psdFiles) {
                ec.Logger.WriteLine($"&7正在分离&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(psdPath,file);
                string pngRelativePath = Path.Combine(Path.GetDirectoryName(relativePath)!,Path.GetFileNameWithoutExtension(relativePath));
                string destPath = Path.Combine(imagePath,pngRelativePath);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory!);
                }
                if (includeBasicTexture) Shared.GetMergedLayers(file,"^(?!n_|s_)")?.Write(destPath + ".png",MagickFormat.Png);
                Shared.GetMergedLayers(file,"^n_")?.Write(destPath + "_n.png",MagickFormat.Png);
                Shared.GetMergedLayers(file,"^s_")?.Write(destPath + "_s.png",MagickFormat.Png);
            }
        } else if (File.Exists(psdPath)) {
            imagePath = Path.Combine(Path.GetDirectoryName(psdPath)!,Path.GetFileNameWithoutExtension(psdPath));
            if (args.Length > 2) {
                imagePath = args[2];
            }
            string? path = Path.GetDirectoryName(imagePath);
            if (path != null && !Directory.Exists(path)) {
                Directory.CreateDirectory(path);
            }
            ec.Logger.WriteLine($"&7正在分离&8[&7{psdPath}&8]>[&7{imagePath}&8_*.png]");
            if (includeBasicTexture) Shared.GetMergedLayers(psdPath,"^(?!n_|s_)")?.Write(imagePath + ".png",MagickFormat.Png);
            Shared.GetMergedLayers(psdPath,"^n_")?.Write(imagePath + "_n.png",MagickFormat.Png);
            Shared.GetMergedLayers(psdPath,"^s_")?.Write(imagePath + "_s.png",MagickFormat.Png);
        } else {
            ec.Logger.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
        return null;
    }
}