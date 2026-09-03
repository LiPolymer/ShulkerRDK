using System.Text.Json;
using System.Text.Json.Serialization;
using ShulkerRDK.Prismarine.Services;
using ShulkerRDK.Shared;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Services;
using TridentCore.Pref;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineFileMeta {
    public PrismarineFileMeta() { }

    public PrismarineFileMeta(Package triPack, Filter? limiter = null) {
        Pref = PackageHelper.ToPref(triPack);
        if (triPack.Hash is { Algorithm: HashAlgorithm.Sha1, Value: var hash }) Sha1 = hash;
        FileName = triPack.FileName;
        Limiter = limiter;
        Type = triPack.Kind;
    }

    public PrismarineFileMeta(string purl, Filter? limiter = null) {
        if (!PackageHelper.TryParse(purl, out PackageIdentifier pdi))
            throw new ArgumentException($"无法解析purl: [{purl}]");
        Pref = PackageHelper.ToPref(pdi);
        Limiter = limiter;
    }

    public string Pref { get; set; } = string.Empty;
    public string? Sha1 { get; set; }
    public string? FileName { get; set; }
    public Filter? Limiter { get; set; }
    public ResourceKind? Type { get; set; }
    public bool Locked { get; set; }
    public bool Enabled { get; set; } = true;

    public void Update(Filter? filter = null, bool updateLimiter = false) { 
        filter ??= Limiter ?? PrismarineContext.GetLimiter(Type);

        if (!PackageHelper.TryParse(Pref, out PackageIdentifier pdi))
            throw new ArgumentException($"无法解析purl: [{Pref}]");

        RepositoryAgent agent = TridentServices.RepositoryAgent;
        Package resolved = agent.ResolveAsync(pdi, filter)
            .GetAwaiter().GetResult();

        Pref = PackageHelper.ToPref(resolved);
        Sha1 = resolved.Hash is { Algorithm: HashAlgorithm.Sha1, Value: var hash } ? hash : null;
        FileName = resolved.FileName;
        if (updateLimiter) Limiter = filter;
    }

    public Profile.Rice.Entry ToEntry() {
        return new Profile.Rice.Entry {
            Pref = Pref,
            Enabled = Enabled
        };
    }

}

public class PrismarineFileInstance {
    public required string FilePath;
    public required PrismarineFileMeta Meta;

    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };
    
    public static PrismarineFileInstance Load(string path) {
        return new PrismarineFileInstance {
            FilePath = path,
            Meta = JsonSerializer.Deserialize<PrismarineFileMeta>(File.ReadAllText(path), JsonSerializerOptions)
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
        File.WriteAllText(path, JsonSerializer.Serialize(Meta, JsonSerializerOptions));
    }
}