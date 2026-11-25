using TridentCore.Abstractions.Utilities;
using TridentCore.Core.Models.PrismLauncherApi;
using TridentCore.Core.Services;

namespace ShulkerRDK.Prismarine.Services;

public static class LoaderResolver {
    static readonly Dictionary<string, string> IdentityAliases = new(StringComparer.OrdinalIgnoreCase) {
        ["fabric"] = LoaderHelper.LOADERID_FABRIC,
        [LoaderHelper.LOADERID_FABRIC] = LoaderHelper.LOADERID_FABRIC,
        ["neoforge"] = LoaderHelper.LOADERID_NEOFORGE,
        [LoaderHelper.LOADERID_NEOFORGE] = LoaderHelper.LOADERID_NEOFORGE,
        ["forge"] = LoaderHelper.LOADERID_FORGE,
        [LoaderHelper.LOADERID_FORGE] = LoaderHelper.LOADERID_FORGE,
        ["quilt"] = LoaderHelper.LOADERID_QUILT,
        [LoaderHelper.LOADERID_QUILT] = LoaderHelper.LOADERID_QUILT,
        ["flint"] = LoaderHelper.LOADERID_FLINT,
        [LoaderHelper.LOADERID_FLINT] = LoaderHelper.LOADERID_FLINT
    };

    public static async Task<string?> ResolveToLurlAsync(
        string? loaderInput,
        string mcVersion,
        CancellationToken ct = default) {
        if (IsVanillaOrEmpty(loaderInput))
            return null;

        string trimmed = loaderInput!.Trim();
        int colonIndex = trimmed.IndexOf(':');
        if (colonIndex >= 0) {
            string rawId = trimmed[..colonIndex].Trim();
            string version = trimmed[(colonIndex + 1)..].Trim();

            if (!IdentityAliases.TryGetValue(rawId, out string? normalizedId))
                throw new ArgumentException($"Unknown loader '{rawId}'. Supported loaders: fabric, neoforge, forge, quilt, vanilla.", nameof(loaderInput));

            if (string.IsNullOrEmpty(version))
                throw new ArgumentException($"Loader '{loaderInput}' is missing a version after ':'.", nameof(loaderInput));

            string lurl = LoaderHelper.ToLurl(normalizedId, version);
            if (!LoaderHelper.TryParse(lurl, out _))
                throw new ArgumentException($"Invalid loader version '{version}' in '{loaderInput}'.", nameof(loaderInput));

            return lurl;
        }

        if (!IdentityAliases.TryGetValue(trimmed, out string? identity))
            throw new ArgumentException($"Unknown loader '{trimmed}'. Supported loaders: fabric, neoforge, forge, quilt, vanilla.", nameof(loaderInput));

        if (!PrismLauncherService.UidMappings.TryGetValue(identity, out string? uid))
            throw new InvalidOperationException($"Automatic version resolution is not supported for loader '{identity}'. Specify an explicit version like '{identity}:<version>'.");

        IReadOnlyList<ComponentIndex.ComponentVersion> versions = await TridentServices.PrismLauncherService
            .GetVersionsForMinecraftVersionAsync(uid, mcVersion, ct)
            .ConfigureAwait(false);

        ComponentIndex.ComponentVersion? chosen =
            versions.FirstOrDefault(v => v.Recommended)
            ?? versions.OrderByDescending(v => v.ReleaseTime).FirstOrDefault();

        if (chosen is null)
            throw new InvalidOperationException($"No {LoaderHelper.ToDisplayName(identity)} version found for Minecraft {mcVersion}.");

        return LoaderHelper.ToLurl(identity, chosen.Version);
    }

    /// <summary>
    /// Normalizes a loader alias, identity, or LURL into a Trident <see cref="LoaderHelper"/> identity
    /// (e.g. <c>net.fabricmc</c>, <c>net.neoforged</c>, <c>net.minecraftforge</c>, <c>org.quiltmc</c>),
    /// which is what Trident's <c>Filter.Loader</c> expects for both Modrinth and CurseForge repositories.
    /// </summary>
    public static string? ToFilterLoader(string? loaderOrLurl) {
        if (IsVanillaOrEmpty(loaderOrLurl))
            return null;

        string trimmed = loaderOrLurl!.Trim();
        int colonIndex = trimmed.IndexOf(':');
        string rawId = colonIndex >= 0 ? trimmed[..colonIndex].Trim() : trimmed;

        if (IdentityAliases.TryGetValue(rawId, out string? identity))
            return identity;

        return LoaderHelper.TryParse(trimmed, out (string Identity, string Version) parsed)
            ? parsed.Identity
            : null;
    }

    public static string FormatDisplay(string? lurl) {
        if (IsVanillaOrEmpty(lurl))
            return "Vanilla";

        string trimmed = lurl!.Trim();
        if (LoaderHelper.TryParse(trimmed, out (string Identity, string Version) parsed))
            return LoaderHelper.ToDisplayLabel(parsed.Identity, parsed.Version);

        if (IdentityAliases.TryGetValue(trimmed, out string? identity))
            return LoaderHelper.ToDisplayName(identity);

        return trimmed;
    }

    static bool IsVanillaOrEmpty(string? value) {
        if (string.IsNullOrWhiteSpace(value))
            return true;
        string trimmed = value.Trim();
        return trimmed.Equals("vanilla", StringComparison.OrdinalIgnoreCase)
               || trimmed.Equals("none", StringComparison.OrdinalIgnoreCase);
    }
}
