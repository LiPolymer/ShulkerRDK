using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

static class PackMetadata {
    const string ExplicitSourceHint = " Specify an explicit source pack format instead of relying on metadata detection.";
    static readonly JsonSerializerOptions OUTPUT_OPTIONS = new JsonSerializerOptions { WriteIndented = true };

    public static JsonObject Read(Stream input) {
        ArgumentNullException.ThrowIfNull(input);
        try {
            return JsonNode.Parse(input) as JsonObject
                ?? throw new InvalidDataException("pack.mcmeta must contain a JSON object.");
        } catch (JsonException exception) {
            throw new InvalidDataException("pack.mcmeta contains invalid JSON.", exception);
        }
    }

    public static PackFormat? Detect(JsonObject root) {
        ArgumentNullException.ThrowIfNull(root);
        if (!root.TryGetPropertyValue("pack", out JsonNode? packNode)) return null;
        if (packNode is not JsonObject pack) throw DetectionError("The pack metadata field must be an object.");

        bool hasMinimum = pack.TryGetPropertyValue("min_format", out JsonNode? minimumNode);
        bool hasMaximum = pack.TryGetPropertyValue("max_format", out JsonNode? maximumNode);
        if (hasMinimum || hasMaximum) {
            if (!hasMinimum || !hasMaximum) {
                throw DetectionError("Both min_format and max_format are needed to determine a unique source format.");
            }
            PackFormat minimum = ReadBound(minimumNode, "min_format", false);
            PackFormat maximum = ReadBound(maximumNode, "max_format", true);
            return UniqueRange(minimum, maximum, "min_format/max_format");
        }

        if (pack.TryGetPropertyValue("pack_format", out JsonNode? formatNode)) {
            return ReadFormat(ReadInteger(formatNode, "pack_format"), 0, "pack_format");
        }

        if (!pack.TryGetPropertyValue("supported_formats", out JsonNode? supportedNode)) return null;
        if (supportedNode is JsonArray range) {
            if (range.Count != 2) throw DetectionError("supported_formats must contain exactly two integer endpoints.");
            PackFormat minimum = ReadFormat(ReadInteger(range[0], "supported_formats[0]"), 0, "supported_formats[0]");
            PackFormat maximum = ReadFormat(ReadInteger(range[1], "supported_formats[1]"), 0, "supported_formats[1]");
            return UniqueRange(minimum, maximum, "supported_formats");
        }
        if (supportedNode is JsonObject bounds) {
            PackFormat minimum = ReadFormat(ReadInteger(bounds["min_inclusive"], "supported_formats.min_inclusive"), 0, "supported_formats.min_inclusive");
            PackFormat maximum = ReadFormat(ReadInteger(bounds["max_inclusive"], "supported_formats.max_inclusive"), 0, "supported_formats.max_inclusive");
            return UniqueRange(minimum, maximum, "supported_formats");
        }
        return ReadFormat(ReadInteger(supportedNode, "supported_formats"), 0, "supported_formats");
    }

    public static byte[] Rewrite(JsonObject root, PackFormat target) {
        ArgumentNullException.ThrowIfNull(root);
        target = PackFormat.Parse(target.ToString());
        JsonObject result = (JsonObject)root.DeepClone();
        JsonObject pack;
        if (!result.TryGetPropertyValue("pack", out JsonNode? packNode)) {
            pack = new JsonObject();
            result["pack"] = pack;
        } else {
            pack = packNode as JsonObject
                ?? throw new InvalidDataException("The pack metadata field must be an object.");
        }

        pack.Remove("pack_format");
        pack.Remove("supported_formats");
        pack.Remove("min_format");
        pack.Remove("max_format");
        if (target.Major >= 65) {
            pack["min_format"] = new JsonArray(target.Major, target.Minor);
            pack["max_format"] = new JsonArray(target.Major, target.Minor);
        } else {
            pack["pack_format"] = target.Major;
        }
        return JsonSerializer.SerializeToUtf8Bytes(result, OUTPUT_OPTIONS);
    }

    static PackFormat ReadBound(JsonNode? node, string field, bool maximum) {
        int major;
        int minor;
        if (node is JsonArray parts) {
            if (parts.Count is < 1 or > 2) {
                throw DetectionError($"{field} must be an integer or a one- or two-element integer array.");
            }
            major = ReadInteger(parts[0], $"{field}[0]");
            minor = parts.Count == 2 ? ReadInteger(parts[1], $"{field}[1]") : maximum ? int.MaxValue : 0;
        } else {
            major = ReadInteger(node, field);
            minor = maximum ? int.MaxValue : 0;
        }

        if (minor == int.MaxValue && maximum && (node is not JsonArray array || array.Count == 1)) {
            ReadFormat(major, 0, field);
            return new PackFormat(major, int.MaxValue);
        }
        return ReadFormat(major, minor, field);
    }

    static int ReadInteger(JsonNode? node, string field) {
        if (node is not JsonValue value || value.GetValueKind() != JsonValueKind.Number
            || !int.TryParse(value.ToJsonString(), NumberStyles.None, CultureInfo.InvariantCulture, out int number)) {
            throw DetectionError($"{field} must be a non-negative JSON integer within the 32-bit integer range; decimals, exponents, strings and game versions are not accepted.");
        }
        return number;
    }

    static PackFormat ReadFormat(int major, int minor, string field) {
        try {
            return PackFormat.Parse(string.Create(CultureInfo.InvariantCulture, $"{major}.{minor}"));
        } catch (FormatException exception) {
            throw new InvalidDataException($"{field} contains an unsupported resource-pack format {major}.{minor}." + ExplicitSourceHint, exception);
        }
    }

    static PackFormat UniqueRange(PackFormat minimum, PackFormat maximum, string field) {
        int comparison = minimum.CompareTo(maximum);
        if (comparison > 0) throw DetectionError($"{field} has a minimum greater than its maximum.");
        if (comparison < 0) throw DetectionError($"{field} describes a range, not a unique source pack format.");
        return minimum;
    }

    static InvalidDataException DetectionError(string message) {
        return new InvalidDataException(message + ExplicitSourceHint);
    }
}
