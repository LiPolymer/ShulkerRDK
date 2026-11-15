// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Globalization;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class PropertyRules
{
    internal static void Run(ConversionWorkspace w)
    {
        if (!w.Crosses(393)) return;
        var directory = ContentJson.Minecraft + (w.Forward ? "optifine" : "mcpatcher");
        var ids = RuleData.Json((w.Forward ? "forwards/" : "backwards/") + "ids.json")["regular"]!.AsObject();
        foreach (var file in w.Files(directory, "*.properties"))
        {
            var properties = LegacyProperties.Read(w, file);
            var before = new Dictionary<string, string>(properties, StringComparer.Ordinal);
            if (properties.TryGetValue("texture", out var texture) && texture.StartsWith("textures/", StringComparison.Ordinal))
                properties["texture"] = "textures/" + ModelRules.Resource(w, texture[9..], true, file);
            var metadata = properties.TryGetValue("metadata", out var meta) ? Tokens(meta).ToArray() : [];
            var usedMetadata = false;
            var allMetadataResolved = true;
            foreach (var key in new[] { "matchItems", "items", "matchBlocks" })
            {
                if (!properties.TryGetValue(key, out var value)) continue;
                var remapped = new List<string>();
                foreach (var token in Tokens(value))
                {
                    if (w.Forward && metadata.Length > 0 && int.TryParse(token, NumberStyles.None, CultureInfo.InvariantCulture, out _))
                    {
                        var converted = new List<string>();
                        foreach (var data in metadata)
                        {
                            var combined = data == "0" && ids[token + ":0"] == null ? token : token + ":" + data;
                            if (ContentJson.Text(ids[combined]) is { } mapped) converted.Add(ContentJson.Qualify(mapped));
                            else throw new InvalidDataException($"No upstream mapping for legacy ID '{combined}' with metadata in '{file}'; conversion cannot preserve this selector.");
                        }
                        if (converted.Count > 0) { remapped.AddRange(converted); usedMetadata = true; continue; }
                    }
                    var lookup = w.Forward ? token : ContentJson.Qualify(token);
                    var mappedId = ContentJson.Text(ids[lookup]);
                    remapped.Add(mappedId == null ? token : w.Forward ? ContentJson.Qualify(mappedId) : mappedId);
                    if (metadata.Length > 0 && mappedId == null) allMetadataResolved = false;
                }
                properties[key] = string.Join(" ", remapped.Distinct(StringComparer.Ordinal));
            }
            if (usedMetadata && !allMetadataResolved)
                throw new InvalidDataException($"Mixed unmapped IDs and metadata in '{file}' cannot be flattened without changing the selector.");
            if (usedMetadata) properties.Remove("metadata");
            if (before.Count != properties.Count || before.Any(x => !properties.TryGetValue(x.Key, out var value) || value != x.Value))
                LegacyProperties.Write(w, file, properties);
        }
    }

    private static IEnumerable<string> Tokens(string text)
    {
        foreach (var token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            var dash = token.IndexOf('-');
            if (dash > 0 && int.TryParse(token.AsSpan(0, dash), NumberStyles.None, CultureInfo.InvariantCulture, out var start) &&
                int.TryParse(token.AsSpan(dash + 1), NumberStyles.None, CultureInfo.InvariantCulture, out var end) && end >= start && end - start <= 65535)
            {
                for (var i = start; i <= end; i++) yield return i.ToString(CultureInfo.InvariantCulture);
            }
            else yield return token;
        }
    }
}
