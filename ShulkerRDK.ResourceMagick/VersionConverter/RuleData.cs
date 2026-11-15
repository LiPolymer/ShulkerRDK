using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

static class RuleData {
    static readonly ConcurrentDictionary<string, JsonNode> JSON = new(StringComparer.Ordinal);
    static readonly ConcurrentDictionary<string, byte[]> BINARY = new(StringComparer.Ordinal);
    static readonly Assembly ASSEMBLY = typeof(RuleData).Assembly;
    const string PREFIX = "ShulkerRDK.ResourceMagick.VersionConverter.Rules.";

    // Shared mapping nodes are read-only. Clone a node before placing it in generated JSON.
    public static JsonNode Json(string relative) => JSON.GetOrAdd(relative, static path => {
        using Stream stream = Open(path);
        return JsonNode.Parse(stream) ?? throw new InvalidDataException($"转换映射为空：{path}");
    });

    public static byte[] Bytes(string relative) => BINARY.GetOrAdd(relative, static path => {
        using Stream input = Open(path);
        using MemoryStream output = new();
        input.CopyTo(output);
        return output.ToArray();
    });

    static Stream Open(string relative) => ASSEMBLY.GetManifestResourceStream(PREFIX + relative.Replace('/', '.'))
        ?? throw new InvalidDataException($"缺少嵌入的转换规则：{relative}");
}
