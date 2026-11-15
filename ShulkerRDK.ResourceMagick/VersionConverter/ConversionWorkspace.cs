using System.Text.Json;
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

enum TextureStage {
    Compass, Offhand, MapIcons, Water, Particles, Paintings, MobEffects, Chests,
    Glint, ConduitParticles, Inventory, CreativeTabs, EnchantPath, WidgetSliders,
    Title, GuiSprites, ImageFormats, MapSprites, Celestial
}

sealed class ConversionWorkspace {
    static readonly JsonSerializerOptions JSON_OPTIONS = new() { WriteIndented = true };
    readonly Action<string> warning;

    public string Root { get; }
    public PackFormat SourceFormat { get; }
    public PackFormat TargetFormat { get; }
    public int FromProtocol { get; }
    public int ToProtocol { get; }
    public int MinimumSourceProtocol { get; }
    public bool Forward => SourceFormat.CompareTo(TargetFormat) < 0;
    public int RenamedFiles { get; private set; }
    public int ConvertedImages { get; private set; }
    public int ModifiedFiles { get; private set; }

    public ConversionWorkspace(string root, PackFormat source, PackFormat target, Action<string> warning) {
        Root = Path.GetFullPath(root);
        SourceFormat = source;
        TargetFormat = target;
        this.warning = warning;
        (MinimumSourceProtocol, FromProtocol) = VersionRules.Protocols(source);
        (_, ToProtocol) = VersionRules.Protocols(target);
    }

    public bool Crosses(int boundaryProtocol) => Forward
        ? FromProtocol < boundaryProtocol && ToProtocol >= boundaryProtocol
        : FromProtocol >= boundaryProtocol && ToProtocol < boundaryProtocol;

    public bool AmbiguousSourceAt(int boundaryProtocol) => MinimumSourceProtocol < boundaryProtocol && FromProtocol >= boundaryProtocol;

    public string PathFor(string relative) {
        if (Path.IsPathRooted(relative)) throw new InvalidDataException($"规则包含绝对路径：{relative}");
        string full = Path.GetFullPath(Path.Combine(Root, relative.Replace('/', Path.DirectorySeparatorChar)));
        if (!full.Equals(Root, StringComparison.OrdinalIgnoreCase)
            && !full.StartsWith(Path.TrimEndingDirectorySeparator(Root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidDataException($"规则路径越出暂存目录：{relative}");
        }
        return full;
    }

    public bool Exists(string relative) => Path.Exists(PathFor(relative));

    public IEnumerable<string> Files(string relativeDirectory, string pattern = "*", bool recursive = true) {
        string directory = PathFor(relativeDirectory);
        if (!Directory.Exists(directory)) yield break;
        foreach (string file in Directory.EnumerateFiles(directory, pattern,
                     recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly)) {
            yield return Path.GetRelativePath(Root, file).Replace(Path.DirectorySeparatorChar, '/');
        }
    }

    public JsonNode ReadJson(string relative) {
        try {
            using FileStream stream = File.OpenRead(PathFor(relative));
            return JsonNode.Parse(stream, documentOptions: new JsonDocumentOptions {
                CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true
            }) ?? throw new InvalidDataException($"JSON 内容为空：{relative}");
        } catch (JsonException error) {
            throw new InvalidDataException($"资源 JSON 无效：{relative}: {error.Message}", error);
        }
    }

    public void WriteJson(string relative, JsonNode data) {
        EnsureParent(relative);
        File.WriteAllBytes(PathFor(relative), JsonSerializer.SerializeToUtf8Bytes(data, JSON_OPTIONS));
        ModifiedFiles++;
    }

    public void EnsureParent(string relative) => Directory.CreateDirectory(Path.GetDirectoryName(PathFor(relative))!);

    public void Move(string source, string destination, bool overwrite = false) {
        if (source.Equals(destination, StringComparison.Ordinal)) return;
        string input = PathFor(source);
        string output = PathFor(destination);
        if (!Path.Exists(input)) return;
        EnsureParent(destination);
        if (input.Equals(output, StringComparison.OrdinalIgnoreCase)) {
            string intermediate = input + ".mrvc-case-" + Guid.NewGuid().ToString("N");
            if (Directory.Exists(input)) {
                Directory.Move(input, intermediate);
                Directory.Move(intermediate, output);
            } else {
                File.Move(input, intermediate);
                File.Move(intermediate, output);
            }
            RenamedFiles++;
            return;
        }
        if (Directory.Exists(input)) {
            if (File.Exists(output)) throw new InvalidDataException($"规则文件/目录冲突：{source} -> {destination}");
            if (!Directory.Exists(output)) {
                Directory.Move(input, output);
                RenamedFiles++;
                return;
            }
            foreach (string child in Directory.GetFileSystemEntries(input)) {
                string name = Path.GetFileName(child);
                Move(source.TrimEnd('/') + "/" + name, destination.TrimEnd('/') + "/" + name, overwrite);
            }
            Directory.Delete(input);
        } else {
            if (Directory.Exists(output)) throw new InvalidDataException($"规则文件/目录冲突：{source} -> {destination}");
            if (!overwrite && File.Exists(output)) throw new InvalidDataException($"规则输出冲突：{source} -> {destination}");
            File.Move(input, output, overwrite);
            RenamedFiles++;
        }
    }

    public void Copy(string source, string destination, bool overwrite = false) {
        EnsureParent(destination);
        File.Copy(PathFor(source), PathFor(destination), overwrite);
        ModifiedFiles++;
    }

    public void Delete(string relative) {
        string path = PathFor(relative);
        if (File.Exists(path)) { File.Delete(path); ModifiedFiles++; }
        else if (Directory.Exists(path)) { Directory.Delete(path, true); ModifiedFiles++; }
    }

    public void Warn(string message) => warning(message);
    public void RecordImage() => ConvertedImages++;
    public void RecordModified() => ModifiedFiles++;
}
