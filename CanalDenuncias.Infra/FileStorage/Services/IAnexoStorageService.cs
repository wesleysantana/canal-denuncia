using Microsoft.AspNetCore.Http;

namespace CanalDenuncias.Infra.FileStorage.Services;

public interface IAnexoStorageService
{
    Task<string> UploadAsync(string protocolo, IFormFile file, CancellationToken ct);

    Task DeleteAsync(string protocolo, string nomeArquivo, CancellationToken ct);

    Task DeleteByProtocoloAsync(string protocolo, CancellationToken ct);

    Task<Stream> OpenReadAsync(string protocolo, string nomeArquivo, bool decompress, CancellationToken ct);
}