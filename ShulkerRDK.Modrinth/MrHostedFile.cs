using System.Text.Json.Serialization;
using Modrinth.Models.Enums;

namespace ShulkerRDK.Modrinth;

public class MrHostedFile {
    public required string VersionId { get; set; }
    public required string Sha1 { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Side? ServerSide { get; set; }
    
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Side? ClientSide { get; set; }
}