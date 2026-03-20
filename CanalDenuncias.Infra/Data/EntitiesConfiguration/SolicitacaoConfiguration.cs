using CanalDenuncias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CanalDenuncias.Infra.Data.EntitiesConfiguration;

public class SolicitacaoConfiguration : IEntityTypeConfiguration<Solicitacao>
{
    public void Configure(EntityTypeBuilder<Solicitacao> builder)
    {
        builder.ToTable("CD_SOLICITACOES");
        builder.HasKey(s => s.Id);

        builder.Property(m => m.Id)
            .HasColumnName("ID");

        builder.Property(m => m.DataCriacao)
          .HasColumnName("DATA_CRIACAO")
          .IsRequired();

        builder.Property(s => s.Protocolo)
            .HasColumnName("PROTOCOLO")
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(s => s.Anonima)
            .HasColumnName("ANONIMA")
            .HasConversion<int>() // Converte bool para int (0 ou 1) ao gravar
            .IsRequired();

        builder.Property(s => s.IdentificadorColaboradorOuSetor)
            .HasColumnName("IDENTIFICADOR")
            .HasMaxLength(100);

        builder.Property(s => s.Testemunhas)
            .HasColumnName("TESTEMUNHAS")
            .HasMaxLength(500);

        builder.Property(s => s.Evidencias)
            .HasColumnName("EVIDENCIAS")
            .HasMaxLength(500);

        builder.Property(s => s.DataOcorrencia)
            .HasColumnName("DATA_OCORRENCIA")
            .IsRequired();      

        builder.Property(s => s.StatusSolicitacaoId)
          .HasColumnName("STATUS_SOLICITACAO_ID")
          .IsRequired();   
        
        builder.Property(s => s.VinculoId)
            .HasColumnName("VINCULO_ID")
            .IsRequired();

        builder.Property(s => s.UsuarioId)
            .HasColumnName("USUARIO_ID");

        builder.HasOne(s => s.Usuario)
            .WithMany(s => s.Solicitacoes)
            .HasForeignKey(s => s.UsuarioId);

        builder.HasOne(s => s.StatusSolicitacao)
            .WithMany(s => s.Solicitacoes)
            .HasForeignKey(s => s.StatusSolicitacaoId);

        builder.HasOne(s => s.Vinculo)
            .WithMany(v => v.Solicitacoes)
            .HasForeignKey(s => s.VinculoId);

        builder.HasMany(s => s.Mensagens)
            .WithOne(m => m.Solicitacao)
            .HasForeignKey(m => m.SolicitacaoId);

        builder.HasMany(s => s.Anexos)
            .WithOne(a => a.Solicitacao)
            .HasForeignKey(a => a.SolicitacaoId);
    }
}