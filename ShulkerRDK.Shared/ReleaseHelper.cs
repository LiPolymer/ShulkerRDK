using System.Text.Json;

namespace ShulkerRDK.Shared;

public record ReleaseAssetInfo(string Name,string Url,string? Sha256);
public record ReleaseInfo(string Tag,IReadOnlyList<ReleaseAssetInfo> Assets);

public abstract class ReleaseSource {
    public abstract ReleaseInfo? FetchRelease(string repo,string tag);

    public static ReleaseSource Resolve(string platform) {
        return platform switch {
            "gh" => GitHubReleaseSource.Instance,
            "gl" => GitLabReleaseSource.Instance,
            _ => throw new NotSupportedException($"未知的扩展源平台[{platform}]")
        };
    }
}

public static class ReleaseHttp {
    public static readonly HttpClient Client = new() {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static ReleaseHttp() {
        Client.DefaultRequestHeaders.UserAgent.ParseAdd("ShulkerRDK");
        Client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public static JsonDocument GetJson(string url) {
        using HttpResponseMessage response = Client.GetAsync(url).GetAwaiter().GetResult();
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult());
    }
}

public class GitHubReleaseSource : ReleaseSource {
    public static readonly GitHubReleaseSource Instance = new();

    public override ReleaseInfo? FetchRelease(string repo,string tag) {
        try {
            using JsonDocument doc = ReleaseHttp.GetJson($"https://api.github.com/repos/{repo}/releases/tags/{Uri.EscapeDataString(tag)}");
            JsonElement root = doc.RootElement;
            string releaseTag = root.GetProperty("tag_name").GetString() ?? tag;
            List<ReleaseAssetInfo> assets = [];
            foreach (JsonElement asset in root.GetProperty("assets").EnumerateArray()) {
                string name = asset.GetProperty("name").GetString() ?? "";
                string url = asset.GetProperty("browser_download_url").GetString() ?? "";
                string? digest = asset.TryGetProperty("digest",out JsonElement d) ? d.GetString() : null;
                if (digest is { Length: > 7 } && digest.StartsWith("sha256:")) {
                    digest = digest[7..];
                } else {
                    digest = null;
                }
                assets.Add(new ReleaseAssetInfo(name,url,digest));
            }
            return new ReleaseInfo(releaseTag,assets);
        }
        catch {
            return null;
        }
    }
}

public class GitLabReleaseSource : ReleaseSource {
    public static readonly GitLabReleaseSource Instance = new();

    public override ReleaseInfo? FetchRelease(string repo,string tag) {
        try {
            string project = Uri.EscapeDataString(repo);
            using JsonDocument doc = ReleaseHttp.GetJson($"https://gitlab.com/api/v4/projects/{project}/releases/{Uri.EscapeDataString(tag)}");
            JsonElement root = doc.RootElement;
            string releaseTag = root.GetProperty("tag_name").GetString() ?? tag;
            List<ReleaseAssetInfo> assets = [];
            foreach (JsonElement link in root.GetProperty("assets").GetProperty("links").EnumerateArray()) {
                string name = link.GetProperty("name").GetString() ?? "";
                string url = link.TryGetProperty("direct_asset_url",out JsonElement dau) &&
                             dau.ValueKind == JsonValueKind.String && dau.GetString() is { Length: > 0 }
                             ? dau.GetString()!
                             : link.GetProperty("url").GetString() ?? "";
                assets.Add(new ReleaseAssetInfo(name,url,null));
            }
            return new ReleaseInfo(releaseTag,assets);
        }
        catch {
            return null;
        }
    }
}
