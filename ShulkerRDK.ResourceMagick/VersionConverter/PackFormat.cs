using System.Globalization;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

readonly record struct PackFormat(int Major, int Minor) : IComparable<PackFormat> {
    public static PackFormat Parse(string value) {
        ArgumentNullException.ThrowIfNull(value);
        ReadOnlySpan<char> text = value.AsSpan();
        int separator = text.IndexOf('.');
        ReadOnlySpan<char> majorText = separator < 0 ? text : text[..separator];
        ReadOnlySpan<char> minorText = separator < 0 ? "0" : text[(separator + 1)..];
        if (majorText.IsEmpty || minorText.IsEmpty
            || majorText.IndexOfAnyExceptInRange('0', '9') >= 0 || minorText.IndexOfAnyExceptInRange('0', '9') >= 0
            || !int.TryParse(majorText, NumberStyles.None, CultureInfo.InvariantCulture, out int major)
            || !int.TryParse(minorText, NumberStyles.None, CultureInfo.InvariantCulture, out int minor)) {
            throw new FormatException($"Invalid resource-pack format '{value}'. Use an integer or major.minor pack format, not a Minecraft game version.");
        }

        PackFormat format = new(major, minor);
        if (!VersionRules.Supports(format)) {
            throw new FormatException($"ResourcePackConverter 没有包格式 '{value}' 的转换规则。支持的包格式：{VersionRules.SupportedFormats}；不接受游戏版本或未知次版本。");
        }
        return format;
    }

    public int CompareTo(PackFormat other) {
        int majorComparison = Major.CompareTo(other.Major);
        return majorComparison != 0 ? majorComparison : Minor.CompareTo(other.Minor);
    }

    public override string ToString() {
        return Minor == 0
            ? Major.ToString(CultureInfo.InvariantCulture)
            : string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}");
    }
}
