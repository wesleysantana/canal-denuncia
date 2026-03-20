using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace CanalDenuncias.Infra.Data.Repositories;

public class MensagemRepository : IMensagemRepository
{
    private readonly DataContext _context;

    public MensagemRepository(DataContext context)
    {
        _context = context;
    }

    public void Add(Mensagem mensagem) => _context.Mensagens.Add(mensagem);

    public async Task<IEnumerable<Mensagem>> ObterMensagensPorProtocoloAsync(
        string protocolo, CancellationToken cancellationToken)
    {
        return await _context.Mensagens
            .Where(m => m.Solicitacao.Protocolo == protocolo)
            .OrderBy(m => m.DataCriacao)
            .OrderDescending()
            .AsNoTracking()
            .ToListAsync(cancellationToken: cancellationToken);
    }
}