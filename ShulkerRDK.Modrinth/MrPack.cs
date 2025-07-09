using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ShulkerRDK.Shared;

namespace ShulkerRDK.Modrinth;

public class MrPack {
    [JsonPropertyName("game")]
    public string Game = "minecraft";
    [JsonPropertyName("formatVersion")]
    public int FormatVersion = 1;
    [JsonPropertyName("versionId")]
    public required string VersionId;
    [JsonPropertyName("name")]
    public required string Name;
    [JsonPropertyName("summary")]
    public string Description = string.Empty;
    [JsonPropertyName("files")]
    public List<FileObject> Files = [];
    [JsonPropertyName("dependencies")]
    public required Dictionary<string, string> Dependencies;

    public class FileObject {
        [JsonPropertyName("path")]
        public required string Path;
        [JsonPropertyName("hashes")]
        public required HashesTable Hashes;
        [JsonPropertyName("env")]
        public required EnvTable Envs;
        [JsonPropertyName("downloads")]
        public required List<string> Downloads;
        [JsonPropertyName("fileSize")]
        public required int FileSize;
        
        public class HashesTable {
            [JsonPropertyName("sha512")]
            public required string Sha512;
            [JsonPropertyName("sha1")]
            public required string Sha1;
        }
        
        public class EnvTable {
            [JsonPropertyName("server")]
            public required string Server;
            [JsonPropertyName("client")]
            public required string Client;
        }
    }

    public static MrPack Load(string path) {
        return JsonSerializer.Deserialize<MrPack>(File.ReadAllText(path),_options)!;
    }
    
    static JsonSerializerOptions _options = new JsonSerializerOptions {
        IncludeFields = true, 
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    public void Export(string path) {
        Tools.WriteAllText(path,JsonSerializer.Serialize(this,_options));
    }
}