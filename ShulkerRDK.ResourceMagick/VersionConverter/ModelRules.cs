// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class ModelRules
{
    private const string Models = ContentJson.Minecraft + "models/";

    internal static void Run(ConversionWorkspace w)
    {
        if (!w.Forward && w.Crosses(769)) BackwardItems(w);
        var originals = w.Files(Models, "*.json").ToDictionary(file => file, file => ContentJson.Object(w, file), StringComparer.Ordinal);
        foreach (var (file, original) in originals)
        {
            var model = (JsonObject)original.DeepClone();
            if (!w.Forward && w.Crosses(107)) model = MergeParent(w, model, originals, new HashSet<string>(StringComparer.Ordinal) { file });
            if (ContentJson.Text(model["parent"]) is { } parent) model["parent"] = Resource(w, parent, false, file);
            if (!w.Forward && w.ToProtocol < 578) model.Remove("gui_light");
            if (model["textures"] is JsonObject textures)
                foreach (var key in textures.Select(x => x.Key).ToArray())
                    if (ContentJson.Text(textures[key]) is { } value)
                    {
                        if (w.Forward && w.Crosses(107) && key == "layer0" && !value.Contains('/') && !value.Contains(':') && !value.StartsWith('#'))
                            value = (file.StartsWith(Models + "block/", StringComparison.Ordinal) ? "block/" : "item/") + value;
                        textures[key] = Resource(w, value, true, file);
                    }
            if (model["display"] is JsonObject display && w.Crosses(107)) model["display"] = Display(w.Forward, display);
            if (model["overrides"] is JsonArray overrides)
                foreach (var entry in overrides.OfType<JsonObject>())
                    if (ContentJson.Text(entry["model"]) is { } reference)
                        entry["model"] = Resource(w, reference, false, file);
            if (w.Forward && w.Crosses(769) && file.StartsWith(Models + "item/", StringComparison.Ordinal) && model["overrides"] is JsonArray oldOverrides)
                ForwardItems(w, file, model, oldOverrides);
            ContentJson.SaveChanged(w, file, original, model);
        }
    }

    internal static string Resource(ConversionWorkspace w, string resource, bool texture, string file)
    {
        if (resource.StartsWith('#') || !ContentJson.Vanilla(resource)) return resource;
        var qualified = resource.StartsWith("minecraft:", StringComparison.Ordinal);
        var value = ContentJson.Unqualify(resource);
        if (w.Crosses(393) || (w.Forward && w.Crosses(315))) value = ContentJson.Clean(value).Replace(' ', '_');
        if (w.Forward && w.Crosses(393) && texture)
        {
            if (value.StartsWith("blocks/", StringComparison.Ordinal)) value = "block/" + value[7..];
            else if (value.StartsWith("items/", StringComparison.Ordinal)) value = "item/" + value[6..];
        }
        foreach (var (boundary, version) in ContentJson.NameVersions(w.Forward))
        {
            if (!ContentJson.Applies(w, boundary)) continue;
            if (value.StartsWith("block/", StringComparison.Ordinal))
            {
                var name = value[6..];
                // Snow texture paths did not take part in the model-only snow rename.
                if (!w.Forward && texture && boundary == 393 && name == "snow") continue;
                if (boundary == 393 && !texture)
                {
                    if (w.Forward && name == "snow_layer") { value = "block/snow"; continue; }
                    if (w.Forward && name == "snow") { value = "block/snow_block"; continue; }
                    if (!w.Forward && name == "snow_block") { value = "block/snow"; continue; }
                }
                value = "block/" + NameRules.MapName(w, "blocks", version, name, file);
            }
            else if (value.StartsWith("item/", StringComparison.Ordinal))
                value = "item/" + NameRules.MapName(w, "items", version, value[5..], file);
        }
        if (!w.Forward && w.Crosses(393) && texture)
        {
            if (value.StartsWith("block/", StringComparison.Ordinal)) value = "blocks/" + value[6..];
            else if (value.StartsWith("item/", StringComparison.Ordinal)) value = "items/" + value[5..];
        }
        if (w.Crosses(761)) qualified = w.Forward;
        return qualified ? "minecraft:" + value : value;
    }

    internal static void BlockStates(ConversionWorkspace w)
    {
        foreach (var file in w.Files(ContentJson.Minecraft + "blockstates", "*.json", false).ToArray())
        {
            var json = ContentJson.Object(w, file);
            var before = json.DeepClone();
            if (!w.Forward && w.ToProtocol < 107 && json["multipart"] is JsonArray)
                throw ContentJson.Unsupported("represent multipart blockstates before Minecraft 1.9", [file]);
            if (w.Crosses(393) && json["variants"] is JsonObject variants)
            {
                var source = w.Forward ? "normal" : "";
                var destination = w.Forward ? "" : "normal";
                if (variants[source] is JsonObject or JsonArray)
                {
                    if (variants.ContainsKey(destination))
                        throw new InvalidDataException($"Blockstate '{file}' contains both '{source}' and '{destination}' variants.");
                    var value = variants[source];
                    variants.Remove(source);
                    variants[destination] = value;
                }
            }
            if (json["variants"] is JsonObject states)
                foreach (var state in states) UpdateBlockModel(state.Value);
            if (json["multipart"] is JsonArray multipart)
                foreach (var part in multipart.OfType<JsonObject>()) UpdateBlockModel(part["apply"]);
            ContentJson.SaveChanged(w, file, before, json);
            void UpdateBlockModel(JsonNode? node)
            {
                if (node is JsonArray array)
                {
                    foreach (var entry in array) UpdateBlockModel(entry);
                    return;
                }
                if (node is not JsonObject obj || ContentJson.Text(obj["model"]) is not { } reference || !ContentJson.Vanilla(reference)) return;
                var value = ContentJson.Unqualify(reference);
                var slash = value.LastIndexOf('/');
                var prefix = slash < 0 ? "" : value[..(slash + 1)];
                var name = slash < 0 ? value : value[(slash + 1)..];
                if (w.Forward && w.Crosses(107) && !prefix.StartsWith("block/", StringComparison.Ordinal)) prefix = "block/" + prefix;
                foreach (var (boundary, version) in ContentJson.NameVersions(w.Forward))
                    if (ContentJson.Applies(w, boundary))
                    {
                        if (boundary == 393 && w.Forward)
                        {
                            name = name switch { "snow_layer" => "snow", "snow" => "snow_block", _ => NameRules.MapName(w, "blocks", version, name, file) };
                            prefix = prefix.Replace("blocks/", "block/", StringComparison.Ordinal);
                        }
                        else if (boundary == 393 && !w.Forward && name == "snow_block") name = "snow";
                        else name = NameRules.MapName(w, "blocks", version, name, file);
                    }
                if (!w.Forward && (w.Crosses(393) || w.ToProtocol < 107)) prefix = "";
                var qualified = reference.StartsWith("minecraft:", StringComparison.Ordinal);
                if (w.Crosses(761)) qualified = w.Forward;
                obj["model"] = (qualified ? "minecraft:" : "") + prefix + name;
            }
        }
    }

    private static JsonObject MergeParent(ConversionWorkspace w, JsonObject current, Dictionary<string, JsonObject> originals, HashSet<string> chain)
    {
        var reference = ContentJson.Text(current["parent"]);
        if (reference == null || !ContentJson.Vanilla(reference)) return current;
        var target = ContentJson.Unqualify(Resource(w, reference, false, chain.First()));
        var path = Models + target + ".json";
        if (!originals.TryGetValue(path, out var parent)) return current;
        if (!chain.Add(path)) throw new InvalidDataException($"Cyclic model parent chain: {string.Join(" -> ", chain)} -> {path}.");
        var merged = MergeParent(w, (JsonObject)parent.DeepClone(), originals, chain);
        chain.Remove(path);
        current.Remove("parent");
        foreach (var pair in current)
        {
            if (pair.Key == "textures" && pair.Value is JsonObject childTextures && merged["textures"] is JsonObject parentTextures)
            {
                foreach (var texture in childTextures) parentTextures[texture.Key] = texture.Value?.DeepClone();
            }
            else merged[pair.Key] = pair.Value?.DeepClone();
        }
        if (merged["textures"] is JsonObject textures)
            foreach (var key in textures.Select(x => x.Key).ToArray())
            {
                var visited = new HashSet<string>(StringComparer.Ordinal) { key };
                var value = ContentJson.Text(textures[key]);
                while (value?.StartsWith('#') == true && textures.ContainsKey(value[1..]))
                {
                    if (!visited.Add(value[1..])) throw new InvalidDataException($"Cyclic texture reference in '{path}': '{value}'.");
                    value = ContentJson.Text(textures[value[1..]]);
                }
                if (value != null) textures[key] = value;
            }
        return merged;
    }

    private static JsonObject Display(bool forward, JsonObject old)
    {
        var display = (JsonObject)old.DeepClone();
        var defaults = RuleData.Json((forward ? "forwards/" : "backwards/") + "display.json").AsObject();
        foreach (var kind in new[] { "firstperson", "thirdperson" })
        {
            if (forward)
            {
                var right = kind + "_righthand";
                var left = kind + "_lefthand";
                if (display[kind] is JsonObject original)
                {
                    display[right] = TransformDisplay(original, kind == "firstperson", true);
                    display.Remove(kind);
                }
                else if (display[right] == null) display[right] = defaults[right]!.DeepClone();
                if (display[left] == null) display[left] = Mirror(display[right]!.AsObject());
            }
            else
            {
                var original = display[kind + "_righthand"] as JsonObject;
                if (original == null && display[kind + "_lefthand"] is JsonObject left) original = Mirror(left);
                display.Remove(kind + "_righthand");
                display.Remove(kind + "_lefthand");
                display[kind] = original == null ? defaults[kind]!.DeepClone() : TransformDisplay(original, kind == "firstperson", false);
            }
        }
        foreach (var key in new[] { "ground", "head" })
            if (!forward) display.Remove(key);
            else if (display[key] == null) display[key] = defaults[key]!.DeepClone();
        return display;
    }

    private static JsonObject Mirror(JsonObject source)
    {
        var result = (JsonObject)source.DeepClone();
        Vector(result, "rotation", a => [a[0], -a[1], -a[2]]);
        return result;
    }

    private static JsonObject TransformDisplay(JsonObject source, bool first, bool forward)
    {
        var result = (JsonObject)source.DeepClone();
        if (first)
        {
            Vector(result, "rotation", a => [a[0], a[1] + (forward ? 45 : -45), a[2]]);
            Vector(result, "translation", a => forward
                ? [(a[0]) * .4 + 1.13, (a[1] - 4) * .4 + 3.2, (a[2] - 2) * .4 + 1.13]
                : [(a[0] - 1.13) / .4, (a[1] - 3.2) / .4 + 4, (a[2] - 1.13) / .4 + 2]);
            Vector(result, "scale", a => a.Select(x => forward ? x * .4 : x / .4).ToArray());
        }
        else
        {
            Vector(result, "rotation", a => forward ? [a[0], -a[1], -a[2] + 20] : [a[0], -a[1], -(a[2] - 20)]);
            Vector(result, "translation", a => forward ? [a[0], a[1] + 2.75, -a[2] - 3] : [a[0], a[1] - 2.75, -(a[2] + 3)]);
        }
        return result;
    }

    private static void Vector(JsonObject obj, string key, Func<double[], double[]> transform)
    {
        if (obj[key] is not JsonArray array) return;
        if (array.Count != 3) throw new InvalidDataException($"Display '{key}' must contain three numbers.");
        var values = transform(array.Select(x => x!.GetValue<double>()).ToArray());
        obj[key] = new JsonArray(values.Select(x => (JsonNode?)JsonValue.Create(x)).ToArray());
    }

    private static JsonObject ModelReference(string name) => new() { ["type"] = "minecraft:model", ["model"] = ContentJson.Qualify(name) };

    private static void ForwardItems(ConversionWorkspace w, string file, JsonObject model, JsonArray overrides)
    {
        var relative = file[Models.Length..^5];
        JsonObject dispatch = CompactDispatch(overrides, relative) ?? ModelReference(relative);
        if (ContentJson.Unqualify(ContentJson.Text(dispatch["type"]) ?? "") != "range_dispatch")
        foreach (var entry in overrides)
        {
            if (entry is not JsonObject item || item["predicate"] is not JsonObject predicate || ContentJson.Text(item["model"]) is not { } name)
                throw new InvalidDataException($"Invalid item override in '{file}'.");
            if (predicate.Any(x => x.Key is not ("damage" or "custom_model_data")))
                throw ContentJson.Unsupported("translate item predicates other than damage/custom_model_data to Minecraft 1.21.4", [file]);
            JsonObject match = ModelReference(name);
            foreach (var property in predicate.Reverse())
            {
                if (property.Value is not JsonValue number || !number.TryGetValue<double>(out var threshold) || !double.IsFinite(threshold))
                    throw new InvalidDataException($"Non-numeric item predicate '{property.Key}' in '{file}'.");
                match = new JsonObject
                {
                    ["type"] = "minecraft:range_dispatch", ["property"] = "minecraft:" + property.Key,
                    ["fallback"] = dispatch.DeepClone(),
                    ["entries"] = new JsonArray(new JsonObject { ["threshold"] = threshold, ["model"] = match })
                };
            }
            dispatch = match;
        }
        var output = ContentJson.Minecraft + "items/" + relative[5..] + ".json";
        var definition = new JsonObject { ["model"] = dispatch };
        if (w.Exists(output) && !JsonNode.DeepEquals(w.ReadJson(output), definition))
            throw new InvalidDataException($"Item conversion would overwrite existing definition '{output}'.");
        w.WriteJson(output, definition);
        model.Remove("overrides");
    }

    private static JsonObject? CompactDispatch(JsonArray overrides, string fallback)
    {
        string? property = null;
        var ranges = new List<(double Threshold, int Order, string Model)>(overrides.Count);
        for (var i = 0; i < overrides.Count; i++)
        {
            if (overrides[i] is not JsonObject item || item["predicate"] is not JsonObject predicate || predicate.Count != 1 ||
                ContentJson.Text(item["model"]) is not { } name) return null;
            var pair = predicate.First();
            if (pair.Key is not ("damage" or "custom_model_data") || (property != null && property != pair.Key) ||
                pair.Value is not JsonValue number || !number.TryGetValue<double>(out var threshold) || !double.IsFinite(threshold)) return null;
            property = pair.Key;
            ranges.Add((threshold, i, name));
        }
        if (ranges.Count == 0) return null;
        ranges.Sort((a, b) => a.Threshold.CompareTo(b.Threshold));
        var entries = new JsonArray();
        var selected = -1;
        for (var i = 0; i < ranges.Count;)
        {
            var threshold = ranges[i].Threshold;
            var best = ranges[i];
            do
            {
                if (ranges[i].Order > best.Order) best = ranges[i];
                i++;
            } while (i < ranges.Count && ranges[i].Threshold == threshold);
            // Legacy overrides are a last-match-wins decision list, even when thresholds
            // are unsorted. Discard unreachable entries without changing that precedence.
            if (best.Order <= selected) continue;
            selected = best.Order;
            entries.Add(new JsonObject { ["threshold"] = threshold, ["model"] = ModelReference(best.Model) });
        }
        return new JsonObject
        {
            ["type"] = "minecraft:range_dispatch", ["property"] = "minecraft:" + property,
            ["fallback"] = ModelReference(fallback), ["entries"] = entries
        };
    }

    private static void BackwardItems(ConversionWorkspace w)
    {
        foreach (var file in w.Files(ContentJson.Minecraft + "items", "*.json").ToArray())
        {
            var definition = ContentJson.Object(w, file);
            if (definition.Any(x => x.Key != "model") || definition["model"] is not JsonObject node)
                throw ContentJson.Unsupported("downgrade this Minecraft 1.21.4 item definition", [file]);
            var overrides = new JsonArray();
            var fallback = Flatten(node);
            var itemPath = "item/" + file[(ContentJson.Minecraft + "items/").Length..^5];
            var output = Models + ContentJson.Unqualify(Resource(w, itemPath, false, file)) + ".json";
            var fallbackPath = ContentJson.Vanilla(fallback) ? Models + ContentJson.Unqualify(Resource(w, fallback, false, file)) + ".json" : null;
            if (output == fallbackPath && !w.Exists(output))
            {
                if (overrides.Count > 0)
                    throw ContentJson.Unsupported("downgrade an item range dispatch whose self-referenced base model is absent from the pack", [file]);
                w.Delete(file);
                continue;
            }
            JsonObject model;
            if (output == fallbackPath && w.Exists(output)) model = ContentJson.Object(w, output);
            else model = new JsonObject { ["parent"] = fallback };
            if (overrides.Count > 0) model["overrides"] = overrides;
            if (w.Exists(output) && output != fallbackPath && !JsonNode.DeepEquals(w.ReadJson(output), model))
                throw new InvalidDataException($"Item conversion would overwrite model '{output}'.");
            w.WriteJson(output, model);
            w.Delete(file);

            string Flatten(JsonObject current)
            {
                var type = ContentJson.Unqualify(ContentJson.Text(current["type"]) ?? "");
                if (type == "model" && current.All(x => x.Key is "type" or "model") && ContentJson.Text(current["model"]) is { } modelName) return modelName;
                var property = ContentJson.Unqualify(ContentJson.Text(current["property"]) ?? "");
                if (type != "range_dispatch" || property is not ("damage" or "custom_model_data") ||
                    current.Any(x => x.Key is not ("type" or "property" or "fallback" or "entries")) ||
                    current["fallback"] is not JsonObject defaultModel || current["entries"] is not JsonArray entries)
                    throw ContentJson.Unsupported("downgrade item definitions beyond damage/custom_model_data range dispatches", [file]);
                var fallbackName = Flatten(defaultModel);
                double previous = double.NegativeInfinity;
                foreach (var entry in entries)
                {
                    if (entry is not JsonObject range || range["threshold"] is not JsonValue thresholdNode ||
                        !thresholdNode.TryGetValue<double>(out var threshold) || !double.IsFinite(threshold) || threshold < previous || range["model"] is not JsonObject match ||
                        ContentJson.Unqualify(ContentJson.Text(match["type"]) ?? "") != "model" ||
                        match.Any(x => x.Key is not ("type" or "model")) || ContentJson.Text(match["model"]) is not { } name)
                        throw ContentJson.Unsupported("downgrade complex or unsorted item range dispatch entries", [file]);
                    previous = threshold;
                    overrides.Add(new JsonObject { ["predicate"] = new JsonObject { [property] = threshold }, ["model"] = name });
                }
                return fallbackName;
            }
        }
    }
}
