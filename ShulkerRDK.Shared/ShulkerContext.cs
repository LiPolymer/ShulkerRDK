namespace ShulkerRDK.Shared;

public class ShulkerContext {
    public ProjectConfig? ProjectConfig { get; set; }
    public LocalConfig? LocalConfig { get; set; }
    public Dictionary<string,IShulkerExtension> Extensions { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
}