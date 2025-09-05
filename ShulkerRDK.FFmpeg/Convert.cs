using System.Text.RegularExpressions;
using FFMpegCore;
using ShulkerRDK.Shared;

namespace ShulkerRDK.FFmpeg;

public static partial class Convert {
    public static string? Method(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&2FFmpeg");
        if (!Tools.CheckParamLength(args,1,ec)) return null;
        string inputPath = args[1];
        string outputPath;
        bool destroySource = true;
        if (Directory.Exists(inputPath)) {
            outputPath = inputPath;
            if (args.Length > 2) {
                outputPath = args[2];
                destroySource = false;
            }
            ec.Logger.WriteLine($"&7正在转换&8[&7{inputPath}&8]>[&7{outputPath}&8]");
            Regex audioFileRegex = FileTypeRegex();
            string[] audioFiles = Directory.GetFiles(inputPath,"*.*",SearchOption.AllDirectories)
                .Where(path => audioFileRegex.IsMatch(path))
                .ToArray();
            foreach (string file in audioFiles) {
                ec.Logger.WriteLine($"&7正在转换&8[&7{Path.GetFileName(file)}&8]",Terminal.MessageType.Debug);
                string relativePath = Path.GetRelativePath(inputPath,file);
                string pngRelativePath = Path.ChangeExtension(relativePath,".ogg");
                string destPath = Path.Combine(outputPath,pngRelativePath);
                string? destDirectory = Path.GetDirectoryName(destPath);
                if (!Directory.Exists(destDirectory)) {
                    Directory.CreateDirectory(destDirectory!);
                }
                FFMpegArguments.FromFileInput(file).OutputToFile(destPath).ProcessSynchronously();
                if (destroySource) File.Delete(file);
            }
        } else if (File.Exists(inputPath)) {
            outputPath = Path.Combine(Path.GetDirectoryName(inputPath)!,Path.GetFileNameWithoutExtension(inputPath) + ".ogg");
            if (args.Length > 2) {
                outputPath = args[2];
            }
            ec.Logger.WriteLine($"&7正在转换&8[&7{inputPath}&8]>[&7{outputPath}&8]");
            FFMpegArguments.FromFileInput(inputPath).OutputToFile(outputPath).ProcessSynchronously();
        } else {
            ec.Logger.WriteLine("&c无效的路径",Terminal.MessageType.Error);
        }
        return null;
    }

    [GeneratedRegex(".*\\.(?:mp3|MP3|wav|WAV|flac|FLAC)$")]
    private static partial Regex FileTypeRegex();
}