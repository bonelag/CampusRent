using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace CampusRent.Server.Data;
// Migrations always target PostgreSQL. Development SQLite uses EnsureCreated.
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CampusRentDbContext>
{
    public CampusRentDbContext CreateDbContext(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=campusrent;Username=postgres;Password=local-design-time";
        return new CampusRentDbContext(new DbContextOptionsBuilder<CampusRentDbContext>().UseNpgsql(connection).Options);
    }
}
