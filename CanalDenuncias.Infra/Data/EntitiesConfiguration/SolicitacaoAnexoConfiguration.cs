using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CanalDenuncias.Infra.Data.EntitiesConfiguration;

public class SolicitacaoAnexoConfiguration : IEntityTypeConfiguration<SolicitacaoAnexo>
{
    public void Configure(EntityTypeBuilder<SolicitacaoAnexo> builder)
    {
        builder.ToTable("CD_SOLICITACAO_ANEXOS");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("ID");       

        builder.Property(a => a.SolicitacaoId)
            .HasColumnName("SOLICITACAO_ID")
            .IsRequired();

        builder.Property(a => a.Protocolo)
            .HasColumnName("PROTOCOLO")
            .IsRequired()
            .HasMaxLength(11);

        builder.Property(a => a.NomeArquivo)
            .HasColumnName("NOME_ARQUIVO")
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(a => a.NomeOriginal)
            .HasColumnName("NOME_ORIGINAL")
            .IsRequired()
            .HasMaxLength(260);

        builder.Property(a => a.ContentType)
            .HasColumnName("CONTENT_TYPE")
            .HasMaxLength(200);

        builder.Property(a => a.TamanhoOriginal)
            .HasColumnName("TAMANHO_ORIGINAL")
            .IsRequired();       

        builder.Property(a => a.Compactado)
            .HasColumnName("COMPACTADO")
            .HasConversion<int>()
            .IsRequired();

        builder.HasOne(a => a.Solicitacao)
            .WithMany(s => s.Anexos)
            .HasForeignKey(a => a.SolicitacaoId);
    }
}