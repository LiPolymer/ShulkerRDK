using System.Text.Json;

namespace ShulkerRDK.Shared;

public class ProjectConfig {
    public event Action? OnPropertyChanged;
    string _projectName = string.Empty;
    public string ProjectName {
        get => _projectName;
        set {
            _projectName = value;
            OnPropertyChanged?.Invoke();
        }
    }

    string _version = "0.0.0";

    public string Version {
        get => _version;
        set {
            _version = value;
            OnPropertyChanged?.Invoke();
        }
    }

    string _rootPath = string.Empty;

    public string RootPath {
        get => _rootPath;
        set {
            _rootPath = value;
            OnPropertyChanged?.Invoke();
        }
    }

    string _outPath = string.Empty;

    public string OutPath {
        get => _outPath;
        set {
            _outPath = value;
            OnPropertyChanged?.Invoke();
        }
    }

    public Dictionary<string,string> DefaultEnvVars { get; init; } = [];

    public void Save() {
        Tools.WriteAllText(StaticContext.Paths.ProjectConfig,JsonSerializer.Serialize(this,Tools.JsonSerializerOptions));
    }

    public static ProjectConfig? Load() {
        return JsonSerializer.Deserialize<ProjectConfig>(File.ReadAllText(StaticContext.Paths.ProjectConfig));
    }
}