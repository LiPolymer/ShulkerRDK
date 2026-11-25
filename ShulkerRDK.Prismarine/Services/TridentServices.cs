using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ShulkerRDK.Prismarine.Shared;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Exporters;
using TridentCore.Abstractions.Importers;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Engines.Deploying;
using TridentCore.Core.Exporters;
using TridentCore.Core.Extensions;
using TridentCore.Core.Importers;
using TridentCore.Core.Lifetimes;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using ZiggyCreatures.Caching.Fusion;

namespace ShulkerRDK.Prismarine.Services;

public static class TridentServices {
    static ServiceProvider? _provider;
    static LifetimeServiceRuntime? _lifetime;

    public static string TridentHomePath { get; private set; } = string.Empty;

    public static IServiceProvider Provider => _provider ?? throw new InvalidOperationException("TridentServices not initialized");

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();

    public static void Initialize(string? curseForgeApiKey = null) {
        if (_provider != null) throw new InvalidOperationException("Already initialized");

        TridentHomePath = PrepareTridentHome();
        InitializeCore(curseForgeApiKey);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static string PrepareTridentHome() {
        string? existing = Environment.GetEnvironmentVariable("TRIDENT_HOME");
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        string isolated = Path.GetFullPath("./shulker/local/trident");
        Environment.SetEnvironmentVariable("TRIDENT_HOME", isolated);
        return isolated;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void InitializeCore(string? curseForgeApiKey) {
        PathDef.BrandNames = new("shulker.prismarine", "ShulkerRDK.Prismarine", "ink.lipoly.shulker.prismarine");

        if (!Directory.Exists(PathDef.Default.InstanceDirectory)) Directory.CreateDirectory(PathDef.Default.InstanceDirectory);
        if (!Directory.Exists(PathDef.Default.CacheDirectory)) Directory.CreateDirectory(PathDef.Default.CacheDirectory);

        string? effectiveCfKey = !string.IsNullOrWhiteSpace(curseForgeApiKey)
            ? curseForgeApiKey
            : PrismarineConfig.Instance.CurseForgeApiKey;

        ServiceCollection services = new ServiceCollection();

        services.AddLogging(b => {
            b.SetMinimumLevel(LogLevel.Warning);
            b.AddConsole();
        });

        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddFusionCache().WithDefaultEntryOptions(options => options.Duration = TimeSpan.FromDays(7));
        services.AddHttpClient();

        services.AddPrismLauncher()
            .AddMojangLauncher()
            .AddMicrosoft()
            .AddXboxLive()
            .AddMinecraft()
            .AddMclogs()
            .AddAuthlibInjector()
            .AddYggdrasil()
            .AddAccountConfigurers()
            .AddRepositoryInfrastructure()
            .AddLifetimeRuntime();

        services.AddTransient<IProfileImporter, TridentImporter>();
        services.AddTransient<IProfileImporter, CurseForgeImporter>();
        services.AddTransient<IProfileImporter, ModrinthImporter>();
        services.AddTransient<IProfileImporter, MultiMcImporter>();
        services.AddTransient<IProfileImporter, PackwizImporter>();

        services.AddTransient<IProfileExporter, TridentExporter>();
        services.AddTransient<IProfileExporter, CurseForgeExporter>();
        services.AddTransient<IProfileExporter, ModrinthExporter>();
        services.AddTransient<IProfileExporter, MultiMcExporter>();

        services.AddSingleton<ProfileManager>();
        services.AddSingleton<RepositoryAgent>();
        services.AddSingleton<ImporterAgent>();
        services.AddSingleton<ExporterAgent>();
        services.AddSingleton<InstanceManager>();
        services.AddSingleton<IRepositoryProviderAccessor>(_ => new PrismarineRepoProvider(effectiveCfKey));

        // Deploy pipeline services
        services.AddTransient<PackagePlanner>();
        services.AddTransient<ProjectionArbitrator>();
        services.AddTransient<SourceProjectionPlanner>();
        services.AddTransient<DeploymentPlanner>();
        services.AddTransient<DeploymentDiffer>();
        services.AddTransient<DeploymentIndexService>();
        services.AddTransient<PackageResolver>();
        services.AddTransient<PackageMaterializer>();

        _provider = services.BuildServiceProvider();

        _lifetime = _provider.GetRequiredService<LifetimeServiceRuntime>();
        _lifetime.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public static void Shutdown() {
        if (_provider != null) {
            try {
                _provider.GetService<InstanceManager>()?.StopAsync().Wait(TimeSpan.FromSeconds(5));
            }
            catch {
                // Best-effort detach/stop of running instances during shutdown
            }
        }

        _lifetime?.StopAsync(CancellationToken.None).GetAwaiter().GetResult();
        _provider?.Dispose();
        _provider = null;
        _lifetime = null;
    }

    public static ProfileManager ProfileManager => Get<ProfileManager>();
    public static InstanceManager InstanceManager => Get<InstanceManager>();
    public static RepositoryAgent RepositoryAgent => Get<RepositoryAgent>();
    public static ImporterAgent ImporterAgent => Get<ImporterAgent>();
    public static ExporterAgent ExporterAgent => Get<ExporterAgent>();
    public static PrismLauncherService PrismLauncherService => Get<PrismLauncherService>();

    public static IReadOnlyList<(uint? Major, string Home)> DefaultJavaVault => PrismarineConfig.Instance.BuildJavaVault();

    public static async Task<InstanceActivity> WaitForActivityAsync(
        IObservable<InstanceActivity> activities,
        CancellationToken cancellationToken = default) {
        InstanceActivity final = await activities.LastAsync().ToTask(cancellationToken).ConfigureAwait(false);
        return final.State switch {
            ActivityState.Faulted => throw final.FailureReason ?? new InvalidOperationException("Activity faulted"),
            ActivityState.Cancelled => throw new OperationCanceledException("Activity cancelled"),
            _ => final
        };
    }
}
