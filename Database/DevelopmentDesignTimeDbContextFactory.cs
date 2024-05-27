using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OpenFastDL.Api;

public sealed class DevelopmentDesignTimeDbContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<DatabaseContext>().UseNpgsql();
        return new DatabaseContext(options.Options);
    }
}