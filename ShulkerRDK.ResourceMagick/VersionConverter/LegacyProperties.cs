// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Globalization;
using System.Text;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class LegacyProperties
{
    internal static Dictionary<string, string> Read(ConversionWorkspace w, string file)
    {
        var bytes = File.ReadAllBytes(w.PathFor(file));
        string text;
        try { text = new UTF8Encoding(false, true).GetString(bytes).TrimStart('\uFEFF'); }
        catch (DecoderFallbackException) { text = Encoding.Latin1.GetString(bytes); }
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        using var reader = new StringReader(text);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            line = line.TrimStart(' ', '\t', '\f');
            if (line.Length == 0 || line[0] is '#' or '!') continue;
            while (Continues(line))
            {
                var next = reader.ReadLine();
                line = line[..^1] + (next?.TrimStart(' ', '\t', '\f') ?? "");
                if (next == null) break;
            }
            var separator = 0;
            for (; separator < line.Length; separator++)
            {
                if (line[separator] == '\\') { separator++; continue; }
                if (line[separator] is '=' or ':' or ' ' or '\t' or '\f') break;
            }
            var valueStart = separator;
            while (valueStart < line.Length && line[valueStart] is ' ' or '\t' or '\f') valueStart++;
            if (valueStart < line.Length && line[valueStart] is '=' or ':') valueStart++;
            while (valueStart < line.Length && line[valueStart] is ' ' or '\t' or '\f') valueStart++;
            try { result[Unescape(line[..Math.Min(separator, line.Length)])] = Unescape(line[valueStart..]); }
            catch (FormatException e) { throw new InvalidDataException($"Invalid Java-properties escape in '{file}'.", e); }
        }
        return result;
    }

    private static bool Continues(string line)
    {
        var slashes = 0;
        for (var i = line.Length - 1; i >= 0 && line[i] == '\\'; i--) slashes++;
        return slashes % 2 == 1;
    }

    private static string Unescape(string value)
    {
        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (c != '\\' || i + 1 == value.Length) { result.Append(c); continue; }
            c = value[++i];
            if (c == 'u')
            {
                if (i + 4 >= value.Length) throw new FormatException("Incomplete Unicode escape.");
                result.Append((char)ushort.Parse(value.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                i += 4;
            }
            else result.Append(c switch { 't' => '\t', 'r' => '\r', 'n' => '\n', 'f' => '\f', _ => c });
        }
        return result.ToString();
    }

    internal static void Write(ConversionWorkspace w, string file, IReadOnlyDictionary<string, string> values)
    {
        var result = new StringBuilder();
        foreach (var entry in values)
            result.Append(Escape(entry.Key, true)).Append('=').Append(Escape(entry.Value, false)).Append('\n');
        w.EnsureParent(file);
        File.WriteAllText(w.PathFor(file), result.ToString(), new UTF8Encoding(false));
        w.RecordModified();
    }

    private static string Escape(string value, bool key)
    {
        var result = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            switch (c)
            {
                case '\\': result.Append("\\\\"); break;
                case '\n': result.Append("\\n"); break;
                case '\r': result.Append("\\r"); break;
                case '\t': result.Append("\\t"); break;
                case '\f': result.Append("\\f"); break;
                default:
                    if ((key && c is '=' or ':' or '#' or '!' or ' ') || (!key && i == 0 && c == ' ')) result.Append('\\');
                    result.Append(c);
                    break;
            }
        }
        return result.ToString();
    }
}
