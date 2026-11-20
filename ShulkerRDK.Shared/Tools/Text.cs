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
