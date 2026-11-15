// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class ContentJson
{
    internal const string Minecraft = "assets/minecraft/";
    internal static JsonObject Object(ConversionWorkspace w, string file) =>
        w.ReadJson(file) as JsonObject ?? throw new InvalidDataException($"Expected a JSON object in '{file}'.");
    internal static string? Text(JsonNode? node) => node is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
    internal static bool Vanilla(string resource) => !resource.Contains(':') || resource.StartsWith("minecraft:", StringComparison.Ordinal);
    internal static string Unqualify(string resource) => resource.StartsWith("minecraft:", StringComparison.Ordinal) ? resource[10..] : resource;
    internal static string Qualify(string resource) => resource.Contains(':') ? resource : "minecraft:" + resource;
    internal static string Clean(string value) => value.ToLowerInvariant().Replace("(", "").Replace(")", "");
    internal static string Map(bool forward, string table, string version, string value)
    {
        var node = RuleData.Json((forward ? "forwards/" : "backwards/") + table + ".json")[version] as JsonObject;
        return Text(node?[value]) ?? value;
    }
    internal static void SaveChanged(ConversionWorkspace w, string file, JsonNode before, JsonNode after)
    {
        if (!JsonNode.DeepEquals(before, after)) w.WriteJson(file, after);
    }
    internal static IEnumerable<(int Protocol, string Key)> NameVersions(bool forward)
    {
        (int, string)[] versions = [(393, "1_13"), (477, "1_14"), (755, "1_17"), (759, "1_19"), (765, "1_20_3")];
        return forward ? versions : versions.Reverse();
    }
    internal static bool Applies(ConversionWorkspace w, int boundary) => w.Crosses(boundary) ||
        (w.Forward && w.AmbiguousSourceAt(boundary) && w.ToProtocol >= boundary);
    internal static InvalidDataException Unsupported(string operation, IEnumerable<string> files) =>
        new($"ResourcePackConverter cannot {operation}. Affected files: {string.Join(", ", files)}");
}
