// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class ContentRules
{
    public static void Run(ConversionWorkspace workspace, Action<TextureStage> textures)
    {
        var w = workspace;
        if (w.FromProtocol == w.ToProtocol) return;
        if (!w.Forward) RejectUnsupportedBackwards(w);
        NameRules.Run(w);
        if (w.Forward)
        {
            textures(TextureStage.Compass);
            textures(TextureStage.Offhand);
            NameRules.RemoveSpaces(w);
            ModelRules.Run(w);
            Animation(w);
            textures(TextureStage.MapIcons);
            PropertyRules.Run(w);
            textures(TextureStage.Water);
            ModelRules.BlockStates(w);
            LanguageRules.Run(w);
            textures(TextureStage.Particles);
            textures(TextureStage.Paintings);
            textures(TextureStage.MobEffects);
            textures(TextureStage.Chests);
            textures(TextureStage.Glint);
            textures(TextureStage.ConduitParticles);
            textures(TextureStage.Inventory);
            Atlas(w);
            textures(TextureStage.CreativeTabs);
            textures(TextureStage.EnchantPath);
            textures(TextureStage.WidgetSliders);
            textures(TextureStage.Title);
            textures(TextureStage.GuiSprites);
            textures(TextureStage.ImageFormats);
            textures(TextureStage.MapSprites);
            Armor(w);
            PostEffectRules.Run(w);
            textures(TextureStage.Celestial);
        }
        else
        {
            DeleteNewResources(w);
            textures(TextureStage.EnchantPath);
            textures(TextureStage.ConduitParticles);
            textures(TextureStage.Inventory);
            textures(TextureStage.Glint);
            textures(TextureStage.Chests);
            textures(TextureStage.Paintings);
            textures(TextureStage.Particles);
            LanguageRules.Run(w);
            textures(TextureStage.MapIcons);
            PropertyRules.Run(w);
            textures(TextureStage.Water);
            ModelRules.BlockStates(w);
            ModelRules.Run(w);
            textures(TextureStage.Compass);
        }
    }

    private static void RejectUnsupportedBackwards(ConversionWorkspace w)
    {
        if (w.Crosses(763))
        {
            var titles = w.Files(ContentJson.Minecraft + "textures/gui/title", "minecraft.png", false).ToArray();
            if (titles.Length > 0) throw ContentJson.Unsupported("reconstruct the legacy title atlas from the Minecraft 1.20 title texture", titles);
        }
        (int Boundary, string Directory, string Description)[] unsupported =
        [
            (764, "textures/gui/sprites", "reconstruct legacy GUI atlases from 1.20.2 GUI sprites"),
            (766, "textures/map/decorations", "reconstruct the map icon atlas from 1.20.5 map sprites"),
            (768, "textures/entity/equipment", "downgrade the 1.21.2 equipment/armor texture layout"),
            (768, "equipment", "downgrade 1.21.2 equipment definitions"),
            (768, "post_effect", "downgrade 1.21.2 post-effect shader definitions"),
            (774, "textures/environment/celestial", "reconstruct the pre-1.21.11 celestial atlases")
        ];
        foreach (var (boundary, directory, description) in unsupported)
        {
            if (!w.Crosses(boundary)) continue;
            var files = w.Files(ContentJson.Minecraft + directory).ToArray();
            if (files.Length > 0) throw ContentJson.Unsupported(description, files);
        }
    }

    private static void Animation(ConversionWorkspace w)
    {
        if (!w.Crosses(393)) return;
        foreach (var kind in new[] { "block", "item" })
            foreach (var file in w.Files(ContentJson.Minecraft + "textures/" + kind, "*.png.mcmeta", false))
            {
                var json = ContentJson.Object(w, file);
                var before = json.DeepClone();
                if (json["animation"] is JsonObject animation)
                {
                    animation.Remove("width");
                    animation.Remove("height");
                }
                ContentJson.SaveChanged(w, file, before, json);
            }
    }

    private static void Atlas(ConversionWorkspace w)
    {
        const string output = ContentJson.Minecraft + "atlases/blocks.json";
        if (!w.Crosses(761) || w.Exists(output)) return;
        const string textures = ContentJson.Minecraft + "textures";
        if (!Directory.Exists(w.PathFor(textures))) return;
        var sources = new JsonArray();
        AddDirectories(textures, "");
        foreach (var file in w.Files(textures, "*.png", false))
            sources.Add(new JsonObject { ["type"] = "single", ["resource"] = Path.GetFileNameWithoutExtension(file) });
        w.WriteJson(output, new JsonObject { ["sources"] = sources });
        void AddDirectories(string directory, string prefix)
        {
            foreach (var full in Directory.GetDirectories(w.PathFor(directory)).Order(StringComparer.Ordinal))
            {
                var name = Path.GetFileName(full);
                var source = prefix + name;
                sources.Add(new JsonObject { ["type"] = "directory", ["source"] = source, ["prefix"] = source + "/" });
                AddDirectories(directory + "/" + name, source + "/");
            }
        }
    }

    private static void Armor(ConversionWorkspace w)
    {
        if (!w.Crosses(768)) return;
        const string old = ContentJson.Minecraft + "textures/models/armor/";
        const string equipment = ContentJson.Minecraft + "textures/entity/equipment/";
        foreach (var material in new[] { "chainmail", "diamond", "gold", "iron", "leather", "netherite" })
            for (var layer = 1; layer <= 2; layer++)
                foreach (var suffix in new[] { ".png", ".png.mcmeta" })
                {
                    var directory = layer == 1 ? "humanoid/" : "humanoid_leggings/";
                    NameRules.MoveIfPresent(w, old + material + "_layer_" + layer + suffix, equipment + directory + material + suffix);
                    if (material == "leather")
                        NameRules.MoveIfPresent(w, old + material + "_layer_" + layer + "_overlay" + suffix,
                            equipment + directory + "leather_overlay" + suffix);
                }
        foreach (var suffix in new[] { ".png", ".png.mcmeta" })
            NameRules.MoveIfPresent(w, old + "turtle_layer_1" + suffix, equipment + "humanoid/turtle_scute" + suffix);
    }

    private static void DeleteNewResources(ConversionWorkspace w)
    {
        var protocols = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["1.8"] = 47, ["1.9"] = 107, ["1.10"] = 210, ["1.11"] = 315, ["1.12"] = 335,
            ["1.13"] = 393, ["1.14"] = 477, ["1.15"] = 573, ["1.16"] = 735, ["1.17"] = 755,
            ["1.18"] = 757, ["1.19"] = 759
        };
        foreach (var kind in new[] { "blocks", "items", "entities" })
            foreach (var version in RuleData.Json("backwards/delete/" + kind + ".json").AsObject())
            {
                if (!protocols.TryGetValue(version.Key, out var protocol) || !w.Crosses(protocol)) continue;
                foreach (var entry in version.Value!.AsArray())
                {
                    var name = entry!.GetValue<string>();
                    // A successful name conversion salvages a resource for the old layout;
                    // deletion tables intentionally refer only to its unsalvaged new name.
                    if (kind == "entities")
                    {
                        foreach (var suffix in new[] { ".png", ".png.mcmeta" }) w.Delete(ContentJson.Minecraft + "textures/entity/" + name + suffix);
                    }
                    else
                    {
                        var type = kind == "blocks" ? "block" : "item";
                        w.Delete(ContentJson.Minecraft + "models/" + type + "/" + name + ".json");
                        foreach (var folder in new[] { type, type + "s" })
                            foreach (var suffix in new[] { ".png", ".png.mcmeta" })
                                w.Delete(ContentJson.Minecraft + "textures/" + folder + "/" + name + suffix);
                    }
                }
            }
        if (w.Crosses(761)) w.Delete(ContentJson.Minecraft + "atlases");
    }
}
