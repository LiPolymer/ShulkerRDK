namespace ShulkerRDK.Shared;

public interface IShulkerExtension {
    public string Id { get; }
    public string Name { get; }
    public string Description { get; }
    public string Author { get; }
    public string Version { get; }
    public string Link { get; }
    public string? AsciiArt { get; }
    
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; }
    public Dictionary<string, Action<string[],ShulkerContext>> StartActions { get; }
    public Dictionary<string,LevitateMethod> LevitateMethods { get; }
    public Dictionary<string,string> CommandAliases { get; }
    public Dictionary<string,string> StartActionAliases { get; }
    public Dictionary<string,string> LevitateAliases { get; }
    
    public void Init(ShulkerContext context);
    public void Shutdown(ShulkerContext context);
}