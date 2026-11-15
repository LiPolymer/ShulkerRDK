// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class LanguageRules
{
    internal static void Run(ConversionWorkspace w)
    {
        const string directory = ContentJson.Minecraft + "lang";
        if (w.Forward && w.ToProtocol >= 393)
        {
            foreach (var file in w.Files(directory, "*.lang", false).ToArray())
            {
                var entries = LegacyProperties.Read(w, file);
                entries = Remap(entries, ForwardMap("1_13"), file);
                if (w.ToProtocol >= 477) entries = Remap(entries, ForwardMap("1_14"), file);
                var output = file[..^5].ToLowerInvariant() + ".json";
                var json = new JsonObject();
                foreach (var entry in entries) json[entry.Key] = entry.Value;
                if (w.Exists(output) && !JsonNode.DeepEquals(w.ReadJson(output), json))
                    throw new InvalidDataException($"Converting '{file}' would overwrite conflicting language file '{output}'.");
                w.WriteJson(output, json);
                w.Delete(file);
            }
            if (ContentJson.Applies(w, 477))
                foreach (var file in w.Files(directory, "*.json", false))
                {
                    var original = ContentJson.Object(w, file);
                    var entries = Values(original, file);
                    entries = Remap(entries, ForwardMap("1_14"), file);
                    var updated = new JsonObject();
                    foreach (var entry in entries) updated[entry.Key] = entry.Value;
                    ContentJson.SaveChanged(w, file, original, updated);
                }
        }
        else if (!w.Forward)
        {
            foreach (var file in w.Files(directory, "*.json", false).ToArray())
            {
                var original = ContentJson.Object(w, file);
                var entries = Values(original, file);
                if (w.Crosses(477)) entries = Remap(entries, BackwardMap("1_14"), file);
                if (w.Crosses(393))
                {
                    entries = Remap(entries, BackwardMap("1_13"), file);
                    var output = file[..^5] + ".lang";
                    if (w.Exists(output))
                    {
                        var existing = LegacyProperties.Read(w, output);
                        if (existing.Count != entries.Count || existing.Any(x => !entries.TryGetValue(x.Key, out var value) || value != x.Value))
                            throw new InvalidDataException($"Converting '{file}' would overwrite conflicting language file '{output}'.");
                    }
                    LegacyProperties.Write(w, output, entries);
                    w.Delete(file);
                }
                else
                {
                    var updated = new JsonObject();
                    foreach (var entry in entries) updated[entry.Key] = entry.Value;
                    ContentJson.SaveChanged(w, file, original, updated);
                }
            }
        }
    }

    private static Dictionary<string, string> Values(JsonObject json, string file)
    {
        var entries = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in json)
            entries[entry.Key] = ContentJson.Text(entry.Value) ?? throw new InvalidDataException($"Language value '{entry.Key}' in '{file}' is not a string.");
        return entries;
    }

    private static Dictionary<string, string> ForwardMap(string version) =>
        RuleData.Json("forwards/lang.json")[version]!.AsObject()
            .ToDictionary(x => x.Key, x => x.Value!.GetValue<string>(), StringComparer.Ordinal);

    private static Dictionary<string, string> BackwardMap(string version)
    {
        // Upstream's backwards 1_13 resource is accidentally forward-oriented; consume its
        // full table in reverse instead of reproducing the empty/deleted-language bug.
        var table = RuleData.Json("backwards/lang.json")[version]!.AsObject();
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in table)
        {
            var key = version == "1_13" ? entry.Value!.GetValue<string>() : entry.Key;
            var value = version == "1_13" ? entry.Key : entry.Value!.GetValue<string>();
            result.TryAdd(key, value);
        }
        return result;
    }

    private static Dictionary<string, string> Remap(Dictionary<string, string> entries, Dictionary<string, string> map, string file)
    {
        var output = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            var key = map.GetValueOrDefault(entry.Key, entry.Key);
            if (output.TryGetValue(key, out var value) && value != entry.Value)
                throw new InvalidDataException($"Language keys in '{file}' both map to '{key}' with conflicting values.");
            output[key] = entry.Value;
        }
        return output;
    }
}
