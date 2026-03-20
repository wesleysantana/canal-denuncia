using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Filters;
using CanalDenuncias.Domain.Repositories;
using CanalDenuncias.Infra.Data.Context;
using CanalDenuncias.Infra.Data.Repositories.Filters;
using Microsoft.EntityFrameworkCore;

namespace CanalDenuncias.Infra.Data.Repositories;

public class SolicitacaoRepository : ISolicitacaoRepository
{
    private readonly DataContext _context;

    public SolicitacaoRepository(DataContext context)
    {
        _context = context;
    }

    public void Add(Solicitacao solicitacao) => _context.Solicitacoes.Add(solicitacao);

    public async Task<Solicitacao?> ObterSolicitacaoPorProtocoloAsync(
        string protocolo, CancellationToken cancellationToken)
    {
        return await _context.Solicitacoes
            .Include(s => s.StatusSolicitacao)
            .Include(s => s.Usuario)
            .FirstOrDefaultAsync(s => s.Protocolo == protocolo, cancellationToken: cancellationToken);
    }  

    public async Task<IEnumerable<Solicitacao>> ListarSolicitacoesAsync(
        SolicitacaoFilter filtro, CancellationToken cancellationToken)
    {
        var spec = new SolicitacaoFilterSpecification(filtro);

        var query = _context.Solicitacoes.Include(x => x.StatusSolicitacao).AsQueryable();

        // O Evaluator aplica todos os Wheres, Includes, OrderBy e Skip/Take de uma vez
        var result = SpecificationEvaluator.Default.GetQuery(query: query, specification: spec);

        return await result
            .Include(s => s.StatusSolicitacao)
            .Include(s => s.Usuario)
            .Include(s => s.Anexos)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Solicitacao?> AtualizarStatus(
        Solicitacao solicitacao,
        int novoStatusId,
        CancellationToken cancellationToken)
    {
        if (solicitacao == null)
            return null;

        solicitacao.AtualizarStatus(novoStatusId);
        _context.Solicitacoes.Update(solicitacao);

        return solicitacao;
    }
}