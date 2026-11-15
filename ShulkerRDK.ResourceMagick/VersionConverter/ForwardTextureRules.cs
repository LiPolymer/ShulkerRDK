// Port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System;
using System.IO;
using System.Linq;
using ImageMagick;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class ForwardTextureRules {
    private const string Textures = "assets/minecraft/textures/";

    public static void Apply(TextureStage stage, ConversionWorkspace workspace) {
        switch (stage) {
            case TextureStage.Compass:
                if (workspace.FromProtocol <= 47 && workspace.ToProtocol >= 107) Compass(workspace);
                break;
            case TextureStage.Offhand:
                if (workspace.FromProtocol <= 47 && workspace.ToProtocol >= 107) Offhand(workspace);
                break;
            case TextureStage.MapIcons:
                if (workspace.FromProtocol <= 340 && workspace.ToProtocol >= 393) LegacyMapIcons(workspace);
                break;
            case TextureStage.Water:
                if (workspace.FromProtocol <= 340 && workspace.ToProtocol >= 393) Water(workspace);
                break;
            case TextureStage.Particles: Particles(workspace); break;
            case TextureStage.Paintings:
                if (workspace.ToProtocol >= 498 && workspace.MinimumSourceProtocol <= 393) {
                    WarnLegacyAlias(workspace, 498, Textures + "painting/paintings_kristoffer_zetterstrand.png");
                    SliceRules.Run(workspace, "paintings_kristoffer_zetterstrand.json", Textures + "painting", ignorePredicates: true);
                }
                break;
            case TextureStage.MobEffects:
                if (workspace.ToProtocol >= 477 && workspace.MinimumSourceProtocol <= 404 && workspace.Exists(Textures + "gui/container/inventory.png")) {
                    if (workspace.AmbiguousSourceAt(477))
                        throw new InvalidDataException("Cannot determine whether inventory.png uses pre-1.14 mob-effect slots: source pack format covers both layouts.");
                    SliceRules.Run(workspace, "mob_effect.json", Textures.TrimEnd('/'));
                }
                break;
            case TextureStage.Chests:
                if (workspace.FromProtocol <= 498 && workspace.ToProtocol >= 573) Chests(workspace);
                break;
            case TextureStage.Glint:
                if (workspace.FromProtocol <= 498 && workspace.ToProtocol >= 573)
                    Edit(workspace, "misc/enchanted_item_glint.png", 64, 64, image => image.ColorizeGrayscale(128, 64, 204), true);
                break;
            case TextureStage.ConduitParticles:
                if (workspace.FromProtocol <= 756 && workspace.ToProtocol >= 757) ParticleDefinitions(workspace);
                break;
            case TextureStage.Inventory:
                Edit(workspace, "gui/container/inventory.png", 256, 256, image => {
                    image.NewImage(256, 256);
                    image.SubImage(0, 0, 256, 256);
                    image.SubImage(0, 166, 16, 198, 0, 198);
                    image.SubImage(104, 166, 120, 198, 16, 198);
                });
                break;
            case TextureStage.CreativeTabs:
                if (workspace.FromProtocol <= 760 && workspace.ToProtocol >= 761) CreativeTabs(workspace);
                break;
            case TextureStage.EnchantPath:
                if (workspace.FromProtocol <= 761 && workspace.ToProtocol >= 762) EnchantPath(workspace);
                break;
            case TextureStage.WidgetSliders:
                if (workspace.FromProtocol <= 761 && workspace.ToProtocol >= 762) WidgetSliders(workspace);
                break;
            case TextureStage.Title:
                if (workspace.FromProtocol <= 762 && workspace.ToProtocol >= 763)
                    Edit(workspace, "gui/title/minecraft.png", 256, 256, image => {
                        image.NewImage(274, 64);
                        image.SubImage(0, 0, 156, 44);
                        image.SubImage(0, 45, 119, 90, 155, 0);
                    }, true, true);
                break;
            case TextureStage.GuiSprites:
                if (workspace.FromProtocol <= 763 && workspace.ToProtocol >= 764)
                    SliceRules.Run(workspace, "gui.json", Textures + "gui", metadata: true);
                break;
            case TextureStage.ImageFormats:
                if (workspace.FromProtocol <= 764 && workspace.ToProtocol >= 765) ImageFormats(workspace);
                break;
            case TextureStage.MapSprites:
                if (workspace.FromProtocol <= 765 && workspace.ToProtocol >= 766)
                    SliceRules.Run(workspace, "map_icons.json", Textures + "map");
                break;
            case TextureStage.Celestial:
                if (workspace.FromProtocol <= 773 && workspace.ToProtocol >= 774) Celestial(workspace);
                break;
            default: throw new ArgumentOutOfRangeException(nameof(stage), stage, "Unknown forward texture conversion stage.");
        }
    }

    private static void Edit(ConversionWorkspace workspace, string relative, int width, int height,
        Action<Raster> transform, bool powerOfTwo = false, bool square = false) {
        string path = Textures + relative;
        if (!workspace.Exists(path)) return;
        using var image = new Raster(width, height, workspace.PathFor(path));
        if ((powerOfTwo && !image.IsPowerOfTwo) || (square && !image.IsSquare)) return;
        transform(image);
        image.Store();
        workspace.RecordImage();
    }

    private static void Compass(ConversionWorkspace workspace) {
        string folder = Textures + (workspace.ToProtocol >= 393 ? "item" : "items");
        string path = folder + "/compass.png";
        if (!workspace.Exists(path)) return;
        using var image = new Raster(16, 512, workspace.PathFor(path));
        if (!image.IsPowerOfTwo) return;
        for (int frame = 0; frame < 32; frame++) {
            image.SaveSlice(0, frame * 16, 16, 16, workspace.PathFor(folder + $"/compass_{frame:00}.png"));
            workspace.RecordImage();
        }
        workspace.Delete(path + ".mcmeta");
    }

    private static void Offhand(ConversionWorkspace workspace) =>
        Edit(workspace, "gui/widgets.png", 256, 256, image => {
            image.FillEmpty(24, 22, 29, 24);
            image.FillEmpty(60, 22, 29, 24);
            image.SubImage(0, 0, 11, 22, 24, 23);
            image.SubImage(171, 0, 182, 22, 35, 23);
            image.SubImage(0, 0, 11, 22, 60, 23);
            image.SubImage(171, 0, 182, 22, 71, 23);
        });

    private static void LegacyMapIcons(ConversionWorkspace workspace) {
        string path = Textures + "map/map_icons.png";
        if (!workspace.Exists(path)) return;
        using var old = new MagickImage(workspace.PathFor(path));
        using var image = new MagickImage(RuleData.Bytes("forwards/map_icons.png"));
        double sx = old.Width / 32.0, sy = old.Height / 32.0;
        int width = (int)(128 * sx), height = (int)(128 * sy);
        if ((int)(8 * sx) <= 0 || (int)(8 * sy) <= 0)
            throw new InvalidDataException($"Legacy map atlas '{path}' is too small to slice.");
        // Unlike upstream's fixed 128px canvas, retain the pack's native pixel scale.
        image.FilterType = FilterType.Point;
        image.Resize(new MagickGeometry((uint)width, (uint)height) { IgnoreAspectRatio = true });
        for (int index = 0; index < 10; index++) {
            int x = index % 4 * 8, y = index / 4 * 8;
            using var part = old.Clone((int)(x * sx), (int)(y * sy), (uint)(8 * sx), (uint)(8 * sy));
            part.ResetPage();
            image.Composite(part, (int)(index * 8 * sx), 0, CompositeOperator.Over);
        }
        string temporary = workspace.PathFor(path + ".rpc-" + Guid.NewGuid().ToString("N") + ".png");
        try {
            image.Write(temporary, MagickFormat.Png);
            File.Move(temporary, workspace.PathFor(path), true);
        } finally { if (File.Exists(temporary)) File.Delete(temporary); }
        workspace.RecordImage();
    }

    private static void Water(ConversionWorkspace workspace) {
        Edit(workspace, "block/water_flow.png", 16, 1024, image => image.Grayscale(), true);
        Edit(workspace, "block/water_still.png", 16, 512, image => image.Grayscale(), true);
        Edit(workspace, "block/water_overlay.png", 16, 16, image => image.Grayscale(), true);
    }

    private static void Particles(ConversionWorkspace workspace) {
        string atlas = Textures + "particle/particles.png";
        if (!workspace.Exists(atlas)) return;
        if (workspace.ToProtocol >= 393 && workspace.FromProtocol <= 340) {
            Edit(workspace, "particle/particles.png", 128, 128, image => {
                image.NewImage(256, 256);
                image.SubImage(0, 0, 128, 128);
            });
        }
        if (workspace.MinimumSourceProtocol <= 404 && workspace.ToProtocol >= 477) {
            WarnLegacyAlias(workspace, 477, atlas);
            int size = workspace.ToProtocol >= 393 ? 256 : 128;
            SliceRules.Run(workspace, "particles.json", Textures + "particle", width: size, height: size);
            string hook = Textures + "particle/fishing_hook.png";
            if (workspace.Exists(hook)) workspace.Move(hook, Textures + "entity/fishing_hook.png", true);
        }
    }

    private static void WarnLegacyAlias(ConversionWorkspace workspace, int boundary, string path) {
        if (workspace.AmbiguousSourceAt(boundary) && workspace.Exists(path))
            workspace.Warn($"Source pack format aliases multiple game versions; converting explicitly present legacy atlas '{path}'.");
    }

    private static void ParticleDefinitions(ConversionWorkspace workspace) {
        const string root = "assets/minecraft/particles/";
        if (workspace.Exists(root + "barrier.json")) workspace.Move(root + "barrier.json", root + "block_marker.json");
        else if (workspace.Exists(root + "light.json")) workspace.Move(root + "light.json", root + "block_marker.json");
    }

    private static void CreativeTabs(ConversionWorkspace workspace) =>
        Edit(workspace, "gui/container/creative_inventory/tabs.png", 256, 256, image => {
            image.NewImage(256, 256);
            int[] originals = [0, 1, 2, 3, 4, 4, 5, 6];
            for (int index = 0; index < originals.Length; index++) {
                int start = originals[index] * 28;
                image.SubImage(start, 0, start + 14, 160, index * 26, 0);
                image.SubImage(start + 14, 0, start + 28, 160, index * 26 + 12, 0);
            }
            image.SubImage(232, 0, 256, 15, 232, 0);
        });

    private static void EnchantPath(ConversionWorkspace workspace) {
        foreach (string suffix in new[] { ".png", ".png.mcmeta" }) {
            string old = Textures + "misc/enchanted_item_glint" + suffix;
            if (!workspace.Exists(old)) continue;
            workspace.Copy(old, Textures + "misc/enchanted_glint_entity" + suffix);
            workspace.Copy(old, Textures + "misc/enchanted_glint_item" + suffix);
            workspace.Delete(old);
        }
    }

    private static void WidgetSliders(ConversionWorkspace workspace) {
        string source = Textures + "gui/widgets.png";
        if (!workspace.Exists(source)) return;
        using var image = new Raster(256, 256, workspace.PathFor(source));
        image.NewImage(256, 256);
        image.SubImageSized(0, 46, 200, 20, 0, 0);
        image.SubImageSized(0, 46, 200, 20, 0, 20);
        image.SubImageSized(0, 66, 200, 20, 0, 40);
        image.SubImageSized(0, 86, 200, 20, 0, 60);
        image.Store(workspace.PathFor(Textures + "gui/slider.png"));
        workspace.RecordImage();
    }

    private static void ImageFormats(ConversionWorkspace workspace) {
        string[] extensions = [".jpg", ".jpeg", ".raw", ".ico", ".bmp"];
        foreach (string file in workspace.Files(Textures.TrimEnd('/')).ToArray()) {
            if (!extensions.Contains(Path.GetExtension(file), StringComparer.Ordinal)) continue;
            string destination = file[..file.LastIndexOf('.')] + ".png";
            using var image = new MagickImage(workspace.PathFor(file));
            if (workspace.Exists(destination)) throw new IOException($"Image format conversion would overwrite '{destination}'.");
            workspace.EnsureParent(destination);
            image.Write(workspace.PathFor(destination), MagickFormat.Png);
            workspace.RecordImage();
            workspace.Delete(file);
        }
    }

    private static void Celestial(ConversionWorkspace workspace) {
        foreach (string name in new[] { "sun.png", "sun.png.mcmeta", "end_flash.png", "end_flash.png.mcmeta" }) {
            string old = Textures + "environment/" + name;
            if (workspace.Exists(old)) workspace.Move(old, Textures + "environment/celestial/" + name);
        }
        SliceRules.Run(workspace, "moon_phases.json", Textures + "environment", ignorePredicates: true);
    }

    private static void Chests(ConversionWorkspace workspace) {
        foreach (string name in new[] { "normal", "trapped", "christmas" }) DoubleChest(workspace, name);
        foreach (string name in new[] { "normal", "trapped", "christmas", "ender" })
            Edit(workspace, "entity/chest/" + name + ".png", 64, 64, image => {
                image.NewImage(64, 64);
                image.SubImage(14, 0, 28, 14, 28, 0, true);
                image.SubImage(28, 0, 42, 14, 14, 0, true);
                image.SubImage(28, 19, 42, 33, 14, 19, true);
                image.SubImage(14, 19, 28, 33, 28, 19, true);
                image.SubImage(28, 14, 42, 19, 28, 14, 1);
                image.SubImage(42, 14, 56, 19, 14, 14, 1);
                image.SubImage(14, 14, 28, 19, 42, 14, 1);
                image.SubImage(0, 14, 14, 19, 0, 14, 1);
                image.SubImage(0, 33, 14, 43, 0, 33, 1);
                image.SubImage(42, 33, 56, 43, 14, 33, 1);
                image.SubImage(28, 33, 42, 43, 28, 33, 1);
                image.SubImage(14, 33, 28, 43, 42, 33, 1);
                image.SubImage(0, 1, 1, 5, 0, 1, true);
                image.SubImage(3, 0, 5, 1, 1, 0);
                image.SubImage(1, 0, 3, 1, 3, 0);
                image.SubImage(1, 1, 3, 5, 4, 1, 1);
                image.SubImage(3, 1, 6, 5, 1, 1, 1);
            }, true);
    }

    private static void DoubleChest(ConversionWorkspace workspace, string name) {
        string root = Textures + "entity/chest/";
        string path = root + name + "_double.png";
        if (!workspace.Exists(path)) return;
        using var image = new Raster(128, 64, workspace.PathFor(path));
        if (!image.IsPowerOfTwo) return;
        image.NewImage(64, 64);
        image.SubImage(59, 19, 74, 33, 14, 19, true);
        image.SubImage(59, 0, 74, 14, 14, 0, true);
        image.SubImage(29, 19, 44, 33, 29, 19, true);
        image.SubImage(29, 14, 44, 19, 43, 14, 1);
        image.SubImage(29, 0, 44, 14, 29, 0, true);
        image.SubImage(44, 33, 58, 43, 29, 33, 1);
        image.SubImage(29, 33, 44, 43, 43, 33, 1);
        image.SubImage(58, 33, 73, 43, 14, 33, 1);
        image.SubImage(44, 14, 58, 19, 29, 14, 1);
        image.SubImage(58, 14, 73, 19, 14, 14, 1);
        image.SubImage(4, 0, 5, 1, 1, 0);
        image.SubImage(2, 0, 3, 1, 2, 0);
        image.SubImage(2, 1, 5, 5, 1, 1, 1);
        image.Store(workspace.PathFor(root + name + "_left.png"));
        workspace.RecordImage();
        image.NewImage(64, 64);
        image.SubImage(44, 0, 59, 14, 14, 0, true);
        image.SubImage(14, 0, 29, 14, 29, 0, true);
        image.SubImage(14, 19, 29, 33, 29, 19, true);
        image.SubImage(0, 14, 14, 19, 0, 14, 1);
        image.SubImage(73, 14, 88, 19, 14, 14, 1);
        image.SubImage(14, 14, 29, 19, 43, 14, 1);
        image.SubImage(44, 19, 59, 33, 14, 19, true);
        image.SubImage(14, 33, 29, 43, 43, 33, 1);
        image.SubImage(0, 33, 14, 43, 0, 33, 1);
        image.SubImage(73, 33, 88, 43, 14, 33, 1);
        image.SubImage(3, 0, 4, 1, 1, 0);
        image.SubImage(1, 0, 2, 1, 2, 0);
        image.SubImage(0, 1, 1, 5, 0, 1, true);
        image.SubImage(5, 1, 6, 5, 1, 1, true);
        image.SubImage(1, 1, 2, 5, 3, 1, true);
        image.Store(workspace.PathFor(root + name + "_right.png"));
        workspace.RecordImage();
        workspace.Delete(path);
    }
}
