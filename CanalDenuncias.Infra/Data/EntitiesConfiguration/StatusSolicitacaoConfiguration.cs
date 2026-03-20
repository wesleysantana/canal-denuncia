using CanalDenuncias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CanalDenuncias.Infra.Data.EntitiesConfiguration;

public class StatusSolicitacaoConfiguration : IEntityTypeConfiguration<StatusSolicitacao>
{
    public void Configure(EntityTypeBuilder<StatusSolicitacao> builder)
    {
        builder.ToTable("CD_STATUS_SOLICITACOES");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("ID");       

        builder.Property(s => s.Descricao)
            .HasColumnName("DESCRICAO")
            .IsRequired()
            .HasMaxLength(50);

        builder.HasMany(s => s.Solicitacoes)
           .WithOne(s => s.StatusSolicitacao)
           .HasForeignKey(s => s.StatusSolicitacaoId);
    }
}