// Native port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class NameRules
{
    private const string Root = ContentJson.Minecraft;
    private static readonly HashSet<string> SharedDyes = new(StringComparer.Ordinal)
        { "ink_sac", "lapis_lazuli", "cocoa_beans", "bone_meal" };

    internal static void Run(ConversionWorkspace w)
    {
        if (w.Crosses(393))
        {
            if (w.Forward)
            {
                NormalizePaths(w, false);
                MoveIfPresent(w, Root + "mcpatcher", Root + "optifine");
                foreach (var folder in new[] { "models", "textures" })
                {
                    MoveIfPresent(w, Root + folder + "/blocks", Root + folder + "/block");
                    MoveIfPresent(w, Root + folder + "/items", Root + folder + "/item");
                }
            }
            else MoveIfPresent(w, Root + "optifine", Root + "mcpatcher");
        }

        foreach (var (protocol, version) in ContentJson.NameVersions(w.Forward))
        {
            if (!ContentJson.Applies(w, protocol)) continue;
            foreach (var table in new[] { "blocks", "items" })
            {
                var kind = table == "blocks" ? "block" : "item";
                RenameDirectory(w, Root + "models/" + kind, ".json", table, version, protocol);
                foreach (var suffix in new[] { ".png", ".png.mcmeta" })
                    RenameDirectory(w, Root + "textures/" + kind, suffix, table, version, protocol);
            }
            RenameDirectory(w, Root + "blockstates", ".json", "blocks", version, protocol);
        }

        if (w.Crosses(393))
        {
            var entity = Root + "textures/entity/";
            MoveIfPresent(w, entity + (w.Forward ? "endercrystal" : "end_crystal"),
                entity + (w.Forward ? "end_crystal" : "endercrystal"));
            var mapping = RuleData.Json((w.Forward ? "forwards/" : "backwards/") + "entities.json")["*"]!.AsObject();
            foreach (var directory in w.Files(entity).Select(p => p[..p.LastIndexOf('/')]).Distinct().ToArray())
                foreach (var suffix in new[] { ".png", ".png.mcmeta" })
                    RenameFiles(w, directory, suffix, name => ContentJson.Text(mapping[name]) ?? name);
            if (!w.Forward)
            {
                foreach (var kind in new[] { "block", "item" })
                    MoveIfPresent(w, Root + "textures/" + kind, Root + "textures/" + kind + "s");
                w.Delete(Root + "models/item/banner.json");
            }
        }
        // The upstream forward boundary is 1.15, not the misleading '1.16' comment.
        if ((w.Forward && ContentJson.Applies(w, 573)) || (!w.Forward && w.Crosses(735)))
            MoveEntity(w, "iron_golem");
        if (w.Crosses(755)) MoveEntity(w, "squid");
    }

    internal static void RemoveSpaces(ConversionWorkspace w)
    {
        if (!w.Crosses(315)) return;
        RenameTree(w, Root.TrimEnd('/'), name => name.Replace(' ', '_'));
    }

    private static void NormalizePaths(ConversionWorkspace w, bool spaces) =>
        RenameTree(w, Root.TrimEnd('/'), name => spaces ? name.Replace(' ', '_') : ContentJson.Clean(name));

    private static void RenameTree(ConversionWorkspace w, string directory, Func<string, string> rename)
    {
        if (!Directory.Exists(w.PathFor(directory))) return;
        foreach (var full in Directory.GetFileSystemEntries(w.PathFor(directory)))
        {
            var oldName = Path.GetFileName(full);
            var newName = rename(oldName);
            var oldPath = directory + "/" + oldName;
            var newPath = directory + "/" + newName;
            if (!string.Equals(oldName, newName, StringComparison.Ordinal))
            {
                // Windows needs an intermediate name for a case-only rename.
                if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
                {
                    var temporary = directory + "/.rpc-name-" + Guid.NewGuid().ToString("N");
                    w.Move(oldPath, temporary);
                    w.Move(temporary, newPath);
                }
                else w.Move(oldPath, newPath);
            }
            if (Directory.Exists(w.PathFor(newPath))) RenameTree(w, newPath, rename);
        }
    }

    private static void MoveEntity(ConversionWorkspace w, string name)
    {
        foreach (var suffix in new[] { ".png", ".png.mcmeta" })
        {
            var flat = Root + "textures/entity/" + name + suffix;
            var nested = Root + "textures/entity/" + name + "/" + name + suffix;
            MoveIfPresent(w, w.Forward ? flat : nested, w.Forward ? nested : flat);
        }
    }

    internal static void MoveIfPresent(ConversionWorkspace w, string source, string destination)
    {
        if (w.Exists(source)) w.Move(source, destination);
    }

    private static void RenameDirectory(ConversionWorkspace w, string directory, string suffix,
        string table, string version, int protocol)
    {
        RenameFiles(w, directory, suffix, name =>
        {
            var mapped = MapName(w, table, version, name, directory + "/" + name + suffix);
            if (protocol == 393 && table == "blocks")
            {
                if (suffix == ".json")
                {
                    if (w.Forward && name == "snow_layer") return "snow";
                    if (w.Forward && name == "snow") return "snow_block";
                }
                else if (!w.Forward && (name == "snow" || name == "snow_layer")) return name;
            }
            return mapped;
        }, w.Forward && protocol == 477 && table == "items" ? SharedDyes : null);
    }

    internal static string MapName(ConversionWorkspace w, string table, string version, string name, string file)
    {
        var mapped = ContentJson.Map(w.Forward, table, version, name);
        if (w.Forward && version == "1_14" && table == "items" && SharedDyes.Contains(name) && w.AmbiguousSourceAt(477))
            throw new InvalidDataException($"Pack format 4 covers both Minecraft 1.13 and 1.14; '{file}' uses '{name}', whose dye meaning cannot be inferred safely. Supply a pack with an unambiguous source format or resolve this legacy dye resource explicitly.");
        return mapped;
    }

    private static void RenameFiles(ConversionWorkspace w, string directory, string suffix,
        Func<string, string> map, HashSet<string>? copyNames = null)
    {
        var changes = w.Files(directory, "*" + suffix, false)
            .Select(source => (Source: source, Name: Path.GetFileName(source)[..^suffix.Length]))
            .Select(x => (x.Source, x.Name, Destination: directory + "/" + map(x.Name) + suffix))
            .Where(x => !string.Equals(x.Source, x.Destination, StringComparison.Ordinal)).ToArray();
        if (changes.Length == 0) return;
        var sources = changes.Select(x => x.Source).ToHashSet(StringComparer.Ordinal);
        var destinations = new HashSet<string>(StringComparer.Ordinal);
        foreach (var change in changes)
        {
            if (!destinations.Add(change.Destination) || (w.Exists(change.Destination) && !sources.Contains(change.Destination)))
                throw new InvalidDataException($"Renaming '{change.Source}' would overwrite '{change.Destination}'. The pack contains conflicting resource names.");
        }
        var staging = ".rpc-names-" + Guid.NewGuid().ToString("N");
        for (var i = 0; i < changes.Length; i++)
        {
            if (copyNames?.Contains(changes[i].Name) == true) w.Copy(changes[i].Source, staging + "/" + i);
            else w.Move(changes[i].Source, staging + "/" + i);
        }
        for (var i = 0; i < changes.Length; i++) w.Move(staging + "/" + i, changes[i].Destination);
        w.Delete(staging);
    }
}
