using System.Buffers.Binary;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace OpenFastDL.Api;

public static class EndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapOidEndpoints(this IEndpointRouteBuilder builder, string route = "/oids/{oid}")
    {
        builder.MapGet(route, GetOidAsync)
            .AddEndpointFilter<ApiKeyEndpointFilter>();
        
        builder.MapPut(route, PutOidAsync)
            .AddEndpointFilter<ApiKeyEndpointFilter>();
        
        builder.MapDelete(route, DeleteOidAsync)
            .AddEndpointFilter<ApiKeyEndpointFilter>();

        return builder;

        static async Task<IResult> GetOidAsync(HttpContext context,
            [FromServices] DatabaseContext db,
            string oid)
        {
            return await db.Files.FirstOrDefaultAsync(x => x.Id == oid) is { } file
                ? Results.Ok(new RemoteFileDTO(file))
                : Results.NotFound();
        }
        
        static async Task<IResult> PutOidAsync(HttpContext context,
            [FromServices] DatabaseContext db,
            string oid,
            [FromBody] CreateRemoteFileDTO file)
        {
            if (await db.Files.AnyAsync(x => x.Id == oid))
                return Results.Conflict();
            
            db.Files.Add(new RemoteFile(oid, file.Size, file.RelativePath));
            await db.SaveChangesAsync();
            return Results.NoContent();
        }

        static async Task<IResult> DeleteOidAsync(HttpContext context,
            [FromServices] DatabaseContext db,
            string oid)
        {
            if (await db.Files.FirstOrDefaultAsync(x => x.Id == oid) is not { } file)
                return Results.NotFound();

            db.Files.Remove(file);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }
    }
    
    public static IEndpointRouteBuilder MapUploadEndpoints(this IEndpointRouteBuilder builder, string route = "/uploads/lfs/{code}")
    {
        builder.MapPut(route, PutUploadAsync)
            /*.AddEndpointFilter<ApiKeyEndpointFilter>()*/;

        return builder;

        static async Task<IResult> PutUploadAsync(HttpContext context,
            [FromServices] ILoggerFactory loggerFactory,
            [FromServices] IConfiguration configuration,
            [FromServices] PostgresLfsOidMapper oidMapper,
            string code)
        {
            const string contentType = "application/octet-stream";

            if (context.Request.ContentType != contentType)
                return Results.BadRequest($"Content-Type must be {contentType}");

            if (!Guid.TryParse(code, out var guid) || !oidMapper.UploadValidationCodes.TryGetValue(guid, out var file))
                return Results.BadRequest("Invalid OID validation code");

            var stream = new MemoryStream();

            try
            {
                await context.Request.Body.CopyToAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (Exception ex)
            {
                return Results.UnprocessableEntity($"Failed to read the input stream: {ex.Message}");
            }

            if (stream.Length != file.Size)
                return Results.UnprocessableEntity("Input stream length did not match OID lookup size");
            
            var hash = BitConverter.ToString(SHA256.HashData(stream.ToArray())).Replace("-", "").ToLower();
            if (hash != file.Id)
                return Results.UnprocessableEntity("Input stream hash did not match OID lookup hash");

            var filePath = Path.Combine(configuration["BaseFilePath"]!, file.RelativePath);

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
                await using var fileStream = File.OpenWrite(Path.Combine(configuration["BaseFilePath"]!, file.RelativePath));
                await stream.CopyToAsync(fileStream);
            }
            catch (Exception ex)
            {
                var logger = loggerFactory.CreateLogger("FileUpload");
                logger.LogError(ex, "Failed to create directory and/or write file {Path}.", filePath);
                return Results.UnprocessableEntity("Failed to write file to disk.");
            }

            oidMapper.UploadValidationCodes.Remove(guid);
            return Results.Ok();
        }
    }
}