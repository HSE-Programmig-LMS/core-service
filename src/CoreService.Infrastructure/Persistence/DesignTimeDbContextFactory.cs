using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoreService.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
    public CoreDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("CORE_DB_CS");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                "CORE_DB_CS is not set. Provide connection string for EF migrations, e.g. CORE_DB_CS='Host=...;Database=...;Username=...;Password=...'"
            );
        var opt = new DbContextOptionsBuilder<CoreDbContext>()
            .UseNpgsql(cs)
            .Options;

        return new CoreDbContext(opt);
    }
}
