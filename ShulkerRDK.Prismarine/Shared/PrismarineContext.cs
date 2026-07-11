using System.Text.Json;
using ShulkerRDK.Shared;
using TridentCore.Abstractions.FileModels;
using TridentCore.Abstractions.Repositories;
using TridentCore.Abstractions.Repositories.Resources;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineContext {
    const string ConfigPath = "./shulker/prismarine.json";
    
    public static PrismarineContext Instance {
        get {
            field ??= Load();
            return field;
        }
    } = null;
    
    public static Profile ProfileTemplate {
        get {
            field ??= LoadProfile();
            return field;
        }
    } = null;

    public static Filter GetLimiter(ResourceKind? kind = null) {
        return new Filter(ProfileTemplate.Setup.Version, ProfileTemplate.Setup.Loader, kind);
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
            return JsonSerializer.Deserialize<Profile>(File.ReadAllText(Instance.ProfilePath))
                   ?? throw new Exception("Profile template is invalid!");
        Profile prof = CreateProfile();
        SaveProfile(prof);
        return prof;
    }
    
    public static void SaveProfile(Profile? prof = null) {
        prof ??= ProfileTemplate;
        string? dir = Path.GetDirectoryName(Instance.ProfilePath);
        if (dir != null & !Directory.Exists(dir)) 
            Directory.CreateDirectory(dir!);
        File.WriteAllText(Instance.ProfilePath, JsonSerializer.Serialize(prof, Tools.JsonSerializerOptions));
    }
    
    public PrismarineContext Save() {
        string? dir = Path.GetDirectoryName(ConfigPath);
        if (dir != null & !Directory.Exists(dir)) 
            Directory.CreateDirectory(dir!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, Tools.JsonSerializerOptions));
        return this;
    }
    
    public static PrismarineContext Load() {
        if (!File.Exists(ConfigPath)) 
            return new PrismarineContext().Save();
        return JsonSerializer.Deserialize<PrismarineContext>(File.ReadAllText(ConfigPath)) 
               ?? new PrismarineContext().Save();
    }
}