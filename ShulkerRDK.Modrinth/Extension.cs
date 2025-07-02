using Modrinth;
using ShulkerRDK.Shared;
using Version = Modrinth.Models.Version;

namespace ShulkerRDK.Modrinth;

// ReSharper disable once UnusedType.Global
public class Extension : IShulkerExtension {
    public Extension() {
        #if !DEBUG
        NugetHelper.DependencyVerify("Modrinth.Net/3.5.1");
        #endif

        Commands.Add("mrGet",(_,_)=> {
            ModrinthClient mrClient = new ModrinthClient();
            Task<Version> getTask = mrClient.VersionFile.GetVersionByHashAsync(Utils.GetSha1("./test.jar"));
            getTask.Wait();
            Version version = getTask.Result;
            Terminal.WriteLine("&aModrinth",$"{version.Name} &8{version.ProjectId}@{version.VersionNumber}");
        });
        Commands.Add("mrp",Manager.Command);
        LevitateMethods.Add("mrp",Manager.Method);
    }
    public string Id { get => "shulker.modrinth"; }
    public string Name { get => "ModrinthPSK"; }
    public string Description { get => "添加Modrinth支持"; }
    public string Author { get => "LiPolymer"; }
    public string Version { get => "Dev.Inf"; }
    public string Link { get => "https://github.com/LiPolymer/ShulkerRDK"; }
    public Dictionary<string,Action<string[],ShulkerContext>> Commands { get; } = [];
    public Dictionary<string,Action<string[],ShulkerContext>> StartActions { get; } = [];
    public Dictionary<string,LevitateMethod> LevitateMethods { get; } = [];
    public Dictionary<string,string> CommandAliases { get; } = [];
    public Dictionary<string,string> StartActionAliases { get; } = [];
    public Dictionary<string,string> LevitateAliases { get; } = [];
    public void Init(ShulkerContext context) { }
    public void Shutdown(ShulkerContext context) { }
}