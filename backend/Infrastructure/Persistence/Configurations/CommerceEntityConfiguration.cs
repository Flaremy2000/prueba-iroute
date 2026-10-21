using IRoute.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRoute.Infrastructure.Persistence.Configurations;

internal sealed class CommerceEntityConfiguration : IEntityTypeConfiguration<CommerceEntity>
{
    public void Configure(EntityTypeBuilder<CommerceEntity> builder)
    {
        builder.ToTable("commerce");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.MerchantName)
            .HasColumnName("pc_nomcomred")
            .HasMaxLength(200);

        builder.Property(entity => entity.DocumentNumber)
            .HasColumnName("pc_numdoc")
            .HasMaxLength(50);

        builder.Property(entity => entity.ProcessDate)
            .HasColumnName("pc_processdate")
            .HasColumnType("date");
    }
}
