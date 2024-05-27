using Elefess;
using Elefess.Models;
using Microsoft.EntityFrameworkCore;

namespace OpenFastDL.Api;

public sealed class PostgresLfsOidMapper : ILfsOidMapper
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly ILogger _logger;

    public PostgresLfsOidMapper(IServiceProvider services, IConfiguration configuration, ILogger<PostgresLfsOidMapper> logger)
    {
        _services = services;
        _configuration = configuration;
        _logger = logger;
    }

    public Dictionary<Guid, RemoteFile> UploadValidationCodes { get; } = new();

    public async Task<LfsResponseObject> MapObjectAsync(string oid, long size, LfsOperation operation, CancellationToken cancellationToken)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

        var file = await db.Files.FirstOrDefaultAsync(x => x.Id == oid, cancellationToken);
        if (file is null)
            return LfsResponseObject.FromError(LfsObjectError.NotFound($"OID lookup failed for {oid}"));

        switch (operation)
        {
            case LfsOperation.Upload:
            {
                // To enable support for overwriting files, uncomment the following:
                //if (File.Exists(Path.Combine(_configuration["BaseFilePath"]!, location.RelativePath)))
                //    return LfsResponseObject.FromError(LfsObjectError.Conflict($"OID {oid} already has a file uploaded"));
                
                if (file.Size != size)
                    return LfsResponseObject.FromError(LfsObjectError.UnprocessableEntity($"OID {oid} size mismatch! TX: {size}, RX: {file.Size}"));
                
                
                var code = Guid.NewGuid();
                UploadValidationCodes[code] = file;
                
                var remote = _configuration.GetSection("Remote");
                return LfsResponseObject.BasicUpload(new($"{remote["Upload"]}/uploads/lfs/{code}"));
            }
            case LfsOperation.Download:
            {
                var localPath = Path.Combine(_configuration["BaseFilePath"]!, file.RelativePath);
                
                long actualSize;
                try
                {
                    actualSize = new FileInfo(localPath).Length;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get file info for OID {Oid}", oid);
                    return LfsResponseObject.FromError(LfsObjectError.UnprocessableEntity($"Failed to get file info for OID {oid}'s local file"));
                }
                
                if (actualSize != size)
                    return LfsResponseObject.FromError(LfsObjectError.UnprocessableEntity($"OID {oid} size mismatch! TX: {size}, RX: {actualSize}"));
                
                var remote = _configuration.GetSection("Remote");
                return LfsResponseObject.BasicDownload(new($"{remote["Download"]}/{file.RelativePath}"));
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(operation), operation, null);
        }
    }
}