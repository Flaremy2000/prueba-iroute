using IRoute.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace IRoute.Infrastructure.Persistence;

public sealed class CommerceDbContext(DbContextOptions<CommerceDbContext> options) : DbContext(options)
{
    public DbSet<CommerceEntity> Commerce => Set<CommerceEntity>();

    public DbSet<CommerceQuarantineEntity> Quarantine => Set<CommerceQuarantineEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CommerceDbContext).Assembly);
    }
}
