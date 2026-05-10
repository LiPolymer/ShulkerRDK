using System.Text.Json;
using ShulkerRDK.Shared;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineFileMeta {
    public static Filter? GlobalLimiter;
    public PrismarineFileMeta(Package triPack, Filter? limiter = null) {
        Purl = PackageHelper.ToPurl(triPack);
        if (triPack.Sha1 != null) Sha1 = triPack.Sha1;
        Limiter = limiter;
    }
    public PrismarineFileMeta(string purl, Filter? limiter = null) {
        if (!PackageHelper.TryParse(purl, out (string Label, string? Namespace, string Pid, string? Vid) pdi))
            throw new ArgumentException($"无法解析purl: [{purl}]");
        Purl = PackageHelper.ToPurl(pdi.Label,pdi.Namespace,pdi.Pid,pdi.Vid); //标准化
        Limiter = limiter;
    }
    public string Purl;
    public string? Sha1;
    public Filter? Limiter;

    public void Update(Filter? filter = null) {
        filter ??= GlobalLimiter ?? throw new Exception("全局限滤器未设定");
        
    }
}

public class PrismarineFileInstance {
    public required string FilePath;
    public required PrismarineFileMeta Meta;

    public static PrismarineFileInstance Load(string path) {
        return new PrismarineFileInstance {
            FilePath = path,
            Meta = JsonSerializer.Deserialize<PrismarineFileMeta>(File.ReadAllText(path)) 
                   ?? throw new FileLoadException()
        };
    }

    public static PrismarineFileInstance Create(string path, PrismarineFileMeta meta) {
        PrismarineFileInstance pfi = new PrismarineFileInstance {
            FilePath = path,
            Meta = meta,
        };
        pfi.Save();
        return pfi;
    }
    
    public void Save(string? path = null) {
        path ??= FilePath;
        string? dir = Path.GetDirectoryName(path);
        if (dir != null & !Directory.Exists(dir)) Directory.CreateDirectory(dir!);
        File.WriteAllText(path, JsonSerializer.Serialize(Meta, Tools.JsonSerializerOptions));
    }
}