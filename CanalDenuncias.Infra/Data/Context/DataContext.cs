using System.Reflection;
using CanalDenuncias.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CanalDenuncias.Infra.Data.Context;

public class DataContext : DbContext
{
    public DataContext(DbContextOptions<DataContext> options) : base(options)
    {
    }

    public DbSet<Mensagem> Mensagens { get; set; }
    public DbSet<Solicitacao> Solicitacoes { get; set; }
    public DbSet<StatusSolicitacao> StatusSolicitacoes { get; set; }
    public DbSet<Vinculo> Vinculos { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<SolicitacaoAnexo> SolicitacaoAnexos { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Carrega todas as configurações específicas das entidades
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}