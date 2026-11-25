using System.Text.Json;
using ShulkerRDK.Prismarine.Services;
using ShulkerRDK.Shared;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;
using TridentCore.Core.Utilities;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineContext {
    const string ConfigPath = "./shulker/prismarine.json";

    static readonly JsonSerializerOptions LoadOptions = new(Tools.JsonSerializerOptions) {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static PrismarineContext? _instance;
    static Profile? _profileTemplate;

    public static PrismarineContext Instance => _instance ??= Load();

    public static Profile ProfileTemplate => _profileTemplate ??= LoadProfile();

    public static Filter GetLimiter(ResourceKind? kind = null, ReleaseType? type = null) {
        return new Filter(
            ProfileTemplate.Setup.Version,
            LoaderResolver.ToFilterLoader(ProfileTemplate.Setup.Loader),
            kind,
            type);
    }

    public string ProfilePath { get; init; } = "./shulker/trident.profile.json";

    public static Profile CreateProfile() {
        //todo: 添加交互逻辑, 询问用户版本加载器和杂七杂八的信息
        return new Profile {
            Name = "ShulkerRDK Trident Template",
            Setup = new Profile.Rice {
                Version = "1.21.1"
            }
        };
    }

    public static Profile LoadProfile() {
        if (File.Exists(Instance.ProfilePath))
            return JsonSerializer.Deserialize<Profile>(File.ReadAllText(Instance.ProfilePath), FileHelper.SerializerOptions)
                   ?? throw new Exception("Profile template is invalid!");
        Profile prof = CreateProfile();
        SaveProfile(prof);
        return prof;
    }

    public static void SaveProfile(Profile? prof = null) {
        prof ??= ProfileTemplate;
        string? dir = Path.GetDirectoryName(Instance.ProfilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(Instance.ProfilePath, JsonSerializer.Serialize(prof, Tools.JsonSerializerOptions));
    }

    public PrismarineContext Save() {
        string? dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, Tools.JsonSerializerOptions));
        return this;
    }

    public static PrismarineContext Load() {
        if (!File.Exists(ConfigPath))
            return new PrismarineContext().Save();
        return JsonSerializer.Deserialize<PrismarineContext>(File.ReadAllText(ConfigPath), LoadOptions)
               ?? new PrismarineContext().Save();
    }
}
