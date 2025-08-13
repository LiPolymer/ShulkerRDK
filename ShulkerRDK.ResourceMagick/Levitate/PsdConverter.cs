using ImageMagick;
using ShulkerRDK.Shared;

namespace ShulkerRDK.ResourceMagick.Levitate;

public static class PsdConverter {
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&dMagick");
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        string psdPath = args[1];
        string imagePath;
        if (Directory.Exists(psdPath)) {
            imagePath = psdPath;
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ec.Logger.WriteLine($"&7正在转换&8[&7{psdPath}&8]>[&7{imagePath}&8]");
            string[] psdFiles = Directory.GetFiles(psdPath,"*.psd",SearchOption.AllDirectories);
            foreach (string file in psdFiles) {
                ec.Logger.WriteLine($"&7正在转换&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(psdPath,file);
                string pngRelativePath = Path.ChangeExtension(relativePath,".png");
                string destPath = Path.Combine(imagePath,pngRelativePath);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory!);
                }
                using MagickImage image = new MagickImage(file);
                image.Write(destPath);
            }
        } else if (File.Exists(psdPath)) {
            imagePath = Path.Combine(Path.GetDirectoryName(psdPath)!,Path.GetFileNameWithoutExtension(psdPath) + ".png");
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ec.Logger.WriteLine($"&7正在转换&8[&7{psdPath}&8]>[&7{imagePath}&8]");
            using MagickImage image = new MagickImage(psdPath);
            image.Write(imagePath);
        } else {
            ec.Logger.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
        return null;
    }
}