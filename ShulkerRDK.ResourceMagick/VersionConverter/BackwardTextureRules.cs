// Geometry ported from ResourcePackConverter (MIT), agentdid127 and Hypixel.
using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using ImageMagick;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class BackwardTextureRules
{
    private const string Textures = "assets/minecraft/textures/";

    public static void Apply(TextureStage stage, ConversionWorkspace workspace)
    {
        if (workspace.Forward)
            throw new InvalidOperationException("Backward texture rules require a backwards conversion.");
        switch (stage)
        {
            case TextureStage.EnchantPath:
                if (workspace.Crosses(762)) EnchantPath(workspace);
                break;
            case TextureStage.ConduitParticles:
                if (workspace.Crosses(757))
                {
                    const string source = "assets/minecraft/particles/block_marker.json";
                    if (workspace.Exists(source))
                        workspace.Move(source, "assets/minecraft/particles/light.json");
                }
                break;
            case TextureStage.Inventory:
                if (workspace.Crosses(757)) Inventory(workspace);
                break;
            case TextureStage.Glint:
                if (workspace.Crosses(573)) Glint(workspace);
                break;
            case TextureStage.Chests:
                if (workspace.Crosses(573)) Chests(workspace);
                break;
            case TextureStage.Paintings:
                if (workspace.Crosses(477)) RebuildAtlas(workspace, true);
                break;
            case TextureStage.Particles:
                Particles(workspace);
                break;
            case TextureStage.MapIcons:
                if (workspace.Crosses(393)) MapIcons(workspace);
                break;
            case TextureStage.Water:
                if (workspace.Crosses(393)) Water(workspace);
                break;
            case TextureStage.Compass:
                if (workspace.Crosses(107)) Compass(workspace);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(stage), stage, "No registered backwards texture converter.");
        }
    }

    private static void EnchantPath(ConversionWorkspace workspace)
    {
        const string misc = Textures + "misc/";
        foreach (string suffix in new[] { ".png", ".png.mcmeta" })
        {
            string item = misc + "enchanted_glint_item" + suffix;
            string entity = misc + "enchanted_glint_entity" + suffix;
            string target = misc + "enchanted_item_glint" + suffix;
            if (workspace.Exists(item)) workspace.Move(item, target, overwrite: true);
            else if (workspace.Exists(entity)) workspace.Move(entity, target, overwrite: true);
            workspace.Delete(item);
            workspace.Delete(entity);
        }
    }

    private static void Inventory(ConversionWorkspace workspace)
    {
        const string path = Textures + "gui/container/inventory.png";
        if (!workspace.Exists(path)) return;
        if (workspace.AmbiguousSourceAt(757))
            throw new InvalidDataException("The source pack format aliases pre-1.18 and 1.18 inventory layouts; inventory.png cannot be safely disambiguated.");
        using var image = new Raster(256, 256, workspace.PathFor(path));
        image.NewImage(256, 256);
        image.SubImage(0, 0, 256, 256);
        image.SubImage(0, 198, 16, 230, 0, 166);
        image.SubImage(16, 198, 32, 230, 104, 166);
        image.Store();
        workspace.RecordImage();
    }

    private static void Glint(ConversionWorkspace workspace)
    {
        const string path = Textures + "misc/enchanted_item_glint.png";
        if (!workspace.Exists(path)) return;
        using var image = new Raster(64, 64, workspace.PathFor(path));
        if (!image.IsPowerOfTwo) return;
        image.NewImage(64, 64);
        image.Grayscale();
        image.Store();
        workspace.RecordImage();
    }

    private static void Chests(ConversionWorkspace workspace)
    {
        foreach (string name in new[] { "normal", "trapped", "christmas" }) DoubleChest(workspace, name);
        foreach (string name in new[] { "normal", "trapped", "christmas", "ender" }) Chest(workspace, name);
    }

    private static void Chest(ConversionWorkspace workspace, string name)
    {
        string path = Textures + "entity/chest/" + name + ".png";
        if (!workspace.Exists(path)) return;
        using var normal = new Raster(64, 64, workspace.PathFor(path));
        if (!normal.IsPowerOfTwo) return;
        normal.NewImage(64, 64);
        normal.SubImage(28, 0, 42, 14, 14, 0, true);
        normal.SubImage(14, 0, 28, 14, 28, 0, true);
        normal.SubImage(14, 19, 28, 33, 28, 19, true);
        normal.SubImage(28, 19, 42, 33, 14, 19, true);
        normal.SubImage(28, 14, 42, 19, 28, 14, 1);
        normal.SubImage(42, 14, 56, 19, 14, 14, 1);
        normal.SubImage(14, 14, 28, 19, 42, 14, 1);
        normal.SubImage(0, 14, 14, 19, 0, 14, 1);
        normal.SubImage(0, 33, 14, 43, 0, 33, 1);
        normal.SubImage(42, 33, 56, 43, 14, 33, 1);
        normal.SubImage(28, 33, 42, 43, 28, 33, 1);
        normal.SubImage(14, 33, 28, 43, 42, 33, 1);
        normal.SubImage(0, 1, 1, 5, 0, 1, true);
        normal.SubImage(1, 0, 3, 1, 3, 0);
        normal.SubImage(3, 0, 5, 1, 1, 0);
        normal.SubImage(4, 1, 6, 5, 1, 1, 1);
        normal.SubImage(1, 1, 4, 4, 3, 1, 1);
        normal.Store();
        workspace.RecordImage();
    }

    private static void DoubleChest(ConversionWorkspace workspace, string name)
    {
        string prefix = Textures + "entity/chest/" + name;
        string left = prefix + "_left.png", right = prefix + "_right.png", combined = prefix + "_double.png";
        if (workspace.Exists(left) != workspace.Exists(right))
            throw new InvalidDataException($"Rebuilding {name} double chest requires both left and right textures.");
        if (workspace.Exists(left) && workspace.Exists(right))
        {
            using var setup = new Raster(64, 64, workspace.PathFor(left));
            using var other = new Raster(64, 64, workspace.PathFor(right));
            if (setup.Width != other.Width || setup.Height != other.Height || !setup.IsSquare)
                throw new InvalidDataException($"The {name} chest halves must be square and have matching dimensions.");
            setup.NewImage(128, 64);
            setup.SubImage(0, 0, 64, 64);
            setup.AddImage(workspace.PathFor(right), 64, 0);
            setup.Store(workspace.PathFor(combined));
        }
        if (!workspace.Exists(combined)) return;
        using var normal = new Raster(128, 64, workspace.PathFor(combined));
        if (!normal.IsPowerOfTwo) return;
        normal.NewImage(128, 64);
        normal.SubImage(14, 19, 29, 33, 59, 19, true);
        normal.SubImage(14, 0, 29, 14, 59, 0, true);
        normal.SubImage(29, 19, 44, 33, 29, 19, true);
        normal.SubImage(43, 14, 58, 19, 29, 14, 1);
        normal.SubImage(29, 0, 44, 14, 29, 0, true);
        normal.SubImage(29, 33, 43, 43, 44, 33, 1);
        normal.SubImage(43, 33, 58, 43, 29, 33, 1);
        normal.SubImage(14, 33, 29, 43, 58, 33, 1);
        normal.SubImage(29, 14, 43, 19, 44, 14, 1);
        normal.SubImage(14, 14, 29, 19, 58, 14, 1);
        normal.SubImage(1, 0, 2, 1, 4, 0);
        normal.SubImage(2, 0, 3, 1, 2, 0);
        normal.SubImage(1, 1, 4, 5, 2, 1, 1);
        normal.SubImage(78, 0, 93, 14, 44, 0, true);
        normal.SubImage(93, 0, 108, 14, 14, 0, true);
        normal.SubImage(43, 19, 58, 33, 14, 19, true);
        normal.SubImage(64, 14, 78, 19, 0, 14, 1);
        normal.SubImage(78, 14, 93, 19, 73, 14, 1);
        normal.SubImage(107, 14, 122, 19, 14, 14, 1);
        normal.SubImage(78, 19, 93, 33, 44, 19, true);
        normal.SubImage(107, 33, 122, 43, 14, 33, 1);
        normal.SubImage(64, 33, 78, 43, 0, 33, 1);
        normal.SubImage(78, 33, 93, 43, 73, 33, 1);
        normal.SubImage(65, 0, 66, 1, 3, 0);
        normal.SubImage(67, 0, 68, 1, 1, 0);
        normal.SubImage(64, 1, 65, 5, 0, 1, true);
        normal.SubImage(65, 1, 66, 5, 5, 1, true);
        normal.SubImage(68, 1, 69, 5, 1, 1, true);
        normal.Store();
        workspace.RecordImage();
        workspace.Delete(left);
        workspace.Delete(right);
    }

    // The cached tables supply sprite dimensions as well as placement. Backwards painting
    // deliberately places the back at 192,0, unlike the forwards atlas's 240,0.
    private static void RebuildAtlas(ConversionWorkspace workspace, bool painting)
    {
        string folder = Textures + (painting ? "painting/" : "particle/");
        string output = folder + (painting ? "paintings_kristoffer_zetterstrand.png" : "particles.png");
        JsonArray entries = RuleData.Json(painting ? "forwards/paintings_kristoffer_zetterstrand.json" : "forwards/particles.json")["textures"]!.AsArray();
        var sprites = entries.Select(node =>
        {
            JsonObject entry = node!.AsObject();
            string name = entry["path"]!.GetValue<string>();
            JsonObject box = entry["box"]!.AsObject();
            string path = !painting && name == "fishing_hook.png" ? Textures + "entity/" + name : folder + name;
            return (Path: path, X: painting && name == "back.png" ? 192 : box["x"]!.GetValue<int>(),
                Y: box["y"]!.GetValue<int>(), Width: box["width"]!.GetValue<int>(), Height: box["height"]!.GetValue<int>());
        }).Where(sprite => workspace.Exists(sprite.Path)).ToArray();
        if (sprites.Length == 0) return;
        if (workspace.AmbiguousSourceAt(477))
            workspace.Warn("The source pack format aliases 1.13 and 1.14; reconstructing the atlas from present individual sprites only.");
        var first = sprites[0];
        using var image = new Raster(first.Width, first.Height, workspace.PathFor(first.Path));
        image.NewImage(256, 256);
        foreach (var sprite in sprites)
            image.AddImage(workspace.PathFor(sprite.Path), sprite.X, sprite.Y);
        image.Store(workspace.PathFor(output));
        workspace.RecordImage();
        if (painting)
            foreach (var sprite in sprites) workspace.Delete(sprite.Path);
    }

    private static void Particles(ConversionWorkspace workspace)
    {
        if (workspace.Crosses(477)) RebuildAtlas(workspace, false);
        const string path = Textures + "particle/particles.png";
        // The old atlas is 128 square; crop the modern 256 grid, not a self-scaled 128 grid.
        if (workspace.FromProtocol < 340 || workspace.ToProtocol >= 393 || !workspace.Exists(path)) return;
        int reference = workspace.FromProtocol >= 393 ? 256 : 128;
        using var image = new Raster(reference, reference, workspace.PathFor(path));
        if (!image.IsPowerOfTwo) return;
        image.NewImage(128, 128);
        image.SubImage(0, 0, 128, 128);
        image.Store();
        workspace.RecordImage();
    }

    private static void MapIcons(ConversionWorkspace workspace)
    {
        const string path = Textures + "map/map_icons.png";
        if (!workspace.Exists(path)) return;
        using var image = new MagickImage(workspace.PathFor(path));
        if (image.Width < 128 || image.Width % 128 != 0 || image.Height != image.Width)
            throw new InvalidDataException("The modern map icon atlas must be square with a width divisible by 128.");
        int scale = checked((int)image.Width) / 128;
        using var target = new MagickImage(RuleData.Bytes("backwards/map_icons.png"));
        // Correct Java's extra backwards/ path, legacy source scale and unreachable mappings.
        target.FilterType = FilterType.Point;
        target.Resize((uint)(32 * scale), (uint)(32 * scale));
        for (int i = 0; i < 10; i++)
        {
            using var tile = image.Clone(new MagickGeometry(i * 8 * scale, 0, (uint)(8 * scale), (uint)(8 * scale)));
            tile.ResetPage();
            target.Composite(tile, i % 4 * 8 * scale, i / 4 * 8 * scale, CompositeOperator.Over);
        }
        target.Write(workspace.PathFor(path), MagickFormat.Png);
        workspace.RecordImage();
    }

    private static void Water(ConversionWorkspace workspace)
    {
        Colorize("water_flow.png", 32, 1024);
        Colorize("water_still.png", 16, 512);
        Colorize("water_overlay.png", 16, 16);
        void Colorize(string name, int width, int height)
        {
            string path = Textures + "blocks/" + name;
            if (!workspace.Exists(path)) return;
            using var image = new Raster(width, height, workspace.PathFor(path));
            if (!image.IsPowerOfTwo) return;
            image.ColorizeGrayscale(45, 63, 244);
            image.Store();
            workspace.RecordImage();
        }
    }

    private static void Compass(ConversionWorkspace workspace)
    {
        const string folder = Textures + "items/";
        string[] frames = Enumerable.Range(0, 32).Select(i => folder + "compass_" +
            i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ".png").ToArray();
        int present = frames.Count(workspace.Exists);
        if (present == 0) return;
        if (present != 32)
            throw new InvalidDataException("Rebuilding the legacy compass requires all 32 compass_00.png through compass_31.png frames.");
        using var image = new Raster(16, 16, workspace.PathFor(frames[0]));
        if (!image.IsSquare)
            throw new InvalidDataException("Compass frames must be square.");
        foreach (string frame in frames.Skip(1))
        {
            using var check = new Raster(16, 16, workspace.PathFor(frame));
            if (check.Width != image.Width || check.Height != image.Height)
                throw new InvalidDataException("Compass frames must all have the same dimensions.");
        }
        // Java's backwards converter mistakenly splits a legacy strip again. Reassemble
        // its actual forwards compass_XX outputs instead, without dropping any frame.
        image.NewImage(16, 512);
        for (int i = 0; i < frames.Length; i++)
            image.AddImage(workspace.PathFor(frames[i]), 0, i * 16);
        string output = folder + "compass.png";
        image.Store(workspace.PathFor(output));
        workspace.RecordImage();
        var indices = new JsonArray();
        for (int i = 0; i < 32; i++) indices.Add(i);
        workspace.WriteJson(output + ".mcmeta", new JsonObject
        {
            ["animation"] = new JsonObject { ["frametime"] = 1, ["frames"] = indices }
        });
        foreach (string frame in frames)
        {
            workspace.Delete(frame);
            workspace.Delete(frame + ".mcmeta");
        }
    }
}
