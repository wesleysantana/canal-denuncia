using CanalDenuncias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CanalDenuncias.Infra.Data.EntitiesConfiguration;

public class VinculoConfiguration
{
    public void Configure(EntityTypeBuilder<Vinculo> builder)
    {
        builder.ToTable("CD_VINCULOS");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("ID");       

        builder.Property(s => s.Descricao)
            .HasColumnName("DESCRICAO")
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(s => s.Solicitacoes)
           .WithOne(s => s.Vinculo)
           .HasForeignKey(s => s.VinculoId);
    }
}