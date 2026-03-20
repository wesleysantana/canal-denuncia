using CanalDenuncias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CanalDenuncias.Infra.Data.EntitiesConfiguration;

public class MensagemConfiguration : IEntityTypeConfiguration<Mensagem>
{
    public void Configure(EntityTypeBuilder<Mensagem> builder)
    {
        builder.ToTable("CD_MENSAGENS");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("ID");

        builder.Property(m => m.DataCriacao)
           .HasColumnName("DATA_CRIACAO")
           .IsRequired();

        builder.Property(m => m.Conteudo)
            .HasColumnName("CONTEUDO")
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(m => m.Autor)
            .HasColumnName("AUTOR")
            .HasMaxLength(100);

        builder.Property(m => m.SolicitacaoId)
            .HasColumnName("SOLICITACAO_ID")
            .IsRequired();

        builder.HasOne(m => m.Solicitacao)
            .WithMany(s => s.Mensagens)
            .HasForeignKey(s => s.SolicitacaoId);
    }
}