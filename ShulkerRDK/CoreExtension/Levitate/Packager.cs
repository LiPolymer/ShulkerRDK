using System.Formats.Tar;
using System.IO.Compression;
using ShulkerRDK.Shared;

namespace ShulkerRDK.CoreExtension.Levitate;

public static class Packager {
    static Dictionary<string, LevitateMethod>? _actions;
    public static string? Method(string[] args, LevitateExecutionContext ec) {
        if (_actions == null) {
            _actions = [];
            _actions.Add("make",Make);
            _actions.Add("tear",Tear);
        }
        ec.Logger.AddNode("&l&dPkgR");
        if (!Tools.TryGetSub(["zip","tar"],args,1,ec)) return null;
        if (!Tools.TryGetSub(["make","tear"],args,2,ec)) return null;
        
        //TODO: 于此处注入环境变量
        if (!Tools.CheckParamLength(args, 3, ec)) return null;
        if (!Tools.CheckParamLength(args, 4, ec)) return null;
        
        Tools.TryRunSub(_actions,args,2,ec);
        return null;
    }

    static string? Make(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&dMake");
        string fileType = args[1];
        string ingredientPath = args[3];
        string outPath = args[4];
        if (!Directory.Exists(ingredientPath)) {
            ec.Logger.WriteLine($"&c目录不存在&8[&c{ingredientPath}&8]");
            return null;
        }
        ec.Logger.WriteLine($"&7正在创建&8[&7{fileType}&8]&7文件&8[&7{ingredientPath}&8]>&8[&7{outPath}&8]");
        string? dir = Path.GetDirectoryName(outPath);
        if (!Directory.Exists(dir) & dir != null & dir != string.Empty) {
            Directory.CreateDirectory(dir!);
        }
        if (File.Exists(outPath)) {
            File.Delete(outPath);
        }
        switch (fileType) {
            case "zip":
                ZipFile.CreateFromDirectory(ingredientPath, outPath);
                break;
            case "tar":
                TarFile.CreateFromDirectory(ingredientPath, outPath, false);
                break;
            default:
                throw new ArgumentOutOfRangeException(fileType);
        }
        ec.Logger.WriteLine("&a完成!");
        return null;
    }
    
    static string? Tear(string[] args,LevitateExecutionContext ec) {
        ec.Logger.AddNode("&dTear");
        string fileType = args[1];
        string srcPath = args[3];
        string outPath = args[4];
        if (!File.Exists(srcPath)) {
            ec.Logger.WriteLine($"&c档案包不存在&8[&c{srcPath}&8]");
            return null;
        }
        ec.Logger.WriteLine($"&7正在释放&8[&7{srcPath}&8]>&8[&7{outPath}&8]");
        if (!Directory.Exists(outPath)) {
            Directory.CreateDirectory(outPath);
        }
        switch (fileType) {
            case "zip":
                ZipFile.ExtractToDirectory(srcPath, outPath, true);
                break;
            case "tar":
                TarFile.ExtractToDirectory(srcPath, outPath, true);
                break;
            default:
                throw new ArgumentOutOfRangeException(fileType);
        }
        ec.Logger.WriteLine("&a完成!");
        return null;
    }
}