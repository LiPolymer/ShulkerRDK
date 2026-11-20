using System.Diagnostics;
using System.Net;

namespace ShulkerRDK.Shared;

public static class FileDownloader {
    public static void DownloadFile(string fileUrl,string destinationPath,bool progBar = true) {
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
                UpdateProgressBar(bytesReceived,totalBytes,!progBar);
                lastPercentage = (int)(bytesReceived * 100 / totalBytes);
                stopwatch.Restart();
            }

            // 下载完成后显示完整进度条
            if (canReportProgress) {
                UpdateProgressBar(totalBytes,totalBytes,!progBar);
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

    static void UpdateProgressBar(long bytesReceived,long totalBytes,bool bypass = false) {
        if (bypass) return;
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
