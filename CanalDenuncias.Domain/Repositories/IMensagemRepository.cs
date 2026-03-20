using CanalDenuncias.Domain.Entities;

namespace CanalDenuncias.Domain.Repositories;

public interface IMensagemRepository
{
    void Add(Mensagem mensagem);

    Task<IEnumerable<Mensagem>> ObterMensagensPorProtocoloAsync(
        string protocolo, CancellationToken cancellationToken);
}