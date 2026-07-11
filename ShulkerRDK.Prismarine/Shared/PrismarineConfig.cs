using System.Text.Json;
using ShulkerRDK.Shared;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineConfig {
    const string ConfigPath = "./shulker/local/prismarineLocal.json";
    
    public static PrismarineConfig Instance {
        get {
            field ??= Load();
            return field;
        }
    } = null;
    
    public PrismarineConfig Save() {
        string? dir = Path.GetDirectoryName(ConfigPath);
        if (dir != null & !Directory.Exists(dir)) 
            Directory.CreateDirectory(dir!);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, Tools.JsonSerializerOptions));
        return this;
    }
    
    public static PrismarineConfig Load() {
        if (!File.Exists(ConfigPath)) 
            return new PrismarineConfig().Save();
        return JsonSerializer.Deserialize<PrismarineConfig>(File.ReadAllText(ConfigPath)) 
               ?? new PrismarineConfig().Save();
    }
}