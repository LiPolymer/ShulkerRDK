// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class PostEffectRules
{
    private static string Target(string value) => value switch
    {
        "largeBlur" => "large_blur", "smallBlur" => "small_blur", "itemEntity" => "item_entity", _ => value
    };

    internal static void Run(ConversionWorkspace w)
    {
        if (!w.Crosses(768) || !w.Forward) return;
        const string legacy = ContentJson.Minecraft + "shaders/post";
        const string effects = ContentJson.Minecraft + "post_effect";
        if (!w.Exists(legacy)) return;
        var files = w.Files(legacy, "*.json", false).ToArray();
        var programs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in files)
        {
            var json = ContentJson.Object(w, file);
            if (json["targets"] is JsonArray oldTargets)
            {
                var targets = new JsonObject();
                foreach (var entry in oldTargets)
                {
                    string name;
                    JsonObject target;
                    if (ContentJson.Text(entry) is { } text) { name = Target(text); target = new JsonObject(); }
                    else if (entry is JsonObject obj && ContentJson.Text(obj["name"]) is { } named)
                    {
                        name = Target(named);
                        target = (JsonObject)obj.DeepClone();
                        target.Remove("name");
                    }
                    else throw new InvalidDataException($"Invalid post-effect target in '{file}'.");
                    if (targets.ContainsKey(name)) throw new InvalidDataException($"Duplicate post-effect target '{name}' in '{file}'.");
                    targets[name] = target;
                }
                json["targets"] = targets;
            }
            if (json["passes"] is JsonArray passes)
                foreach (var entry in passes)
                {
                    if (entry is not JsonObject pass) throw new InvalidDataException($"Invalid post-effect pass in '{file}'.");
                    if (ContentJson.Text(pass["name"]) is { } name)
                    {
                        pass.Remove("name");
                        var colon = name.IndexOf(':');
                        var program = colon < 0 ? "minecraft:post/" + name : name[..(colon + 1)] + "post/" + name[(colon + 1)..];
                        pass["program"] = program;
                        if (ContentJson.Vanilla(name)) programs.Add(ContentJson.Unqualify(name));
                    }
                    var inputs = pass["inputs"] as JsonArray ?? new JsonArray();
                    if (ContentJson.Text(pass["intarget"]) is { } input)
                    {
                        pass.Remove("intarget");
                        var node = new JsonObject { ["sampler_name"] = "In", ["target"] = ContentJson.Qualify(Target(input)) };
                        if (pass["use_linear_filter"] is { } filter) node["bilinear"] = filter.DeepClone();
                        inputs.Add(node);
                    }
                    pass.Remove("use_linear_filter");
                    if (pass["auxtargets"] is JsonArray auxiliary)
                    {
                        foreach (var auxiliaryEntry in auxiliary)
                        {
                            if (auxiliaryEntry is not JsonObject old) throw new InvalidDataException($"Invalid post-effect auxiliary target in '{file}'.");
                            var target = (JsonObject)old.DeepClone();
                            if (ContentJson.Text(target["name"]) is { } sampler) { target.Remove("name"); target["sampler_name"] = sampler; }
                            if (ContentJson.Text(target["id"]) is { } id)
                            {
                                target.Remove("id");
                                if (id.EndsWith(":depth", StringComparison.Ordinal)) { id = id[..^6]; target["use_depth_buffer"] = true; }
                                target["target"] = ContentJson.Qualify(Target(id));
                            }
                            inputs.Add(target);
                        }
                        pass.Remove("auxtargets");
                    }
                    if (inputs.Count > 0 && pass["inputs"] == null) pass["inputs"] = inputs;
                    if (ContentJson.Text(pass["outtarget"]) is { } output) { pass.Remove("outtarget"); pass["output"] = Target(output); }
                }
            var destination = effects + "/" + Path.GetFileName(file);
            if (w.Exists(destination) && !JsonNode.DeepEquals(w.ReadJson(destination), json))
                throw new InvalidDataException($"Post-effect conversion would overwrite '{destination}'.");
            w.WriteJson(destination, json);
            w.Delete(file);
            programs.Add(Path.GetFileNameWithoutExtension(file));
        }
        // Preserve all non-JSON data as well; directory merges reject conflicting sources.
        if (w.Files(legacy).Any()) w.Move(legacy, effects);
        else w.Delete(legacy);
        foreach (var program in programs)
            NameRules.MoveIfPresent(w, ContentJson.Minecraft + "shaders/program/" + program + ".json",
                legacy + "/" + program + ".json");
    }
}
