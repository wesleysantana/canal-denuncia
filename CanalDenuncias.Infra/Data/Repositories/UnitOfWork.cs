using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CanalDenuncias.Infra.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly DataContext _context;

    public UnitOfWork(DataContext context)
    {
        _context = context;
    }

    public async Task<bool> CommitAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken: cancellationToken) > 0;
        }
        catch (DbUpdateException ex)
        {
            throw new ApplicationException("Erro ao salvar alterações no banco de dados", ex);
        }
        catch (Exception ex)
        {
            throw new ApplicationException("Erro inesperado durante a confirmação da transação", ex);
        }
    }
}