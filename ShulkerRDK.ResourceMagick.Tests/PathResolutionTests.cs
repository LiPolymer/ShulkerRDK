using System.IO.Compression;
using ShulkerRDK.Shared;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace ShulkerRDK.ResourceMagick.Tests;

public sealed class PathResolutionTests : IDisposable {
    private readonly string originalDirectory = Environment.CurrentDirectory;
    private readonly string projectDirectory = Path.Combine(Path.GetTempPath(), "mrvc-path-test-" + Guid.NewGuid().ToString("N"));
    private readonly byte[] payload = [0x13, 0x24, 0x19, 0x34];

    public PathResolutionTests() {
        string input = Path.Combine(projectDirectory, "shulker/local/cache/build/assetsLegacy/minecraft/textures/block/probe.bin");
        Directory.CreateDirectory(Path.GetDirectoryName(input)!);
        File.WriteAllBytes(input, payload);
        Directory.CreateDirectory(Path.Combine(projectDirectory, "shulker/tasks"));
        Environment.CurrentDirectory = projectDirectory;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CachePathsRemainProjectRelativeFromNestedLevitateScript(bool zipOutput) {
        ShulkerContext context = new ShulkerContext {
            ProjectConfig = new ProjectConfig { RootPath = "./src", OutPath = "./build", ProjectName = "mrvc-path-test" }
        };
        context.Extensions.Add("shulker.core", new CoreExtension.Extension());
        ResourceMagick.Extension extension = new ResourceMagick.Extension();
        LevitateInterpreter interpreter = new LevitateInterpreter(context);
        interpreter.Methods.Add("mrvc", extension.LevitateMethods["mrvc"]);
        string destination = "./shulker/local/cache/build/method-output" + (zipOutput ? ".zip" : "");
        string sentence = $"mrvc \"%project.cache%/assetsLegacy\" \"{destination}\" 15 34";
        string expanded = Tools.EscapeDictResolver(sentence, interpreter.EnvVars, "%");
        LevitateExecutionContext execution = new LevitateExecutionContext {
            WorkingDir = Path.Combine(projectDirectory, "shulker/tasks"),
            CurrentLine = 6,
            Logger = new LevitateLogger(6, "merge"),
            ShulkerContext = context,
            Interpreter = interpreter,
            EnvVars = interpreter.EnvVars,
            Vars = []
        };

        interpreter.ExecuteMethod(Tools.ResolveArgs(expanded), execution);
        AssertPayload(Path.GetFullPath(destination), zipOutput);

        string commandDestination = "./shulker/local/cache/build/command-output" + (zipOutput ? ".zip" : "");
        extension.Commands["mrvc"](["mrvc", "./shulker/local/cache/build/assetsLegacy", commandDestination, "15", "34"], context);
        AssertPayload(Path.GetFullPath(commandDestination), zipOutput);
        Assert.False(Directory.Exists(Path.Combine(projectDirectory, "shulker/tasks/shulker")));
        Assert.Equal(payload, File.ReadAllBytes(Path.Combine(projectDirectory, "shulker/local/cache/build/assetsLegacy/minecraft/textures/block/probe.bin")));
    }

    private void AssertPayload(string destination, bool zip) {
        if (zip) {
            using ZipArchive archive = ZipFile.OpenRead(destination);
            ZipArchiveEntry? entry = archive.GetEntry("minecraft/textures/block/probe.bin");
            Assert.NotNull(entry);
            using Stream input = entry.Open();
            using MemoryStream actual = new MemoryStream();
            input.CopyTo(actual);
            Assert.Equal(payload, actual.ToArray());
            Assert.Null(archive.GetEntry("pack.mcmeta"));
        } else {
            Assert.Equal(payload, File.ReadAllBytes(Path.Combine(destination, "minecraft/textures/block/probe.bin")));
            Assert.False(File.Exists(Path.Combine(destination, "pack.mcmeta")));
        }
    }

    public void Dispose() {
        Environment.CurrentDirectory = originalDirectory;
        Directory.Delete(projectDirectory, true);
    }
}
