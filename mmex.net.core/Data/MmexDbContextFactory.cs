using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace mmex.net.core.Data;

/// <summary>Used by EF Core tooling (dotnet ef). Not for production use.</summary>
public class MmexDbContextFactory : IDesignTimeDbContextFactory<MmexDbContext>
{
    public MmexDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MmexDbContext>()
            .UseSqlite("Data Source=design-time.mmb")
            .Options;
        return new MmexDbContext(options);
    }
}
