using System.Text.Json;
using ShulkerRDK.Shared;

namespace ShulkerRDK.Prismarine.Shared;

public class PrismarineConfig {
    const string ConfigPath = "./shulker/local/prismarineLocal.json";

    static readonly JsonSerializerOptions LoadOptions = new(Tools.JsonSerializerOptions) {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    static PrismarineConfig? _instance;

    public static PrismarineConfig Instance => _instance ??= Load();

    public string DefaultInstance { get; set; } = "dev";
    public string? CurseForgeApiKey { get; set; }
    public LaunchSettings Launch { get; set; } = new();

    public IReadOnlyList<(uint? Major, string Home)> BuildJavaVault(string? overrideJavaHome = null) {
        if (!string.IsNullOrWhiteSpace(overrideJavaHome))
            return [(null, overrideJavaHome)];

        List<(uint? Major, string Home)> vault = [];
        LaunchSettings launch = Launch ?? new LaunchSettings();
        if (!string.IsNullOrWhiteSpace(launch.JavaHome))
            vault.Add((null, launch.JavaHome));

        if (launch.JavaVault is { Count: > 0 }) {
            foreach (KeyValuePair<uint, string> kv in launch.JavaVault) {
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    vault.Add((kv.Key, kv.Value));
            }
        }

        return vault;
    }

    public PrismarineConfig Save() {
        string? dir = Path.GetDirectoryName(ConfigPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, Tools.JsonSerializerOptions));
        return this;
    }

    public static PrismarineConfig Load() {
        if (!File.Exists(ConfigPath))
            return new PrismarineConfig().Save();

        try {
            PrismarineConfig? loaded = JsonSerializer.Deserialize<PrismarineConfig>(
                File.ReadAllText(ConfigPath),
                LoadOptions);
            if (loaded == null)
                return new PrismarineConfig().Save();

            loaded.Launch ??= new LaunchSettings();
            loaded.Launch.JavaVault ??= [];
            if (string.IsNullOrWhiteSpace(loaded.DefaultInstance))
                loaded.DefaultInstance = "dev";

            return loaded;
        }
        catch {
            return new PrismarineConfig();
        }
    }
}

public class LaunchSettings {
    public string Username { get; set; } = "DevPlayer";
    public string? Uuid { get; set; }
    public uint MaxMemory { get; set; } = 4096;
    public uint WindowWidth { get; set; } = 1270;
    public uint WindowHeight { get; set; } = 720;
    public string? QuickConnectAddress { get; set; }
    public string AdditionalArguments { get; set; } = string.Empty;
    public string CommandWrapperTemplate { get; set; } = string.Empty;
    public string? JavaHome { get; set; }
    public Dictionary<uint, string> JavaVault { get; set; } = [];
}
