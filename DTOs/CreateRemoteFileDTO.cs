using System.Text.Json.Serialization;

namespace OpenFastDL.Api;

public sealed class CreateRemoteFileDTO(long size, string relativePath)
{
    [JsonPropertyName("size")]
    public long Size { get; } = size;

    [JsonPropertyName("path")]
    public string RelativePath { get; } = relativePath;
}