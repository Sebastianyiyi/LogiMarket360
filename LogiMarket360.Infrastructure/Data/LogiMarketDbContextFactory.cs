namespace LogiMarket360.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

public class LogiMarketDbContextFactory : IDesignTimeDbContextFactory<LogiMarketDbContext>
{
    public LogiMarketDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LogiMarketDbContext>()
            .UseSqlite("Data Source=logimarket360.db")
            .Options;

        return new LogiMarketDbContext(options);
    }
}
