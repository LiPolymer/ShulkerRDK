using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static string AliasResolver(string original,Dictionary<string,string> aliases) {
        foreach (KeyValuePair<string,string> alias in aliases) {
            Regex regex = new Regex(alias.Key);
            Match match = regex.Match(original);
            if (match.Success) {
                Terminal.WriteLine("AliasResolver",$"&7PatternMatched&8[&7{alias.Key}&8]",Terminal.MessageType.Debug);
                return regex.Replace(original,alias.Value);
            }
        }
        return original;
    }

    public static string[] AliasResolver(string[] original,Dictionary<string,string> aliases) {
        string partAsm = string.Empty;
        for (int index = 0; index < original.Length; index++) {
            partAsm = index == 0 ? original[0].Trim() : $"{partAsm} {original[index]}".Trim();
            string resolvedAsm = AliasResolver(partAsm,aliases);
            if (resolvedAsm != partAsm) {
                return [..ResolveArgs(resolvedAsm), ..GetSubGroup(original,index + 1)];
            }
        }
        return original;
    }

    const string Base36Chars = "0123456789abcdefghijklmnopqrstuvwxyz";

    [GeneratedRegex(@"^([RBADrbad])(\d+)\.([0-9a-zA-Z]{2})(?:fix(\d+))?$")]
    private static partial Regex ShulkerVersionRegex();

    [GeneratedRegex(@"^(\d+)(.+)$")]
    private static partial Regex LeadingDigitsRegex();

    [GeneratedRegex(@"^(.*?)(\d+)$")]
    private static partial Regex TrailingDigitsRegex();

    public static string VersionStepper(string input,int depth) {
        if (string.IsNullOrEmpty(input) || depth < 0) return input;

        Match shulkerMatch = ShulkerVersionRegex().Match(input);
        if (shulkerMatch.Success) {
            return StepShulkerVersion(shulkerMatch,input,depth);
        }

        string[] resolved = input.Split('.');
        if (resolved.Length - 1 < depth) return input;

        int targetIndex = resolved.Length - 1 - depth;
        string? stepped = TransformVersionSegment(resolved[targetIndex],n => n + 1);
        if (stepped == null) return input;

        resolved[targetIndex] = stepped;
        for (int i = targetIndex + 1; i < resolved.Length; i++) {
            resolved[i] = TransformVersionSegment(resolved[i],_ => 0) ?? resolved[i];
        }
        return string.Join(".",resolved);
    }

    static string StepShulkerVersion(Match match,string original,int depth) {
        string status = match.Groups[1].Value;
        if (!long.TryParse(match.Groups[2].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out long major)) {
            return original;
        }
        string minor = match.Groups[3].Value.ToLowerInvariant();
        long fix = 0;
        if (match.Groups[4].Success &&
            !long.TryParse(match.Groups[4].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out fix)) {
            return original;
        }

        switch (depth) {
            case 0:
                return $"{status}{major}.{minor}fix{fix + 1}";
            case 1:
                minor = StepShulkerMinor(minor,ref major);
                return $"{status}{major}.{minor}";
            case 2:
                return $"{status}{major + 1}.00";
            default:
                return original;
        }
    }

    static string StepShulkerMinor(string minor,ref long major) {
        char c0 = minor[0];
        char c1 = minor[1];

        if (char.IsAsciiDigit(c0) && char.IsAsciiDigit(c1)) {
            int val = (c0 - '0') * 10 + (c1 - '0');
            return val < 99 ? (val + 1).ToString("D2",CultureInfo.InvariantCulture) : "a0";
        }

        int d0 = Base36Chars.IndexOf(c0);
        int d1 = Base36Chars.IndexOf(c1);
        if (d1 + 1 < Base36Chars.Length) {
            d1++;
        } else {
            d1 = 0;
            if (d0 + 1 < Base36Chars.Length) {
                d0++;
            } else {
                d0 = 0;
                major++;
            }
        }
        return $"{Base36Chars[d0]}{Base36Chars[d1]}";
    }

    static string? TransformVersionSegment(string segment,Func<long,long> transform) {
        if (long.TryParse(segment,NumberStyles.Integer,CultureInfo.InvariantCulture,out long num)) {
            return transform(num).ToString(CultureInfo.InvariantCulture);
        }

        Match leadingMatch = LeadingDigitsRegex().Match(segment);
        if (leadingMatch.Success &&
            long.TryParse(leadingMatch.Groups[1].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out long leadingNum)) {
            return $"{transform(leadingNum)}{leadingMatch.Groups[2].Value}";
        }

        Match trailingMatch = TrailingDigitsRegex().Match(segment);
        if (trailingMatch.Success &&
            long.TryParse(trailingMatch.Groups[2].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out long trailingNum)) {
            return $"{trailingMatch.Groups[1].Value}{transform(trailingNum)}";
        }

        return null;
    }

    //// Resolver

    public static string[] ResolveArgs(string st) {
        return ArgResolveRegex().Matches(st)
            .Select(m =>
                        m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value
                   )
            .ToArray();
    }

    [GeneratedRegex("""
                    "([^"]*)"|(\S+)
                    """)]
    private static partial Regex ArgResolveRegex();

    public static string EscapeDictResolver(string input,Dictionary<string,string> replacements,string separator) {
        return EscapeReplacer(input,separator,key => replacements.TryGetValue(key,out string? value) ? value : string.Empty);
    }

    public static string EscapeReplacer(string input,string separator,Func<string,string> replacer) {
        // 确保分隔符为非空且长度为1
        if (string.IsNullOrEmpty(separator) || separator.Length != 1) {
            throw new ArgumentException("分隔符必须为单符号");
        }
        // 转义分隔符，防止正则表达式特殊字符
        string escapedSeparator = Regex.Escape(separator);
        string pattern = $"({escapedSeparator})([^{escapedSeparator}]+)\\1|{escapedSeparator}{{2}}";
        return Regex.Replace(input,pattern,match => {
            // 判断是否为转义
            if (!match.Groups[2].Success) return separator;
            string key = match.Groups[2].Value;
            return replacer(key);
        });
    }

    public static string CrateReplacer(string input,string open,string close,Func<string,string> replacer) {
        // 确保分隔符为非空且长度为1
        if (string.IsNullOrEmpty(open) || open.Length != 1 || string.IsNullOrEmpty(close) || close.Length != 1) {
            throw new ArgumentException("分隔符必须单符号");
        }

        char openChar = open[0];
        char closeChar = close[0];

        if (openChar == closeChar) {
            return EscapeReplacer(input,open,key => replacer(key.Trim()));
        }

        Stack<(StringBuilder Buffer, int EscapedOpens)> stack = new();
        StringBuilder current = new();
        int currentEscapedOpens = 0;

        int i = 0;
        while (i < input.Length) {
            char c = input[i];
            if (c == openChar) {
                if (i + 1 < input.Length && input[i + 1] == openChar) {
                    current.Append(openChar);
                    currentEscapedOpens++;
                    i += 2;
                } else {
                    stack.Push((current, currentEscapedOpens));
                    current = new StringBuilder();
                    currentEscapedOpens = 0;
                    i++;
                }
            } else if (c == closeChar) {
                bool hasDoubleClose = i + 1 < input.Length && input[i + 1] == closeChar;
                if (hasDoubleClose && (stack.Count == 0 || currentEscapedOpens > 0)) {
                    current.Append(closeChar);
                    if (currentEscapedOpens > 0) currentEscapedOpens--;
                    i += 2;
                } else if (stack.Count > 0) {
                    string key = current.ToString().Trim();
                    string replaced = replacer(key);
                    (current, currentEscapedOpens) = stack.Pop();
                    current.Append(replaced);
                    i++;
                } else {
                    current.Append(closeChar);
                    i++;
                }
            } else {
                current.Append(c);
                i++;
            }
        }

        while (stack.Count > 0) {
            string unclosed = current.ToString();
            (current, currentEscapedOpens) = stack.Pop();
            current.Append(openChar).Append(unclosed);
        }

        return current.ToString();
    }
}
