using Microsoft.EntityFrameworkCore;

namespace OpenFastDL.Api;

public sealed class DatabaseContext(DbContextOptions<DatabaseContext> options) : DbContext(options)
{
    public DbSet<RemoteFile> Files { get; init; }
}