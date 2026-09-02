namespace ShulkerRDK.Shared;

public record ExtensionSource(string Platform,string Repo,string Asm,string Tag) {
    public static readonly Dictionary<string,string> PlatformHosts = new() {
        ["gh"] = "github.com",
        ["gl"] = "gitlab.com"
    };

    public string Identifier => $"{Platform}:{Repo}#{Asm}@{Tag}";

    public static ExtensionSource Parse(string identifier) {
        string[] platformSplit = identifier.Split(':',2);
        if (platformSplit.Length != 2 || platformSplit[0].Length == 0 || platformSplit[1].Length == 0) {
            throw new FormatException($"扩展标识符缺少源平台前缀&8[&7{identifier}&8]");
        }
        string platform = platformSplit[0].ToLowerInvariant();
        if (!PlatformHosts.ContainsKey(platform)) {
            throw new FormatException($"未知的扩展源平台&8[&7{platformSplit[0]}&8],可用&8[&7{string.Join('&',PlatformHosts.Keys)}&8]");
        }
        int asmSplit = platformSplit[1].IndexOf('#');
        if (asmSplit < 0) {
            throw new FormatException($"扩展标识符缺少程序集段&8[&7{identifier}&8]");
        }
        string repo = platformSplit[1][..asmSplit];
        string asmTag = platformSplit[1][(asmSplit + 1)..];
        int tagSplit = asmTag.IndexOf('@');
        if (tagSplit < 0) {
            throw new FormatException($"扩展标识符缺少版本钉&8[&7{identifier}&8]");
        }
        string asm = asmTag[..tagSplit];
        string tag = asmTag[(tagSplit + 1)..];
        if (repo.Length == 0 || asm.Length == 0 || tag.Length == 0) {
            throw new FormatException($"扩展标识符格式无效&8[&7{identifier}&8]");
        }
        return new ExtensionSource(platform,repo,asm,tag);
    }
}
