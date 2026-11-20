using System.Diagnostics;

namespace ShulkerRDK.Shared;

public static class FileDownloader {
    static readonly HttpClient Client = CreateClient();

    static HttpClient CreateClient() {
        HttpClient client = new();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("ShulkerRDK");
        return client;
    }

    public static void DownloadFile(string fileUrl,string destinationPath,bool progBar = true) {
        string tempPath = destinationPath + ".tmp";
        try {
            string? dir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            // 获取响应
            using HttpResponseMessage response = Client.GetAsync(fileUrl,HttpCompletionOption.ResponseHeadersRead).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode) {
                Terminal.WriteLine("",$"下载失败: {response.ReasonPhrase}",Terminal.MessageType.Error);
                Terminal.WriteLine("",$"HTTP状态码: {(int)response.StatusCode}",Terminal.MessageType.Error);
                return;
            }

            using Stream responseStream = response.Content.ReadAsStream();
            // 获取文件总大小
            long totalBytes = response.Content.Headers.ContentLength ?? -1;
            bool canReportProgress = totalBytes > 0;
            long bytesReceived = 0;

            using (FileStream fileStream = File.Create(tempPath)) {
                byte[] buffer = new byte[4096];
                int bytesRead;
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
            }

            File.Move(tempPath,destinationPath,true);

            // 下载完成后显示完整进度条
            if (canReportProgress) {
                if (progBar) {
                    UpdateProgressBar(totalBytes,totalBytes,false);
                    Terminal.Write("&e完成!&r");
                    Console.WriteLine();
                }
            } else if (progBar) {
                Terminal.WriteLine($"下载完成！大小: {FormatFileSize(bytesReceived)}");
            }
        }
        catch (HttpRequestException ex) {
            TryDeleteTempFile(tempPath);
            Terminal.WriteLine("",$"下载失败: {ex.Message}",Terminal.MessageType.Error);
            if (ex.StatusCode != null) {
                Terminal.WriteLine("",$"HTTP状态码: {(int)ex.StatusCode.Value}",Terminal.MessageType.Error);
            }
        }
        catch (Exception ex) {
            TryDeleteTempFile(tempPath);
            Terminal.WriteLine("",$"{ex.Message}",Terminal.MessageType.Error);
        }
    }

    static void TryDeleteTempFile(string tempPath) {
        try {
            if (File.Exists(tempPath)) {
                File.Delete(tempPath);
            }
        }
        catch {
            // ignored
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
        if (!Console.IsOutputRedirected) {
            Console.SetCursorPosition(0,Console.CursorTop);
        }
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
