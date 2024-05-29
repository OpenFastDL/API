using System.Net;
using System.Text.Json.Serialization;

namespace OpenFastDL.Api;

public sealed record ErrorResponseDTO(
    [property: JsonPropertyName("code")] HttpStatusCode StatusCode,
    [property: JsonPropertyName("message")] string Message)
{
    public static ErrorResponseDTO Conflict(string message)
        => new(HttpStatusCode.Conflict, message);

    public static ErrorResponseDTO BadRequest(string message)
        => new(HttpStatusCode.BadRequest, message);
}