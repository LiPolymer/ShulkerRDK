using System.Text.Json;

namespace ShulkerRDK.Shared;

public class ProjectConfig {
    public string ProjectName { get; init; } = string.Empty;
    public string RootPath { get; init; } = string.Empty;
    public string OutPath { get; init; } = string.Empty;
    public Dictionary<string,string> DefaultEnvVars { get; set; } = [];

    public void Save() {
        Tools.WriteAllText(StaticContext.Paths.ProjectConfig,JsonSerializer.Serialize(this,Tools.JsonSerializerOptions));
    }

    public static ProjectConfig? Load() {
        return JsonSerializer.Deserialize<ProjectConfig>(File.ReadAllText(StaticContext.Paths.ProjectConfig));
    }
}