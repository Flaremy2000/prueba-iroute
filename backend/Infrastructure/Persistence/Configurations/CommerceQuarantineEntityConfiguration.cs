using IRoute.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IRoute.Infrastructure.Persistence.Configurations;

internal sealed class CommerceQuarantineEntityConfiguration : IEntityTypeConfiguration<CommerceQuarantineEntity>
{
    public void Configure(EntityTypeBuilder<CommerceQuarantineEntity> builder)
    {
        builder.ToTable("commerce_quarantine");

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

        builder.Property(entity => entity.Motivo)
            .HasColumnName("motivo")
            .HasMaxLength(400)
            .IsRequired();
    }
}
