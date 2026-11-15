// Port of ResourcePackConverter (MIT, agentdid127 and Hypixel).
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json.Nodes;

namespace ShulkerRDK.ResourceMagick.VersionConverter;

internal static class SliceRules {
    private const string ButtonMetadata = """{"gui":{"scaling":{"type":"nine_slice","width":200,"height":20,"border":{"left":20,"top":4,"right":20,"bottom":4}}}}""";

    public static void Run(ConversionWorkspace workspace, string manifest, string root, bool metadata = false,
        int? width = null, int? height = null, bool ignorePredicates = false) {
        JsonNode rule = RuleData.Json("forwards/" + manifest);
        if (rule is JsonArray array) {
            foreach (JsonNode? entry in array)
                RunSlice(workspace, entry!.AsObject(), root, metadata, width, height, ignorePredicates);
        } else RunSlice(workspace, rule.AsObject(), root, metadata, width, height, ignorePredicates);
    }

    private static void RunSlice(ConversionWorkspace workspace, JsonObject slice, string root, bool metadata,
        int? width, int? height, bool ignorePredicates) {
        string source = Join(root, String(slice, "path"));
        if (!workspace.Exists(source)) return;
        if (!Matches(workspace, slice["predicate"], source, ignorePredicates)) return;
        string imagePath = slice["name"] is JsonValue name
            ? Join(Parent(source), name.GetValue<string>()) : source;
        using var image = new Raster(width ?? Int(slice, "width"), height ?? Int(slice, "height"), workspace.PathFor(source));
        // Stage all crops before replacing any output or deleting the input. Invalid crops
        // are errors, not an excuse to publish a partially sliced pack.
        string scratch = Path.Combine(workspace.Root, ".rpc-slices-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(scratch);
        var outputs = new List<(string Staged, string Destination, JsonNode? Metadata)>();
        try {
            foreach (JsonNode? entry in slice["textures"]!.AsArray()) {
                JsonObject texture = entry!.AsObject();
                if (!Matches(workspace, texture["predicate"], source, ignorePredicates)) continue;
                string destination = Join(root, String(texture, "path"));
                // Validate output containment before doing any expensive image work.
                workspace.PathFor(destination);
                JsonObject box = texture["box"]!.AsObject();
                int x = Int(box, "x"), y = Int(box, "y"), w = Int(box, "width"), h = Int(box, "height");
                string staged = Path.Combine(scratch, outputs.Count + ".png");
                image.SaveSlice(x, y, w, h, staged);
                if (texture["remove"]?.GetValue<bool>() == true) image.FillEmpty(x, y, w, h);
                JsonNode? textureMetadata = metadata ? Metadata(texture["metadata"]) : null;
                outputs.Add((staged, destination, textureMetadata));
            }
            bool delete = slice["delete"]?.GetValue<bool>() ?? true;
            string? retained = null;
            if (!delete) {
                workspace.PathFor(imagePath);
                retained = Path.Combine(scratch, "retained.png");
                image.Store(retained);
            }
            foreach (var output in outputs) {
                workspace.EnsureParent(output.Destination);
                File.Move(output.Staged, workspace.PathFor(output.Destination), true);
                workspace.RecordImage();
                if (output.Metadata != null) workspace.WriteJson(output.Destination + ".mcmeta", output.Metadata);
            }
            if (delete || !string.Equals(source, imagePath, StringComparison.Ordinal)) workspace.Delete(source);
            if (retained != null) {
                workspace.EnsureParent(imagePath);
                File.Move(retained, workspace.PathFor(imagePath), true);
                workspace.RecordImage();
            }
        } finally { Directory.Delete(scratch, true); }
    }

    private static bool Matches(ConversionWorkspace workspace, JsonNode? predicate, string source, bool ignore) {
        if (ignore || predicate?["protocol"] is not JsonObject protocol) return true;
        int min = protocol["min_inclusive"]?.GetValue<int>() ?? 4;
        int max = protocol["max_inclusive"]?.GetValue<int>() ?? int.MaxValue;
        bool matches = workspace.FromProtocol >= min && workspace.FromProtocol <= max;
        // Endpoints alone are insufficient when a predicate range sits wholly inside
        // an aliased pack-format bucket.
        bool any = workspace.MinimumSourceProtocol <= max && workspace.FromProtocol >= min;
        bool all = workspace.MinimumSourceProtocol >= min && workspace.FromProtocol <= max;
        if (any && !all)
            throw new InvalidDataException($"Cannot select the historical slice layout for '{source}': source pack format spans protocol {workspace.MinimumSourceProtocol}–{workspace.FromProtocol}, but the manifest requires {min}–{max}.");
        return matches;
    }

    private static JsonNode? Metadata(JsonNode? metadata) {
        if (metadata is JsonObject obj) return obj.Count == 0 ? null : obj.DeepClone();
        if (metadata is JsonValue value && value.GetValue<string>() == "button_widget") return JsonNode.Parse(ButtonMetadata)!;
        return null;
    }
    private static int Int(JsonObject obj, string key) => obj[key]!.GetValue<int>();
    private static string String(JsonObject obj, string key) => obj[key]!.GetValue<string>();
    private static string Join(string root, string child) => root.Length == 0 ? child : root + "/" + child;
    private static string Parent(string path) => path[..path.LastIndexOf('/')];
}
