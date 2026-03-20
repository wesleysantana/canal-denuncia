using CanalDenuncias.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CanalDenuncias.Infra.Data.EntitiesConfiguration;

public class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("CD_USUARIOS");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("ID");

        builder.Property(m => m.DataCriacao)
          .HasColumnName("DATA_CRIACAO")
          .IsRequired();

        builder.Property(m => m.Nome)
            .HasColumnName("NOME")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Telefone)
            .HasColumnName("TELEFONE")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.Email)
            .HasColumnName("EMAIL")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.CPF)
            .HasColumnName("CPF")
            .HasMaxLength(14)
            .IsRequired();      
    }
}

