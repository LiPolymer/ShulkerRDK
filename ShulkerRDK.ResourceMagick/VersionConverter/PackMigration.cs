using System.ComponentModel;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text.Json.Nodes;
using ImageMagick;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

readonly record struct PackMigrationResult(
    PackFormat SourceFormat,
    PackFormat TargetFormat,
    int CopiedFiles,
    int RenamedFiles,
    int ConvertedImages,
    int SkippedFiles);

static class PackMigration {
    const int MaximumFiles = 25000;
    const long MaximumBytes = 2L * 1024 * 1024 * 1024;
    static readonly StringComparer PATH_COMPARER = StringComparer.OrdinalIgnoreCase;

    public static PackMigrationResult Run(string sourcePath, string destinationPath, PackFormat? sourceFormat,
        PackFormat targetFormat, Action<string> warning) {
        ArgumentNullException.ThrowIfNull(warning);
        sourcePath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(sourcePath));
        destinationPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(destinationPath));
        if (PATH_COMPARER.Equals(sourcePath, destinationPath) || IsWithin(sourcePath, destinationPath) ||
            IsWithin(destinationPath, sourcePath)) {
            throw new IOException("源包和目标包不能相同，也不能互相包含。");
        }
        EnsureNoLinks(sourcePath);
        EnsureNoLinks(destinationPath);
        if (Path.Exists(destinationPath)) throw new IOException("目标路径已存在，不能覆盖。");
        bool sourceDirectory = Directory.Exists(sourcePath);
        if (!sourceDirectory && (!File.Exists(sourcePath) ||
            !Path.GetExtension(sourcePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))) {
            throw new IOException("源包必须是现有目录或 ZIP 文件。");
        }
        bool destinationZip = Path.GetExtension(destinationPath).Equals(".zip", StringComparison.OrdinalIgnoreCase);
        using FileStream? archiveStream = sourceDirectory ? null :
            new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using ZipArchive? archive = archiveStream is null ? null : new ZipArchive(archiveStream, ZipArchiveMode.Read);
        List<InputFile> files = [];
        HashSet<string> assetRoots = new HashSet<string>(PATH_COMPARER);
        long bytes = 0;
        if (sourceDirectory) ScanDirectory(sourcePath, files, assetRoots, ref bytes);
        else ScanArchive(archive!, files, assetRoots, ref bytes);

        InputFile? metadata = null;
        int metadataDepth = int.MaxValue;
        bool metadataAmbiguous = false;
        foreach (InputFile file in files) {
            if (sourceDirectory && file.Path.Contains('/')) continue;
            if (!GetName(file.Path).Equals("pack.mcmeta", StringComparison.OrdinalIgnoreCase)) continue;
            int depth = Depth(file.Path);
            if (depth < metadataDepth) {
                metadata = file;
                metadataDepth = depth;
                metadataAmbiguous = false;
            } else if (depth == metadataDepth) {
                metadataAmbiguous = true;
            }
        }
        if (metadataAmbiguous) throw new InvalidDataException("发现多个同样浅的 pack.mcmeta，无法确定资源包根目录。");
        bool assetsDirectory = sourceDirectory && metadata is null;
        string root;
        JsonObject? metadataRoot;
        if (metadata is not null) {
            root = GetParent(metadata.Path);
            using Stream input = OpenInput(metadata, sourcePath);
            metadataRoot = PackMetadata.Read(input);
        } else {
            if (assetsDirectory) {
                root = "";
            } else {
                if (assetRoots.Count == 0) throw new InvalidDataException("没有 pack.mcmeta 或 assets 目录，无法确定资源包根目录。");
                int shallowest = assetRoots.Min(Depth);
                string[] roots = [.. assetRoots.Where(path => Depth(path) == shallowest)];
                if (roots.Length != 1) throw new InvalidDataException("发现多个同样浅的 assets 根目录，无法确定资源包根目录。");
                root = roots[0];
            }
            if (sourceFormat is null) throw new InvalidDataException("资源包没有 pack.mcmeta；请显式提供原版本。");
            metadataRoot = assetsDirectory ? null : new JsonObject {
                ["pack"] = new JsonObject {
                    ["description"] = Path.GetFileNameWithoutExtension(sourcePath)
                }
            };
            warning(assetsDirectory
                ? "源被视为纯 assets 内容。"
                : "源包缺少 pack.mcmeta，已使用显式原版本生成元数据。");
        }

        PackFormat? detected = null;
        try {
            detected = metadataRoot is null ? null : PackMetadata.Detect(metadataRoot);
        } catch (InvalidDataException) when (sourceFormat is not null) {
            warning("元数据中的版本无效或范围不唯一；已使用显式原版本。");
        }
        if (sourceFormat is not null && detected is not null && sourceFormat.Value.CompareTo(detected.Value) != 0) {
            warning("显式原版本与元数据不一致；已使用显式原版本。");
        }
        PackFormat effectiveSource = sourceFormat ?? detected ??
            throw new InvalidDataException("无法从元数据确定原版本；请显式提供原版本。");
        if (HasData(metadataRoot?["overlays"])) {
            warning("资源包包含 overlays：相关文件原样保留，不进行现代 overlay 语义转换。");
        }
        byte[] rewrittenMetadata = metadataRoot is null ? [] : PackMetadata.Rewrite(metadataRoot, targetFormat);
        List<OutputFile> outputs = new List<OutputFile>(files.Count);
        HashSet<string> outputFiles = new HashSet<string>(PATH_COMPARER);
        HashSet<string> outputDirectories = new HashSet<string>(PATH_COMPARER);
        int skipped = 0;
        int renamed = 0;
        bool generateMetadata = metadata is null && !assetsDirectory;
        if (generateMetadata) AddOutputPath("pack.mcmeta", outputFiles, outputDirectories);
        foreach (InputFile file in files) {
            if (root.Length != 0 && !file.Path.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase)) {
                skipped++;
                continue;
            }
            string relative = root.Length == 0 ? file.Path : file.Path[(root.Length + 1)..];
            if (assetsDirectory) relative = "assets/" + relative;
            bool isMetadata = ReferenceEquals(file, metadata);
            string outputPath = isMetadata ? "pack.mcmeta" : relative;
            bool convert = !isMetadata && Path.GetExtension(outputPath).Equals(".tga", StringComparison.OrdinalIgnoreCase);
            if (convert) outputPath = Path.ChangeExtension(outputPath, ".png");
            ValidatePath(outputPath);
            AddOutputPath(outputPath, outputFiles, outputDirectories);
            ReadOnlySpan<char> publishedPath = assetsDirectory ? outputPath.AsSpan("assets/".Length) : outputPath.AsSpan();
            if (!file.Path.AsSpan().SequenceEqual(publishedPath)) renamed++;
            outputs.Add(new OutputFile(file, outputPath, isMetadata, convert));
        }
        if (skipped != 0) warning($"已跳过资源包根目录外的 {skipped} 个文件。");

        string parent = Path.GetDirectoryName(destinationPath) ?? throw new IOException("目标路径没有父目录。");
        EnsureNoLinks(parent);
        Directory.CreateDirectory(parent);
        EnsureNoLinks(parent);
        string stagingPath = Path.Combine(parent, ".mrvc-" + Guid.NewGuid().ToString("N") + ".tmp");
        string archivePath = stagingPath + ".zip";
        bool ownsStaging = false;
        bool ownsArchive = false;
        int copied = 0;
        int converted = 0;
        ConversionWorkspace? workspace = null;
        try {
            CreateDirectoryExclusive(stagingPath);
            ownsStaging = true;
            if (generateMetadata) {
                using Stream output = CreateDirectoryFile(stagingPath, "pack.mcmeta");
                output.Write(rewrittenMetadata);
            }
            foreach (OutputFile file in outputs) {
                using Stream output = CreateDirectoryFile(stagingPath, file.Path);
                WriteFile(file, sourcePath, output, rewrittenMetadata);
                if (file.ConvertImage) converted++;
                else copied++;
            }

            workspace = new ConversionWorkspace(stagingPath, effectiveSource, targetFormat, warning);
            if (effectiveSource.CompareTo(targetFormat) != 0) {
                ContentRules.Run(workspace, stage => {
                    if (workspace.Forward) ForwardTextureRules.Apply(stage, workspace);
                    else BackwardTextureRules.Apply(stage, workspace);
                });
            }
            string publishRoot = assetsDirectory ? Path.Combine(stagingPath, "assets") : stagingPath;
            Directory.CreateDirectory(publishRoot);
            using (FileStream? zipStream = destinationZip
                ? new FileStream(archivePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None) : null) {
                ownsArchive = zipStream is not null;
                using ZipArchive? outputArchive = zipStream is null ? null : new ZipArchive(zipStream, ZipArchiveMode.Create, true);
                int finalCount = 0;
                long finalBytes = 0;
                foreach (string path in Directory.EnumerateFiles(publishRoot, "*", SearchOption.AllDirectories)) {
                    EnsureNoLinks(path);
                    string relative = Path.GetRelativePath(publishRoot, path).Replace(Path.DirectorySeparatorChar, '/');
                    ValidatePath(relative);
                    long length = new FileInfo(path).Length;
                    if (++finalCount > MaximumFiles || length > MaximumBytes - finalBytes) {
                        throw new InvalidDataException("转换产物超过 25,000 个文件或 2 GiB。");
                    }
                    finalBytes += length;
                    if (outputArchive is not null) {
                        using Stream input = File.OpenRead(path);
                        using Stream output = CreateArchiveFile(outputArchive, relative);
                        input.CopyTo(output);
                    }
                }
            }
            EnsureNoLinks(parent);
            if (Path.Exists(destinationPath)) throw new IOException("目标路径已存在，不能覆盖。");
            if (destinationZip) {
                File.Move(archivePath, destinationPath, false);
                ownsArchive = false;
            } else {
                Directory.Move(publishRoot, destinationPath);
                if (!assetsDirectory) ownsStaging = false;
            }
        } finally {
            if (ownsArchive) File.Delete(archivePath);
            if (ownsStaging) Directory.Delete(stagingPath, true);
        }
        // Rewritten source metadata counts as copied; generated metadata does not.
        // Renames include removal of a wrapping directory. TGA files count only as converted.
        return new PackMigrationResult(effectiveSource, targetFormat, copied,
            renamed + workspace!.RenamedFiles, converted + workspace.ConvertedImages, skipped);
    }

    static void ScanDirectory(string sourcePath, List<InputFile> files, HashSet<string> assetRoots, ref long bytes) {
        Stack<string> directories = new Stack<string>();
        directories.Push(sourcePath);
        while (directories.TryPop(out string? directory)) {
            EnsureNoLinks(directory);
            foreach (string path in Directory.EnumerateFileSystemEntries(directory)) {
                FileAttributes attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0) throw new InvalidDataException("源目录含符号链接或重解析点。");
                string relative = Path.GetRelativePath(sourcePath, path).Replace(Path.DirectorySeparatorChar, '/');
                ValidatePath(relative);
                bool isDirectory = (attributes & FileAttributes.Directory) != 0;
                FindAssetRoots(relative, isDirectory, assetRoots);
                if (isDirectory) directories.Push(path);
                else {
                    long length = new FileInfo(path).Length;
                    AddInput(new InputFile(relative, path, null, length), files, ref bytes);
                }
            }
        }
    }

    static void ScanArchive(ZipArchive archive, List<InputFile> files, HashSet<string> assetRoots, ref long bytes) {
        foreach (ZipArchiveEntry entry in archive.Entries) {
            int unixType = (entry.ExternalAttributes >> 16) & 0xf000;
            if (unixType == 0xa000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0) {
                throw new InvalidDataException("ZIP 含符号链接或重解析点条目。");
            }
            if (unixType != 0 && unixType != 0x8000 && unixType != 0x4000) {
                throw new InvalidDataException("ZIP 含非普通文件条目。");
            }
            bool directory = entry.FullName.EndsWith('/');
            string path = directory ? entry.FullName[..^1] : entry.FullName;
            ValidatePath(path);
            if ((unixType == 0x4000 || (entry.ExternalAttributes & (int)FileAttributes.Directory) != 0) && !directory) {
                throw new InvalidDataException("ZIP 目录条目的路径表示不一致。");
            }
            FindAssetRoots(path, directory, assetRoots);
            if (!directory) AddInput(new InputFile(path, null, entry, entry.Length), files, ref bytes);
            else if (entry.Length != 0) throw new InvalidDataException("ZIP 目录条目含文件数据。");
        }
    }

    static void AddInput(InputFile file, List<InputFile> files, ref long bytes) {
        if (files.Count >= MaximumFiles) throw new InvalidDataException($"资源包超过 {MaximumFiles} 个文件。");
        if (file.Length < 0 || file.Length > MaximumBytes - bytes) throw new InvalidDataException("资源包未压缩大小超过 2 GiB。");
        bytes += file.Length;
        files.Add(file);
    }

    static void FindAssetRoots(string path, bool directory, HashSet<string> roots) {
        int start = 0;
        while (start < path.Length) {
            int separator = path.IndexOf('/', start);
            int end = separator < 0 ? path.Length : separator;
            if ((separator >= 0 || directory) && path.AsSpan(start, end - start).Equals("assets", StringComparison.OrdinalIgnoreCase)) {
                roots.Add(start == 0 ? "" : path[..(start - 1)]);
            }
            if (separator < 0) break;
            start = separator + 1;
        }
    }

    static void AddOutputPath(string path, HashSet<string> files, HashSet<string> directories) {
        if (!files.Add(path) || directories.Contains(path)) throw new InvalidDataException($"输出路径冲突：{path}");
        int separator = path.IndexOf('/');
        while (separator >= 0) {
            string parent = path[..separator];
            if (files.Contains(parent)) throw new InvalidDataException($"输出文件与目录冲突：{path}");
            directories.Add(parent);
            separator = path.IndexOf('/', separator + 1);
        }
    }

    static void ValidatePath(string path) {
        if (path.Length == 0 || path[0] == '/' || path.Contains('\\')) throw new InvalidDataException($"不安全的包内路径：{path}");
        foreach (string part in path.Split('/')) {
            if (part.Length == 0 || part is "." or ".." || part[^1] is ' ' or '.') {
                throw new InvalidDataException($"不安全的包内路径：{path}");
            }
            foreach (char character in part) {
                if (character < 32 || character is '<' or '>' or ':' or '"' or '|' or '?' or '*') {
                    throw new InvalidDataException($"包内路径含 Windows 非法字符：{path}");
                }
            }
            string name = part.Split('.')[0].TrimEnd(' ');
            if (name.Equals("CON", StringComparison.OrdinalIgnoreCase) || name.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("AUX", StringComparison.OrdinalIgnoreCase) || name.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("CONIN$", StringComparison.OrdinalIgnoreCase) || name.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase) ||
                (name.Length == 4 && (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
                    name.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) && "123456789¹²³".Contains(name[3]))) {
                throw new InvalidDataException($"包内路径使用 Windows 保留名称：{path}");
            }
        }
    }

    static Stream OpenInput(InputFile file, string sourcePath) {
        if (file.ArchiveEntry is not null) return new BoundedReadStream(file.ArchiveEntry.Open(), file.Length);
        EnsureNoLinks(sourcePath);
        EnsureNoLinks(file.DiskPath!);
        FileStream stream = new FileStream(file.DiskPath!, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
        if (stream.Length != file.Length) {
            stream.Dispose();
            throw new IOException("源文件在扫描后发生变化。");
        }
        return new BoundedReadStream(stream, file.Length);
    }

    static void WriteFile(OutputFile file, string sourcePath, Stream output, byte[] metadata) {
        if (file.IsMetadata) {
            output.Write(metadata);
            return;
        }
        using Stream input = OpenInput(file.Input, sourcePath);
        if (file.ConvertImage) {
            using MagickImage image = new MagickImage(input, new MagickReadSettings { Format = MagickFormat.Tga });
            image.Write(output, MagickFormat.Png);
        } else {
            input.CopyTo(output);
        }
    }

    static Stream CreateArchiveFile(ZipArchive archive, string path) =>
        archive.CreateEntry(path, CompressionLevel.Optimal).Open();

    static Stream CreateDirectoryFile(string root, string path) {
        string fullPath = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));
        string parent = Path.GetDirectoryName(fullPath)!;
        EnsureNoLinks(root);
        Directory.CreateDirectory(parent);
        EnsureNoLinks(parent);
        return new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
    }

    static bool HasData(JsonNode? node) => node switch {
        null => false,
        JsonObject value => value.Count != 0,
        JsonArray value => value.Count != 0,
        _ => true
    };

    static string GetName(string path) => path[(path.LastIndexOf('/') + 1)..];
    static string GetParent(string path) => path.LastIndexOf('/') is int separator && separator >= 0 ? path[..separator] : "";
    static int Depth(string path) => path.Length == 0 ? 0 : path.Count(character => character == '/') + 1;

    static bool IsWithin(string path, string directory) =>
        path.StartsWith(Path.EndsInDirectorySeparator(directory) ? directory : directory + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);

    static void EnsureNoLinks(string path) {
        string? current = Path.GetFullPath(path);
        while (current is not null) {
            try {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0) {
                    throw new IOException($"路径含符号链接或重解析点：{current}");
                }
            } catch (FileNotFoundException) {
                // A new destination and its missing parents are allowed.
            } catch (DirectoryNotFoundException) {
            }
            current = Path.GetDirectoryName(current);
        }
    }

    static void CreateDirectoryExclusive(string path) {
        // Directory.CreateDirectory accepts an existing directory. Use the native exclusive
        // operation so cleanup can never claim an already-existing directory as our own.
        bool created = OperatingSystem.IsWindows() ? CreateDirectoryWindows(path, IntPtr.Zero) :
            CreateDirectoryUnix(path, Convert.ToUInt32("700", 8)) == 0;
        if (!created) throw new IOException("无法创建独占的暂存目录。", new Win32Exception(Marshal.GetLastPInvokeError()));
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateDirectoryW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool CreateDirectoryWindows(string path, IntPtr securityAttributes);

    [DllImport("libc", EntryPoint = "mkdir", SetLastError = true)]
    static extern int CreateDirectoryUnix([MarshalAs(UnmanagedType.LPUTF8Str)] string path, uint mode);

    sealed record InputFile(string Path, string? DiskPath, ZipArchiveEntry? ArchiveEntry, long Length);

    sealed record OutputFile(InputFile Input, string Path, bool IsMetadata, bool ConvertImage);

    sealed class BoundedReadStream(Stream input, long expectedLength) : Stream {
        long _position;
        public override bool CanRead => true;
        public override bool CanSeek => input.CanSeek;
        public override bool CanWrite => false;
        public override long Length => expectedLength;
        public override long Position {
            get => _position;
            set => Seek(value, SeekOrigin.Begin);
        }

        public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

        public override int Read(Span<byte> buffer) {
            if (buffer.Length == 0) return 0;
            int count = input.Read(buffer[..(int)Math.Min(buffer.Length, expectedLength - _position + 1)]);
            if (count > expectedLength - _position) throw new InvalidDataException("源文件超过声明的未压缩大小。");
            if (count == 0 && _position < expectedLength) throw new InvalidDataException("源文件在声明的大小之前结束。");
            _position += count;
            return count;
        }

        public override int ReadByte() {
            Span<byte> value = stackalloc byte[1];
            return Read(value) == 0 ? -1 : value[0];
        }

        public override long Seek(long offset, SeekOrigin origin) {
            long target = origin switch {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => checked(_position + offset),
                SeekOrigin.End => checked(expectedLength + offset),
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };
            if (target < 0 || target > expectedLength) throw new IOException("试图定位到源文件范围之外。");
            _position = input.Seek(target, SeekOrigin.Begin);
            return _position;
        }

        public override void Flush() { }
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        protected override void Dispose(bool disposing) {
            if (disposing) input.Dispose();
            base.Dispose(disposing);
        }
    }
}
