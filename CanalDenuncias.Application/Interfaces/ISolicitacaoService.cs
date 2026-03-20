using CanalDenuncias.Application.DTOs.Request;
using CanalDenuncias.Application.DTOs.Response;
using CanalDenuncias.Application.Results;
using CanalDenuncias.Domain.Filters;
using Microsoft.AspNetCore.Http;

namespace CanalDenuncias.Application.Interfaces;

public interface ISolicitacaoService
{
    Task<Result<string>> CriarSolicitacaoAsync(
        SolicitacaoRequest request, List<IFormFile>? anexos, CancellationToken cancellationToken);   

    public Task<Result<IEnumerable<SolicitacaoResponse>>> ListarSolicitacoesAsync(
        SolicitacaoFilter filtro, CancellationToken cancellationToken);

    public Task<Result<bool>> AtualizarStatus(
        string protocolo, int novoStatusId, CancellationToken cancellationToken);   

    Task<Result<(Stream Stream, string ContentType, string DownloadName)>> DownloadAnexoAsync(
        string protocolo, int anexoId, CancellationToken ct);
}