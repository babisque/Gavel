using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Gavel.Api.Infrastructure.Data;

public class GavelDbContextFactory : IDesignTimeDbContextFactory<GavelDbContext>
{
    public GavelDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GavelDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=gaveldb;Username=postgres;Password=postgres");

        return new GavelDbContext(optionsBuilder.Options);
    }
}
