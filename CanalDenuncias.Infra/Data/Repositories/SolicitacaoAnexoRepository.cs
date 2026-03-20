using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CanalDenuncias.Infra.Data.Repositories;

public class SolicitacaoAnexoRepository : ISolicitacaoAnexoRepository
{
    private readonly DataContext _context;

    public SolicitacaoAnexoRepository(DataContext context)
    {
        _context = context;
    }

    public void Add(SolicitacaoAnexo anexo) => _context.SolicitacaoAnexos.Add(anexo);  

    public async Task<SolicitacaoAnexo?> ObterAnexoAsync(int anexoId, CancellationToken ct)
    {
        return await _context.SolicitacaoAnexos
            .Where(a => a.Id == anexoId)
            .AsNoTracking()
            .FirstOrDefaultAsync(ct);
    }
}