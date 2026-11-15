using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using ImageMagick;
using ShulkerRDK.Shared;
using Xunit;

namespace ShulkerRDK.ResourceMagick.Tests;

public sealed class ConversionRulesTests : IDisposable {
    readonly string root = Path.Combine(Path.GetTempPath(), "mrvc-rules-test-" + Guid.NewGuid().ToString("N"));
    readonly ShulkerRDK.ResourceMagick.Extension extension = new();
    readonly LevitateInterpreter interpreter;
    readonly ShulkerContext context;

    public ConversionRulesTests() {
        Directory.CreateDirectory(root);
        context = new ShulkerContext { ProjectConfig = new ProjectConfig { RootPath = root, OutPath = root, ProjectName = "conversion-rules" } };
        context.Extensions.Add("shulker.core", new ShulkerRDK.CoreExtension.Extension());
        interpreter = new LevitateInterpreter(context);
        interpreter.Methods.Add("mrvc", extension.LevitateMethods["mrvc"]);
    }

    [Fact]
    public void GuiAndMapOutputsMatchPinnedUpstreamJavaPixelsAndMetadata() {
        string source = Path.Combine(root, "oracle-source");
        Write(source, "pack.mcmeta", """{"pack":{"pack_format":15,"description":"oracle"}}""");
        WritePng(source, "assets/minecraft/textures/gui/widgets.png", 256, 256);
        WritePng(source, "assets/minecraft/textures/map/map_icons.png", 128, 128);
        string destination = Path.Combine(root, "oracle-result");
        Convert(source, destination, "15", "34");
        JsonObject expected = JsonNode.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "Fixtures/rpc-15-to-34.json")))!["files"]!.AsObject();
        string assets = Path.Combine(destination, "assets");
        string[] actualPaths = Directory.EnumerateFiles(assets, "*", SearchOption.AllDirectories)
            .Select(path => Path.GetRelativePath(assets, path).Replace(Path.DirectorySeparatorChar, '/')).Order().ToArray();
        Assert.Equal(expected.Select(pair => pair.Key).Order(), actualPaths);
        foreach ((string relative, JsonNode? description) in expected) {
            string path = Path.Combine(assets, relative);
            if (description!["json"] is JsonNode json) {
                Assert.True(JsonNode.DeepEquals(json, JsonNode.Parse(File.ReadAllText(path))),
                    $"Generated sprite metadata differs from upstream: {relative}");
            } else {
                using MagickImage image = new(path);
                Assert.Equal(description["width"]!.GetValue<uint>(), image.Width);
                Assert.Equal(description["height"]!.GetValue<uint>(), image.Height);
                image.Depth = 8;
                string hash = System.Convert.ToHexString(SHA256.HashData(image.ToByteArray(MagickFormat.Rgba))).ToLowerInvariant();
                Assert.True(hash == description["rgbaSha256"]!.GetValue<string>(),
                    $"Decoded RGBA pixels differ from upstream: {relative}");
            }
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void GuiAndMapAtlasesBecomeScaledSpritesWithMetadata(bool pureAssets, bool zipOutput) {
        string source = Path.Combine(root, "source");
        string prefix = pureAssets ? "" : "assets/";
        if (!pureAssets) Write(source, "pack.mcmeta", """{"pack":{"pack_format":15,"description":"GUI fixture"}}""");
        int scale = zipOutput ? 2 : 1;
        WritePng(source, prefix + "minecraft/textures/gui/widgets.png", 256 * scale, 256 * scale);
        WritePng(source, prefix + "minecraft/textures/map/map_icons.png", 128 * scale, 128 * scale);
        byte[] modAtlas = MakePng(256, 256);
        WriteBytes(source, prefix + "ae2/textures/gui/widgets.png", modAtlas);
        string destination = Path.Combine(root, zipOutput ? "result.ZIP" : "result");

        Convert(source, destination, "15", "34");
        string result = Unpack(destination);
        string textures = Path.Combine(result, prefix + "minecraft/textures/");
        Assert.False(File.Exists(Path.Combine(textures, "gui/widgets.png")));
        Assert.False(File.Exists(Path.Combine(textures, "map/map_icons.png")));
        AssertImage(Path.Combine(textures, "gui/sprites/hud/hotbar.png"), 182 * scale, 22 * scale, 0, 0);
        AssertImage(Path.Combine(textures, "gui/sprites/widget/button_disabled.png"), 200 * scale, 20 * scale, 0, 46 * scale);
        AssertImage(Path.Combine(textures, "map/decorations/frame.png"), 8 * scale, 8 * scale, 8 * scale, 0);
        JsonNode metadata = JsonNode.Parse(File.ReadAllText(Path.Combine(textures, "gui/sprites/widget/button_disabled.png.mcmeta")))!;
        Assert.Equal("nine_slice", metadata["gui"]!["scaling"]!["type"]!.GetValue<string>());
        Assert.Equal(200, metadata["gui"]!["scaling"]!["width"]!.GetValue<int>());
        Assert.Equal(20, metadata["gui"]!["scaling"]!["border"]!["left"]!.GetValue<int>());
        Assert.Equal(modAtlas, File.ReadAllBytes(Path.Combine(result, prefix + "ae2/textures/gui/widgets.png")));
        Assert.False(Directory.Exists(Path.Combine(result, prefix + "ae2/textures/gui/sprites")));
        Assert.Equal(!pureAssets, File.Exists(Path.Combine(result, "pack.mcmeta")));
        if (pureAssets) Assert.False(Directory.Exists(Path.Combine(result, "assets")));
    }

    [Fact]
    public void LegacyNamesModelReferencesBlockstatesAndLanguageConvertTogether() {
        string source = Path.Combine(root, "legacy");
        Write(source, "pack.mcmeta", """{"pack":{"pack_format":3,"description":{"text":"Legacy"}},"custom":{"keep":true}}""");
        WritePng(source, "assets/minecraft/textures/blocks/stonebrick.png", 16, 16);
        Write(source, "assets/minecraft/models/block/stonebrick.json", """{"parent":"block/cube_all","textures":{"all":"blocks/stonebrick"},"custom_marker":"untouched"}""");
        Write(source, "assets/minecraft/blockstates/stonebrick.json", """{"variants":{"normal":{"model":"blocks/stonebrick"}}}""");
        Write(source, "assets/minecraft/lang/en_US.lang", "tile.stone.stone.name=Rock\\nLine\nprobe.message=a=b\n");
        string destination = Path.Combine(root, "modern");

        Convert(source, destination, "3", "4");
        Assert.True(File.Exists(Path.Combine(destination, "assets/minecraft/textures/block/stone_bricks.png")));
        JsonNode model = JsonNode.Parse(File.ReadAllText(Path.Combine(destination, "assets/minecraft/models/block/stone_bricks.json")))!;
        Assert.Equal("block/stone_bricks", model["textures"]!["all"]!.GetValue<string>());
        Assert.Equal("untouched", model["custom_marker"]!.GetValue<string>());
        JsonNode state = JsonNode.Parse(File.ReadAllText(Path.Combine(destination, "assets/minecraft/blockstates/stone_bricks.json")))!;
        Assert.Equal("block/stone_bricks", state["variants"]![""]!["model"]!.GetValue<string>());
        JsonNode language = JsonNode.Parse(File.ReadAllText(Path.Combine(destination, "assets/minecraft/lang/en_us.json")))!;
        Assert.Equal("Rock\nLine", language["block.minecraft.stone"]!.GetValue<string>());
        Assert.Equal("a=b", language["probe.message"]!.GetValue<string>());
        Assert.False(File.Exists(Path.Combine(destination, "assets/minecraft/lang/en_US.lang")));
        JsonNode pack = JsonNode.Parse(File.ReadAllText(Path.Combine(destination, "pack.mcmeta")))!;
        Assert.True(pack["custom"]!["keep"]!.GetValue<bool>());
        Assert.Equal("Legacy", pack["pack"]!["description"]!["text"]!.GetValue<string>());
    }

    [Fact]
    public void ModernLanguageConvertsBackToLegacyKeysAndEscapedValues() {
        string source = Path.Combine(root, "modern-language");
        Write(source, "pack.mcmeta", """{"pack":{"pack_format":4,"description":"Language"}}""");
        Write(source, "assets/minecraft/lang/en_us.json", """{"block.minecraft.stone":"Rock","probe.message":"a=b"}""");
        string destination = Path.Combine(root, "legacy-language");
        Convert(source, destination, "4", "3");
        string language = File.ReadAllText(Path.Combine(destination, "assets/minecraft/lang/en_US.lang"));
        Assert.Contains("tile.stone.stone.name=Rock", language);
        Assert.Contains("probe.message=a=b", language);
        Assert.False(File.Exists(Path.Combine(destination, "assets/minecraft/lang/en_us.json")));
    }

    [Fact]
    public void UnsupportedNewVersionDoesNotProduceRelabeledPack() {
        string source = Path.Combine(root, "unsupported");
        Write(source, "pack.mcmeta", """{"pack":{"pack_format":15,"description":"Unsupported"}}""");
        string destination = Path.Combine(root, "unsupported-result");
        Assert.Throws<FormatException>(() => Convert(source, destination, "15", "97.1"));
        Assert.False(Path.Exists(destination));
    }

    [Fact]
    public void UnsupportedBackwardGuiReassemblyFailsWithoutPublishingOutput() {
        string source = Path.Combine(root, "sprites");
        Write(source, "pack.mcmeta", """{"pack":{"pack_format":18,"description":"Sprites"}}""");
        WritePng(source, "assets/minecraft/textures/gui/sprites/widget/button.png", 200, 20);
        string destination = Path.Combine(root, "backward-result.zip");
        Assert.Throws<InvalidDataException>(() => Convert(source, destination, "18", "15"));
        Assert.False(Path.Exists(destination));
    }

    [Fact]
    public void AmbiguousSameFormatInventoryDoesNotGuessSourceGameVersion() {
        string source = Path.Combine(root, "ambiguous");
        Write(source, "pack.mcmeta", """{"pack":{"pack_format":4,"description":"Inventory"}}""");
        WritePng(source, "assets/minecraft/textures/gui/container/inventory.png", 256, 256);
        string destination = Path.Combine(root, "ambiguous-result");
        Assert.Throws<InvalidDataException>(() => Convert(source, destination, "4", "5"));
        Assert.False(Path.Exists(destination));
    }

    [Fact]
    public void InvalidAtlasDoesNotLeavePartialSpritesOrDestination() {
        string source = Path.Combine(root, "bad-atlas");
        Write(source, "minecraft/textures/gui/widgets.png", "not a PNG");
        string destination = Path.Combine(root, "bad-result");
        Assert.ThrowsAny<MagickException>(() => Convert(source, destination, "15", "34"));
        Assert.False(Path.Exists(destination));
        Assert.Equal("not a PNG", File.ReadAllText(Path.Combine(source, "minecraft/textures/gui/widgets.png")));
    }

    void Convert(string source, string destination, string original, string target) {
        interpreter.ExecuteMethod(["mrvc", source, destination, original, target], new LevitateExecutionContext {
            WorkingDir = root, CurrentLine = 1, Logger = new LevitateLogger(1, "conversion-rules"),
            ShulkerContext = context, Interpreter = interpreter, EnvVars = interpreter.EnvVars, Vars = []
        });
    }

    string Unpack(string path) {
        if (Directory.Exists(path)) return path;
        string directory = path + ".unpacked";
        ZipFile.ExtractToDirectory(path, directory);
        return directory;
    }

    static void AssertImage(string path, int width, int height, int sourceX, int sourceY) {
        using MagickImage image = new(path);
        Assert.Equal((uint)width, image.Width);
        Assert.Equal((uint)height, image.Height);
        using IPixelCollection<ushort> pixels = image.GetPixels();
        IPixel<ushort> pixel = pixels.GetPixel(0, 0);
        Assert.Equal((ushort)((sourceX % 251) * 257), pixel.GetChannel(0));
        Assert.Equal((ushort)((sourceY % 251) * 257), pixel.GetChannel(1));
        Assert.Equal((ushort)(((sourceX + sourceY) % 251) * 257), pixel.GetChannel(2));
    }

    static void WritePng(string directory, string path, int width, int height) => WriteBytes(directory, path, MakePng(width, height));
    static void Write(string directory, string path, string value) => WriteBytes(directory, path, Encoding.UTF8.GetBytes(value));
    static void WriteBytes(string directory, string path, byte[] value) {
        string full = Path.Combine(directory, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, value);
    }

    static byte[] MakePng(int width, int height) {
        using MemoryStream result = new();
        result.Write(new byte[] {137, 80, 78, 71, 13, 10, 26, 10});
        byte[] header = new byte[13];
        BinaryPrimitives.WriteUInt32BigEndian(header, (uint)width);
        BinaryPrimitives.WriteUInt32BigEndian(header.AsSpan(4), (uint)height);
        header[8] = 8; header[9] = 6;
        Chunk(result, "IHDR", header);
        using MemoryStream compressed = new();
        using (ZLibStream zlib = new(compressed, CompressionLevel.Optimal, true)) {
            byte[] row = new byte[width * 4 + 1];
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    row[x * 4 + 1] = (byte)(x % 251);
                    row[x * 4 + 2] = (byte)(y % 251);
                    row[x * 4 + 3] = (byte)((x + y) % 251);
                    row[x * 4 + 4] = 255;
                }
                zlib.Write(row);
            }
        }
        Chunk(result, "IDAT", compressed.ToArray());
        Chunk(result, "IEND", []);
        return result.ToArray();
    }

    static void Chunk(Stream output, string type, byte[] content) {
        byte[] name = Encoding.ASCII.GetBytes(type);
        Span<byte> word = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(word, (uint)content.Length);
        output.Write(word); output.Write(name); output.Write(content);
        uint crc = 0xffffffff;
        foreach (byte value in name.Concat(content)) {
            crc ^= value;
            for (int bit = 0; bit < 8; bit++) crc = (crc >> 1) ^ ((crc & 1) != 0 ? 0xedb88320u : 0);
        }
        BinaryPrimitives.WriteUInt32BigEndian(word, ~crc);
        output.Write(word);
    }

    public void Dispose() => Directory.Delete(root, true);
}
