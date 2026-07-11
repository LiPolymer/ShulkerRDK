using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TridentCore.Abstractions;
using TridentCore.Abstractions.Exporters;
using TridentCore.Abstractions.Importers;
using TridentCore.Abstractions.Tasks;
using TridentCore.Core.Exporters;
using TridentCore.Core.Extensions;
using TridentCore.Core.Importers;
using TridentCore.Core.Lifetimes;
using TridentCore.Core.Services;
using TridentCore.Core.Services.Instances;
using TridentCore.Core.Utilities;

namespace ShulkerRDK.Prismarine.Services;

public static class TridentServices {
    static ServiceProvider? _provider;
    static LifetimeServiceRuntime? _lifetime;

    public static IServiceProvider Provider => _provider ?? throw new InvalidOperationException("TridentServices not initialized");

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();

    public static void Initialize(string? curseForgeApiKey = null) {
        if (_provider != null) throw new InvalidOperationException("Already initialized");

        string dir = PathDef.Default.Home;
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        if (!Directory.Exists(PathDef.Default.InstanceDirectory)) Directory.CreateDirectory(PathDef.Default.InstanceDirectory);
        if (!Directory.Exists(PathDef.Default.CacheDirectory)) Directory.CreateDirectory(PathDef.Default.CacheDirectory);

        ServiceCollection services = new ServiceCollection();

        services.AddLogging(b => {
            b.SetMinimumLevel(LogLevel.Warning);
            b.AddConsole();
        });

        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddHttpClient();

        services.AddPrismLauncher()
            .AddMojangLauncher()
            .AddMicrosoft()
            .AddXboxLive()
            .AddMinecraft()
            .AddMclogs()
            .AddLifetimeRuntime();

        services.AddTransient<IProfileImporter,TridentImporter>();
        services.AddTransient<IProfileImporter,CurseForgeImporter>();
        services.AddTransient<IProfileImporter,ModrinthImporter>();
        services.AddTransient<IProfileExporter,TridentExporter>();
        services.AddTransient<IProfileExporter,CurseForgeExporter>();
        services.AddTransient<IProfileExporter,ModrinthExporter>();

        services.AddSingleton<ProfileManager>();
        services.AddSingleton<RepositoryAgent>();
        services.AddSingleton<ImporterAgent>();
        services.AddSingleton<ExporterAgent>();
        services.AddSingleton<InstanceManager>();
        services.AddSingleton<IRepositoryProviderAccessor>(_ => new PrismarineRepoProvider(curseForgeApiKey));

        // Deploy pipeline stages
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.CheckArtifactStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.InstallVanillaStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.ProcessLoaderStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.ResolvePackageStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.BuildArtifactStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.EnsureRuntimeStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.GenerateManifestStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.Stages.SolidifyManifestStage>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.PackagePlanner>();
        services.AddTransient<TridentCore.Core.Engines.Deploying.PackageMaterializer>();

        _provider = services.BuildServiceProvider();

        _lifetime = _provider.GetRequiredService<LifetimeServiceRuntime>();
        _lifetime.StartAsync(CancellationToken.None).GetAwaiter().GetResult();
    }

    public static void Shutdown() {
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

    public static JavaHomeLocatorDelegate DefaultJavaLocator => JavaHelper.MakeLocator(_ => null);

    public static Task WaitForTrackerAsync(TrackerBase tracker) {
        TaskCompletionSource tcs = new TaskCompletionSource();

        if (tracker.State == TrackerState.Finished) {
            tcs.TrySetResult();
            return tcs.Task;
        }

        if (tracker.State == TrackerState.Faulted) {
            tcs.TrySetException(tracker.FailureReason ?? new InvalidOperationException("Tracker faulted"));
            return tcs.Task;
        }

        tracker.StateUpdated += (_,state) => {
            switch (state) {
                case TrackerState.Finished:
                    tcs.TrySetResult();
                    break;
                case TrackerState.Faulted:
                    tcs.TrySetException(tracker.FailureReason ?? new InvalidOperationException("Tracker faulted"));
                    break;
            }
        };

        return tcs.Task;
    }
}