namespace ShulkerRDK.Shared;

public abstract class ExtensionBase: IShulkerExtension {
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Author { get; }
    public abstract string Version { get; }
    public abstract string Link { get; }
    public virtual string? Donating { get => null; }
    public virtual string? Document { get => null; }
    public virtual string? AsciiArt { get => null; }
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,LevitateMethod> LevitateMethods { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
    public Dictionary<string,string> LevitateAliases { get; } = [];
    public virtual void Init(ShulkerContext context) { }
    public virtual void Shutdown(ShulkerContext context) { }
}