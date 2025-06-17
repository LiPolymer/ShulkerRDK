using ImageMagick;
using ShulkerRDK.Shared;

namespace ShulkerRDK.ResourceMagick.Command;

public static class Formatter {
    public static void Command(string[] args,ShulkerContext sc) {
        ChainedTerminal ct = new ChainedTerminal("&dMagick");
        if (!Tools.CheckParamLength(args,1,ct)) return;
        string pngPath = args[1];
        string imagePath;
        if (Directory.Exists(pngPath)) {
            imagePath = sc.ProjectConfig!.RootPath;
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ct.WriteLine($"&7正在转换&8[&7{pngPath}&8]>[&7{imagePath}&8]");
            string[] pngFiles = Directory.GetFiles(pngPath,"*.png",SearchOption.AllDirectories);
            foreach (string file in pngFiles) {
                if (file.EndsWith("_s.png")|file.EndsWith("_n.png")) continue;
                ct.WriteLine($"&7正在转换&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(pngPath,file);
                string pngRelativePath = Path.ChangeExtension(relativePath,".psd");
                string destPath = Path.Combine(imagePath,pngRelativePath);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory!);
                }
                using MagickImage image = new MagickImage(file);
                image.Write(destPath);
            }
            ct.WriteLine("&a完成!");
        } else if (File.Exists(pngPath)) {
            imagePath = Path.Combine(Path.GetDirectoryName(pngPath)!,Path.GetFileNameWithoutExtension(pngPath) + ".psd");
            if (args.Length > 2) {
                imagePath = args[2];
            }
            ct.WriteLine($"&7正在转换&8[&7{pngPath}&8]>[{imagePath}&8]");
            using MagickImage image = new MagickImage(pngPath);
            image.Write(imagePath);
            ct.WriteLine("&a完成!");
        } else {
            ct.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
    }
}