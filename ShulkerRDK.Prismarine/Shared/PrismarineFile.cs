using System.Text.Json;
using System.Text.Json.Serialization;
using ShulkerRDK.Prismarine.Services;
using ShulkerRDK.Shared;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineFileMeta {
    public PrismarineFileMeta() { }

    public PrismarineFileMeta(Package triPack, Filter? limiter = null) {
        Purl = PackageHelper.ToPurl(triPack);
        if (triPack.Sha1 != null) Sha1 = triPack.Sha1;
        FileName = triPack.FileName;
        Limiter = limiter;
        Type = triPack.Kind;
    }

    public PrismarineFileMeta(string purl, Filter? limiter = null) {
        if (!PackageHelper.TryParse(purl, out (string Label, string? Namespace, string Pid, string? Vid) pdi))
            throw new ArgumentException($"无法解析purl: [{purl}]");
        Purl = PackageHelper.ToPurl(pdi.Label, pdi.Namespace, pdi.Pid, pdi.Vid);
        Limiter = limiter;
    }

    public string Purl { get; set; } = string.Empty;
    public string? Sha1 { get; set; }
    public string? FileName { get; set; }
    public Filter? Limiter { get; set; }
    public ResourceKind? Type { get; set; }
    public bool Locked { get; set; }
    public bool Enabled { get; set; } = true;

    public void Update(Filter? filter = null, bool updateLimiter = false) { 
        filter ??= Limiter ?? PrismarineContext.GetLimiter(Type);

        if (!PackageHelper.TryParse(Purl, out (string Label, string? Namespace, string Pid, string? Vid) pdi))
            throw new ArgumentException($"无法解析purl: [{Purl}]");

        RepositoryAgent agent = TridentServices.RepositoryAgent;
        Package resolved = agent.ResolveAsync(pdi.Label, pdi.Namespace, pdi.Pid, null, filter)
            .GetAwaiter().GetResult();

        Purl = PackageHelper.ToPurl(resolved);
        Sha1 = resolved.Sha1;
        FileName = resolved.FileName;
        if (updateLimiter) Limiter = filter;
    }

    public Profile.Rice.Entry ToEntry() {
        return new Profile.Rice.Entry {
            Purl = Purl,
            Enabled = Enabled
        };
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
            Meta = meta
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