using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.DTOs.Response;
using CanalDenuncias.Application.Results;

namespace CanalDenuncias.Application.Interfaces;

public interface IMensagemService
{
    Task<Result<string>> CriarMensagemAsync(
        MensagemRequest msgRequest, string? userLogado, CancellationToken cancellationToken);

    Task<Result<IEnumerable<MensagemResponse>>> ObterMensagensPorProtocoloAsync(
        string protocolo, CancellationToken cancellationToken);    
}