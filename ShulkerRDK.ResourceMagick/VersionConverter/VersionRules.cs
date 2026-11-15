using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

static class VersionRules {
    static readonly Dictionary<int, (int Minimum, int Maximum)> PROTOCOLS = LoadProtocols();
    static readonly string SUPPORTED = string.Join(", ", PROTOCOLS.Keys.Order());

    public static bool Supports(PackFormat format) => format.Minor == 0 && PROTOCOLS.ContainsKey(format.Major);

    public static string SupportedFormats => SUPPORTED;

    public static (int Minimum, int Maximum) Protocols(PackFormat format) {
        if (Supports(format)) return PROTOCOLS[format.Major];
        throw new InvalidDataException($"ResourcePackConverter 没有包格式 {format} 的规则。支持：{SUPPORTED}。");
    }

    static Dictionary<int, (int Minimum, int Maximum)> LoadProtocols() {
        Dictionary<int, (int Minimum, int Maximum)> result = new();
        foreach ((string _, JsonNode? entry) in RuleData.Json("protocol.json").AsObject()) {
            if (entry is null) continue;
            decimal rawFormat = entry["pack_format"]!.GetValue<decimal>();
            if (decimal.Truncate(rawFormat) != rawFormat) throw new InvalidDataException("上游版本表包含未支持的次版本格式。");
            int format = checked((int)rawFormat);
            int protocol = entry["protocol_version"]!.GetValue<int>();
            if (result.TryGetValue(format, out var band)) {
                result[format] = (Math.Min(protocol, band.Minimum), Math.Max(protocol, band.Maximum));
            } else {
                result.Add(format, (protocol, protocol));
            }
        }
        return result;
    }
}
