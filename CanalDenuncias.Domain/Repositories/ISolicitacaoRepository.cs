using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Filters;

namespace CanalDenuncias.Domain.Repositories;

public interface ISolicitacaoRepository
{
    void Add(Solicitacao solicitacao);

    Task<Solicitacao?> ObterSolicitacaoPorProtocoloAsync(
        string protocolo, CancellationToken cancellationToken);

    Task<IEnumerable<Solicitacao>> ListarSolicitacoesAsync(
        SolicitacaoFilter filtro, CancellationToken cancellationToken);

    Task<Solicitacao?> AtualizarStatus(
        Solicitacao solicitacao, int novoStatusId, CancellationToken cancellationToken);
}