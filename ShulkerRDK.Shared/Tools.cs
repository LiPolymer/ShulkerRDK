using System.Diagnostics;
using System.Drawing;
using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ShulkerRDK.Shared;

public static partial class Tools {
    public static void MergeDict(Dictionary<string,Action<string[],ShulkerContext>> master,Dictionary<string,Action<string[],ShulkerContext>> branch) {
        foreach (KeyValuePair<string,Action<string[],ShulkerContext>> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }
    public static void MergeDict(Dictionary<string,LevitateMethod> master,
        Dictionary<string,LevitateMethod> branch) {
        foreach (KeyValuePair<string,LevitateMethod> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }
    public static void MergeDict(Dictionary<string,string> master,Dictionary<string,string> branch) {
        foreach (KeyValuePair<string,string> obj in branch) {
            master.Add(obj.Key,obj.Value);
        }
    }

    public static string AliasResolver(string original,Dictionary<string,string> aliases) {
        foreach (KeyValuePair<string,string> alias in aliases) {
            Regex regex = new Regex(alias.Key);
            if (regex.IsMatch(original)) {
                return regex.Replace(original,alias.Value);
            }
        }
        return original;
    }

    public static string[] AliasResolver(string[] original,Dictionary<string,string> aliases) {
        foreach (KeyValuePair<string,string> alias in aliases) {
            string partAsm = string.Empty;
            int index = 0;
            foreach (string part in original) {
                partAsm += " " + part;
                partAsm = partAsm.Trim();
                string resolvedAsm = AliasResolver(part,aliases);
                if (resolvedAsm != partAsm) {
                    List<string> parts = ResolveArgs(resolvedAsm).ToList();
                    parts.AddRange(GetSubGroup(original,index + 1));
                    return parts.ToArray();
                }
                index++;
            }
        }
        return original;
    }

    public static string[] GetSubGroup(string[] group,int startIndex) {
        List<string> result = [];
        for (int i = startIndex; i < group.Length; i++) {
            result.Add(group[i]);
        }
        return result.ToArray();
    }

    public static void WriteAllText(string path,string content) {
        if (!Directory.Exists(Path.GetDirectoryName(path)!)) {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        }
        File.WriteAllText(path,content);
    }

    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions {
        WriteIndented = true
    };

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

    // 尝试/检查 获取/执行
    public static bool TryRunSub(Dictionary<string,LevitateMethod> collection,string[] args,int depth,LevitateExecutionContext ec) {
        return TryRunSub(collection,args,depth,ec,out _);
    }
    public static bool TryRunSub(Dictionary<string,LevitateMethod> collection,string[] args,int depth,LevitateExecutionContext ec,out string? result) {
        LevitateMethod? method = TryGetSub(collection,args,depth,ec);
        if (method == null) {
            result = null;
            return false;
        }
        result = method(args,ec);
        return true;
    }

    public static bool TryRunSub(Dictionary<string,Action<string[]>> collection,string[] args,int depth,ChainedTerminal? ct = null) {
        Action<string[]>? action = TryGetSub(collection,args,depth,ct);
        if (action == null) {
            return false;
        }
        action(args);
        return true;
    }

    public static LevitateMethod? TryGetSub(Dictionary<string,LevitateMethod> collection,string[] args,int depth,LevitateExecutionContext ec) {
        if (!CheckParamLength(args,depth,ec)) return null;
        if (collection.TryGetValue(args[depth],out LevitateMethod? subM)) {
            return subM;
        }
        DisplayUnknownParam(args,depth,ec);
        return null;
    }
    public static bool TryGetSub(List<string> collection,string[] args,int depth,LevitateExecutionContext ec) {
        if (!CheckParamLength(args,depth,ec)) return false;
        if (collection.Contains(args[depth])) {
            return true;
        }
        DisplayUnknownParam(args,depth,ec);
        return false;
    }
    public static bool TryGetSub(List<string> collection,string[] args,int depth,ChainedTerminal ct) {
        if (!CheckParamLength(args,depth,ct)) return false;
        if (collection.Contains(args[depth])) {
            return true;
        }
        DisplayUnknownParam(args,depth,ct);
        return false;
    }

    public static Action<string[]>? TryGetSub(Dictionary<string,Action<string[]>> collection,string[] args,int depth,ChainedTerminal? ct = null) {
        if (!CheckParamLength(args,depth,ct)) return null;
        if (collection.TryGetValue(args[depth],out Action<string[]>? subA)) {
            return subA;
        }
        if (ct != null) {
            DisplayUnknownParam(args,depth,ct);
        }
        return null;
    }

    public static bool CheckParamLength(string[] args,int depth,LevitateExecutionContext? ec = null) {
        if (args.Length >= depth + 1) return true;
        if (ec != null) {
            DisplayMissingParam(args,depth,ec);
        }
        return false;
    }

    public static bool CheckParamLength(string[] args,int depth,ChainedTerminal? ct = null) {
        if (args.Length >= depth + 1) return true;
        if (ct != null) {
            DisplayMissingParam(args,depth,ct);
        }
        return false;
    }

    static void DisplayMissingParam(string[] args,int depth,LevitateExecutionContext ec) {
        List<string> argList = args.ToList();
        argList.Add("_");
        ec.Logger.WriteLine($"缺少参数&8[{HighLightParam(argList.ToArray(),depth)}&8]",Terminal.MessageType.Error);
    }
    static void DisplayMissingParam(string[] args,int depth,ChainedTerminal ct) {
        List<string> argList = args.ToList();
        argList.Add("_");
        ct.WriteLine($"缺少参数&8[{HighLightParam(argList.ToArray(),depth)}&8]",Terminal.MessageType.Error);
    }

    static void DisplayUnknownParam(string[] args,int depth,LevitateExecutionContext ec) {
        ec.Logger.WriteLine($"未知参数&8[{HighLightParam(args,depth)}&8]",Terminal.MessageType.Error);
    }
    static void DisplayUnknownParam(string[] args,int depth,ChainedTerminal ct) {
        ct.WriteLine($"未知参数&8[{HighLightParam(args,depth)}&8]",Terminal.MessageType.Error);
    }

    static string HighLightParam(string[] args,int depth) {
        string buffer = "&7";
        for (int i = 0; i < args.Length; i++) {
            if (i == depth) {
                buffer += "&c";
            }
            buffer += $"{args[i]}&7 ";
        }
        return buffer.TrimEnd();
    }
}

public static class NugetHelper {
    public static void DependencyVerify(string packageIdentifier,string libTarget = "net8.0",bool extractRuntime = true,string[]? checkFiles = null) {
        try {
            checkFiles ??= [packageIdentifier.Split('/')[0] + ".dll"];
            bool passed = true;
            foreach (string checkpoint in checkFiles) {
                if (checkpoint.Contains('|')) {
                    bool subPassed = checkpoint.Split("|").Any(subPoint => File.Exists(Path.Combine("./shulker/local/libs",subPoint)));
                    passed = subPassed;
                } else if (!File.Exists(Path.Combine("./shulker/local/libs",checkpoint))) {
                    passed = false;
                }
            }
            if (passed) return;
            if (!Directory.Exists("./shulker/local/libs")) {
                Directory.CreateDirectory("./shulker/local/libs");
            }
            ChainedTerminal logger = new ChainedTerminal("&9Nuget");
            logger.WriteLine($"正在获取包&8[&7{packageIdentifier}&8]");
            const string cache = "./shulker/local/cache/";
            string pkgCache = $"{cache}{packageIdentifier.Split('/')[0]}.nupkg";
            string extCache = $"{cache}{packageIdentifier.Split('/')[0]}/";
            FileDownloader.DownloadFile($"https://www.nuget.org/api/v2/package/{packageIdentifier}",pkgCache);
            if (Directory.Exists(extCache)) {
                Directory.Delete(extCache,true);
            }
            logger.WriteLine("&7正在安装...");
            ZipFile.ExtractToDirectory(pkgCache,extCache);
            string[] files = Directory.GetFiles(Path.Combine(extCache,$"lib/{libTarget}/"),"*.dll",SearchOption.TopDirectoryOnly);
            foreach (string file in files) {
                File.Copy(file,Path.Combine("./shulker/local/libs",Path.GetFileName(file)),true);
            }
            if (extractRuntime) {
                if (Directory.Exists(Path.Combine(extCache,$"runtimes/"))) {
                    // 获取运行平台
                    string? platform = null;
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                        platform = "win";
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                        platform = "linux";
                    } else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                        platform = "osx";
                    }
                    if (platform == null) throw new Exception("未知平台");

                    // 获取设备架构
                    // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                    string arch = RuntimeInformation.ProcessArchitecture switch {
                        Architecture.X86 => "x86",
                        Architecture.X64 => "x64",
                        Architecture.Arm64 => "arm64",
                        _ => throw new Exception("未知架构")
                    };

                    string[] runtimes = Directory.GetFiles(Path.Combine(extCache,$"runtimes/{platform}-{arch}/native/"),"*.dll*",
                                                           SearchOption.TopDirectoryOnly);
                    foreach (string runtime in runtimes) {
                        File.Copy(runtime,Path.Combine("./shulker/local/libs",Path.GetFileName(runtime)),true);
                    }
                }
            }
            logger.WriteLine("&e依赖安装完成!");
            File.Delete(pkgCache);
            Directory.Delete(extCache,true);
        }
        catch (Exception e) {
            Console.WriteLine(e);
        }
    }
}

public static class FileDownloader {
    public static void DownloadFile(string fileUrl,string destinationPath) {
        try {
            if (!Directory.Exists(Path.GetDirectoryName(destinationPath))) {
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
            }
            if (File.Exists(destinationPath)) {
                File.Delete(destinationPath);
            }
            // 创建请求
#pragma warning disable SYSLIB0014
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fileUrl);
#pragma warning restore SYSLIB0014

            // 获取响应
            using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using Stream responseStream = response.GetResponseStream();
            using FileStream fileStream = File.Create(destinationPath);
            // 获取文件总大小
            long totalBytes = response.ContentLength;
            bool canReportProgress = totalBytes > 0;

            byte[] buffer = new byte[4096];
            int bytesRead;
            long bytesReceived = 0;
            Stopwatch stopwatch = Stopwatch.StartNew();
            int lastPercentage = -1;

            while ((bytesRead = responseStream.Read(buffer,0,buffer.Length)) > 0) {
                fileStream.Write(buffer,0,bytesRead);
                bytesReceived += bytesRead;

                // 更新进度（每100ms或当进度变化超过1%时更新）
                if (!canReportProgress || (stopwatch.ElapsedMilliseconds <= 100 &&
                                           (bytesReceived * 100 / totalBytes) == lastPercentage)) continue;
                UpdateProgressBar(bytesReceived,totalBytes);
                lastPercentage = (int)(bytesReceived * 100 / totalBytes);
                stopwatch.Restart();
            }

            // 下载完成后显示完整进度条
            if (canReportProgress) {
                UpdateProgressBar(totalBytes,totalBytes);
                Terminal.Write("&e完成!&r");
                Console.WriteLine();
            } else {
                Terminal.WriteLine($"下载完成！大小: {FormatFileSize(bytesReceived)}");
            }
        }
        catch (WebException ex) {
            Terminal.WriteLine("",$"下载失败: {ex.Message}",Terminal.MessageType.Error);
            if (ex.Response != null) {
                Terminal.WriteLine("",$"HTTP状态码: {(int)((HttpWebResponse)ex.Response).StatusCode}",Terminal.MessageType.Error);
            }
        }
        catch (Exception ex) {
            Terminal.WriteLine("",$"{ex.Message}",Terminal.MessageType.Error);
        }
    }

    static void UpdateProgressBar(long bytesReceived,long totalBytes) {
        const int progressBarLength = 50;
        double progress = (double)bytesReceived / totalBytes;
        int filledBlocks = (int)(progress * progressBarLength);

        // 创建进度条字符串
        string progressBar = $"&8/&7{new string('/',filledBlocks)}" +
                             $"{new string(' ',progressBarLength - filledBlocks)}&8/&6 " +
                             $"{progress:P1} ({FormatFileSize(bytesReceived)} / " +
                             $"{FormatFileSize(totalBytes)})&r   ";

        // 回到行首覆盖之前的显示
        Console.SetCursorPosition(0,Console.CursorTop);
        Terminal.Write(progressBar);
    }

    static string FormatFileSize(long bytes) {
        string[] sizes = ["B","KB","MB","GB"];
        int order = 0;
        double len = bytes;

        while (len >= 1024 && order < sizes.Length - 1) {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}