using System.ComponentModel.DataAnnotations.Schema;

namespace OpenFastDL.Api;

[Table("files")]
public sealed record RemoteFile([property: Column("id")] string Id, [property: Column("size")] long Size, [property: Column("path")] string RelativePath);