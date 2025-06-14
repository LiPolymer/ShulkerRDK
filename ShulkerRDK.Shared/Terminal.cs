using System.Globalization;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ShulkerRDK.Shared;

public interface ITerminalProvider {
    public void Write(string content);
    public void WriteLine(string content);
}

public static class Terminal {
    static ITerminalProvider? _provider;
    public static void Init(ITerminalProvider? provider) {
        _provider = provider;
    }
    
    public static ITerminalProvider GetCurrentProvider() {
        return _provider ?? new LegacyTerminal();
    }
    
    public static void WriteLine(string unit, string content, MessageType type = MessageType.Info) {
        #if !DEBUG
        if (type == MessageType.Debug){
            return;
        }
        #endif
        string prefix = type switch {
            MessageType.Info => "&2INFO",
            MessageType.None => string.Empty,
            MessageType.Warn => "&cWARN",
            MessageType.Error => "&4ERRO",
            MessageType.Critical => "&4FAIL",
            // ReSharper disable once UnreachableSwitchArmDueToIntegerAnalysis
            MessageType.Debug => "&9DeBG",
            _ => throw new ArgumentOutOfRangeException(nameof(type),type,null)
        };
        WriteLine($"&8[&3{DateTime.Now.ToString("T",CultureInfo.InvariantCulture)} {prefix}&8]&7{unit}&r&8>&r{content}");
    }
    public static void Write(string content) {
        _provider?.Write(content);
    }
    public static void WriteLine(string content) {
        _provider?.WriteLine(content);
    }
    public enum MessageType {
        None,
        Info,
        Warn,
        Error,
        Critical,
        Debug
    }
}

public class ChainedTerminal {
    readonly List<string> _unitChain;
    public ChainedTerminal(string firstNode) {
        _unitChain = [firstNode];
    }
    public ChainedTerminal(List<string> chain) {
        _unitChain = chain;
    }

    public ChainedTerminal Chain(string node) {
        List<string> newChain = [];
        _unitChain.ForEach(n => newChain.Add(n));
        newChain.Add(node);
        return new ChainedTerminal(newChain);
    }

    public void AddNode(string node) {
        _unitChain.Add(node);
    }

    public void WriteLine(string content, Terminal.MessageType type = Terminal.MessageType.Info) {
        string prefix = string.Empty;
        _unitChain.ForEach(n => prefix += n + "&8>&r");
        Terminal.WriteLine(prefix, content, type);
    }
}

public partial class AnsiTerminal : ITerminalProvider {
    static string McFormatterToAnsi(string minecraftColor) {
        RgbColor? rgbColor = minecraftColor switch {
            "0" => new RgbColor { R = 0,G = 0,B = 0 }, // 黑色
            "1" => new RgbColor { R = 0,G = 0,B = 170 }, // 深蓝色
            "2" => new RgbColor { R = 0,G = 170,B = 0 }, // 深绿色
            "3" => new RgbColor { R = 0,G = 170,B = 170 }, // 深青色
            "4" => new RgbColor { R = 170,G = 0,B = 0 }, // 深红色
            "5" => new RgbColor { R = 170,G = 0,B = 170 }, // 紫色
            "6" => new RgbColor { R = 255,G = 170,B = 0 }, // 金色
            "7" => new RgbColor { R = 170,G = 170,B = 170 }, // 灰色
            "8" => new RgbColor { R = 85,G = 85,B = 85 }, // 深灰色
            "9" => new RgbColor { R = 85,G = 85,B = 255 }, // 蓝色
            "a" => new RgbColor { R = 85,G = 255,B = 85 }, // 绿色
            "b" => new RgbColor { R = 85,G = 255,B = 255 }, // 天蓝色
            "c" => new RgbColor { R = 255,G = 85,B = 85 }, // 红色
            "d" => new RgbColor { R = 255,G = 85,B = 255 }, // 粉红色
            "e" => new RgbColor { R = 255,G = 255,B = 85 }, // 黄色
            "f" => new RgbColor { R = 255,G = 255,B = 255 }, // 白色
            "g" => new RgbColor { R = 221,G = 214,B = 5 }, // 硬币金
            _ => null
        };
        if (rgbColor != null) return $"\x1b[38;2;{rgbColor.R};{rgbColor.G};{rgbColor.B}m";
        return minecraftColor switch {
            "l" => "\x1b[1m",
            "m" => "\x1b[9m",
            "n" => "\x1b[4m",
            "o" => "\x1b[3m",
            "r" => "\x1b[0m",
            "&" => "&",
            _ => ""
        };
    }

    class RgbColor {
        public int R { get; init; }
        public int G { get; init; }
        public int B { get; init; }
    }

    [GeneratedRegex(@"&(.)")]
    private static partial Regex FormatterRegex();

    static string Format(string input,bool resetAtEnd = true) {
        if (resetAtEnd) input += "&r";
        return FormatterRegex().Replace(input,match => {
            string fmtChar = match.Groups[1].Value;
            return McFormatterToAnsi(fmtChar);
        });
    }

    public void Write(string content) {
        Console.Write(Format(content, false));
    }
    
    public void WriteLine(string content) {
        Console.WriteLine(Format(content));
    }
}

public class LegacyTerminal : ITerminalProvider {
    readonly ConsoleColor _defaultForeground = Console.ForegroundColor;
    static List<Tuple<string,string>> Format(string input) {
        List<Tuple<string,string>> result = [];
        if (string.IsNullOrEmpty(input))
            return result;
        int firstAmpIndex = input.IndexOf('&');
        if (firstAmpIndex == -1) {
            result.Add(Tuple.Create("r",input));
            return result;
        }
        string firstPart = input[..firstAmpIndex];
        result.Add(Tuple.Create("r",firstPart));
        string remaining = input[firstAmpIndex..];
        while (remaining.Length >= 2) {
            char key = remaining[1];
            string nextPart = remaining[2..];

            int nextAmpIndex = nextPart.IndexOf('&');
            if (nextAmpIndex == -1) {
                result.Add(Tuple.Create(key.ToString(),nextPart));
                break;
            }
            string value = nextPart[..nextAmpIndex];
            result.Add(Tuple.Create(key.ToString(),value));
            remaining = nextPart[nextAmpIndex..];
        }
        return result;
    }
    static ConsoleColor? McColorToConsoleColor(string minecraftColor) {
        return minecraftColor switch {
            "0" => ConsoleColor.Black,
            "1" => ConsoleColor.DarkBlue,
            "2" => ConsoleColor.DarkGreen,
            "3" => ConsoleColor.DarkCyan,
            "4" => ConsoleColor.DarkRed,
            "5" => ConsoleColor.DarkMagenta,
            "6" => ConsoleColor.DarkYellow,
            "7" => ConsoleColor.Gray,
            "8" => ConsoleColor.DarkGray,
            "9" => ConsoleColor.Blue,
            "a" => ConsoleColor.Green,
            "b" => ConsoleColor.Cyan,
            "c" => ConsoleColor.Red,
            "d" => ConsoleColor.Magenta,
            "e" => ConsoleColor.Yellow,
            "f" => ConsoleColor.White,
            "g" => ConsoleColor.DarkYellow,
            _ => null
        };
    }
    
    public void Write(string content) {
        List<Tuple<string,string>> parts = Format(content);
        foreach (Tuple<string,string> part in parts) {
            ConsoleColor? color = part.Item1 == "r" ? _defaultForeground : McColorToConsoleColor(part.Item1);
            if (color != null) {
                Console.ForegroundColor = color.Value;
            }
            Console.Write(part.Item2);
        }
    }

    public void WriteLine(string content) {
        ConsoleColor defaultColor = Console.ForegroundColor;
        Write(content);
        Console.ForegroundColor = defaultColor;
        Console.WriteLine();
    }
}

public partial class MonoTerminal : ITerminalProvider {
    [GeneratedRegex(@"&(.)")]
    private static partial Regex FormatterRegex();
    
    public void Write(string content) {
        Console.Write(FormatterRegex().Replace(content,match => ""));
    }
    public void WriteLine(string content) {
        Console.WriteLine(FormatterRegex().Replace(content,match => ""));
    }
}