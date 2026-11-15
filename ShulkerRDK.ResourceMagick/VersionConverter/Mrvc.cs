using System.ComponentModel;
using ImageMagick;
using ShulkerRDK.Shared;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

static class Mrvc {
    const string Usage = "mrvc <源包> <目标包> [原版本] <目标版本>";

    [Description("迁移 Java 资源包版本")]
    public static void Command(string[] args, ShulkerContext context) {
        ChainedTerminal logger = new ChainedTerminal("&dMagick");
        try {
            Execute(args, logger);
        } catch (Exception error) when (error is ArgumentException or FormatException or IOException
                                       or InvalidDataException or UnauthorizedAccessException or NotSupportedException or MagickException) {
            logger.WriteLine($"&c{error.Message}", Terminal.MessageType.Error);
        }
    }

    public static string? Method(string[] args, LevitateExecutionContext context) {
        context.Logger.AddNode("&dMagick");
        Execute(args, context.Logger);
        return null;
    }

    static void Execute(string[] args, IChainedLikeTerminal logger) {
        logger.AddNode("&dmrvc");
        if (args.Length is not (4 or 5)) throw new ArgumentException($"用法：{Usage}");

        PackFormat? original = args.Length == 5 ? PackFormat.Parse(args[3]) : null;
        PackFormat target = PackFormat.Parse(args[^1]);
        string baseDirectory = Environment.CurrentDirectory;
        string sourcePath = Path.GetFullPath(args[1], baseDirectory);
        string targetPath = Path.GetFullPath(args[2], baseDirectory);
        
        logger.WriteLine($"&7正在迁移&8[&7{sourcePath}&8]>[&7{targetPath}&8]");
        PackMigrationResult result = PackMigration.Run(sourcePath, targetPath, original, target,
            message => logger.WriteLine($"&e{message}", Terminal.MessageType.Warn));
        logger.WriteLine($"&a完成&8[&7{result.SourceFormat} -> {result.TargetFormat}&8] " +
                         $"&7文件 {result.CopiedFiles}，改名 {result.RenamedFiles}，图像转换 {result.ConvertedImages}，跳过 {result.SkippedFiles}");
    }
}
