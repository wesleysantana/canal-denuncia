namespace CanalDenuncias.Domain.Repositories;

public interface ISolicitacaoAnexoRepository
{
    void Add(SolicitacaoAnexo anexo);

    Task<SolicitacaoAnexo?> ObterAnexoAsync(int anexoId, CancellationToken ct);
}