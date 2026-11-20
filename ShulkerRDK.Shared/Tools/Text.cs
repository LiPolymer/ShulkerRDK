using System.Text.RegularExpressions;

namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static string AliasResolver(string original,Dictionary<string,string> aliases) {
        foreach (KeyValuePair<string,string> alias in aliases) {
            Regex regex = new Regex(alias.Key);
            if (regex.IsMatch(original)) {
                Terminal.WriteLine("AliasResolver",$"&7PatternMatched&8[&7{alias.Key}&8]",Terminal.MessageType.Debug);
                return regex.Replace(original,alias.Value);
            }
        }
        return original;
    }

    public static string[] AliasResolver(string[] original,Dictionary<string,string> aliases) {
        string partAsm = string.Empty;
        int index = 0;
        foreach (string part in original) {
            partAsm += " " + part;
            partAsm = partAsm.Trim();
            string resolvedAsm = AliasResolver(partAsm,aliases);
            if (resolvedAsm != partAsm) {
                List<string> parts = ResolveArgs(resolvedAsm).ToList();
                parts.AddRange(GetSubGroup(original,index + 1));
                return parts.ToArray();
            }
            index++;
        }
        return original;
    }

    public static string VersionStepper(string input,int depth) {
        string[] resolved = input.Split('.');
        if (resolved.Length - 1 < depth) return input;
        int buffer = Convert.ToInt32(resolved[^(depth + 1)]);
        buffer++;
        resolved[^(depth + 1)] = buffer.ToString();
        return string.Join(".",resolved);
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

        // 转义替换为临时字符
        const char tempOpen = '\x01';
        const char tempClose = '\x02';

        string protect = input.Replace(open + open,tempOpen.ToString())
            .Replace(close + close,tempClose.ToString());

        // 构建正则表达式，匹配未被转义的目标结构
        string pattern = Regex.Escape(open) + "(.*?)" + Regex.Escape(close);
        Regex regex = new Regex(pattern);

        string resolved = regex.Replace(protect,match => {
            string key = match.Groups[1].Value.Trim();
            return replacer(key);
        });

        // 恢复转义字符，将临时字符替换回原始分隔符
        string result = resolved.Replace(tempOpen.ToString(),open)
            .Replace(tempClose.ToString(),close);

        return result;
    }
}
