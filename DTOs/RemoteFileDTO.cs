using System.Text.Json.Serialization;

namespace OpenFastDL.Api;

public sealed class RemoteFileDTO(RemoteFile file)
{
    [JsonPropertyName("oid")] 
    public string Hash { get; } = file.Id;
    
    [JsonPropertyName("size")]
    public long Size { get; } = file.Size;
    
    [JsonPropertyName("path")]
    public string RelativePath { get; } = file.RelativePath;
}