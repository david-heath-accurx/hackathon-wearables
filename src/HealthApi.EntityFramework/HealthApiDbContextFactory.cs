using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HealthApi.EntityFramework;

public class HealthApiDbContextFactory : IDesignTimeDbContextFactory<HealthApiDbContext>
{
    public HealthApiDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<HealthApiDbContext>()
            .UseSqlServer("Server=hackathon-wearables.database.windows.net;Database=hackathon-wearables;Authentication=Active Directory Default;")
            .Options;

        return new HealthApiDbContext(options);
    }
}
