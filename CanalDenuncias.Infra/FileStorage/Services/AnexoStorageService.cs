using System.IO.Compression;
using CanalDenuncias.Infra.Data.Configurations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CanalDenuncias.Infra.FileStorage.Services;

public sealed class AnexoStorageService : IAnexoStorageService
{
    private readonly AppSettings _settings;

    private const string NomeAplicacao = "CANAL_DENUNCIA";
    private const string NomeTabela = "CANAL_DENUNCIA_ANEXO";

    public AnexoStorageService(IOptions<AppSettings> settings)
    {
        _settings = settings.Value;
        if (string.IsNullOrWhiteSpace(_settings.PathFileStorage))
            throw new InvalidOperationException("AppSettings.PathFileStorage não configurado.");
    }

    public async Task<string> UploadAsync(string protocolo, IFormFile file, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(protocolo))
            throw new ArgumentException("Protocolo inválido.", nameof(protocolo));

        if (file is null || file.Length == 0)
            throw new ArgumentException("Arquivo inválido (vazio).", nameof(file));

        var originalName = Path.GetFileName(file.FileName);
        var extension = Path.GetExtension(originalName);
        var oldName = Path.GetFileNameWithoutExtension(originalName);

        var fileName = $"{oldName}_{Guid.NewGuid():N}{extension}";

        // Pasta: \\path\CANAL_DENUNCIA\CANAL_DENUNCIA_ANEXO\<protocolo>\
        var dir = Path.Combine(_settings.PathFileStorage, NomeAplicacao, NomeTabela, protocolo);
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, fileName);

        await using var inputStream = file.OpenReadStream();
        await using var outputFileStream = new FileStream(
            fullPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await using var gzipStream = new GZipStream(
            outputFileStream,
            CompressionLevel.Optimal,
            leaveOpen: false);

        await inputStream.CopyToAsync(gzipStream, 81920, ct);

        return fileName;
    }

    public Task DeleteAsync(string protocolo, string nomeArquivo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(protocolo) || string.IsNullOrWhiteSpace(nomeArquivo))
            return Task.CompletedTask;

        var safeName = Path.GetFileName(nomeArquivo);
        var dir = Path.Combine(_settings.PathFileStorage, NomeAplicacao, NomeTabela, protocolo);
        var fullPath = Path.Combine(dir, safeName);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task DeleteByProtocoloAsync(string protocolo, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(protocolo))
            return Task.CompletedTask;

        var dir = Path.Combine(_settings.PathFileStorage, NomeAplicacao, NomeTabela, protocolo);

        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);

        return Task.CompletedTask;
    }

    public Task<Stream> OpenReadAsync(string protocolo, string nomeArquivo, bool decompress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(protocolo))
            throw new ArgumentException("Protocolo inválido.", nameof(protocolo));

        if (string.IsNullOrWhiteSpace(nomeArquivo))
            throw new ArgumentException("Nome do arquivo inválido.", nameof(nomeArquivo));

        var safeName = Path.GetFileName(nomeArquivo);
        var dir = Path.Combine(_settings.PathFileStorage, NomeAplicacao, NomeTabela, protocolo);
        var fullPath = Path.Combine(dir, safeName);

        if (!File.Exists(fullPath))
            throw new FileNotFoundException("Arquivo não encontrado.", safeName);

        var fileStream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true
        );

        Stream result = decompress
            ? new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: false)
            : fileStream;

        return Task.FromResult(result);
    }
}