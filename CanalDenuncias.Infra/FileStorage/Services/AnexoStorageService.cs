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

        byte[] originalBytes;
        await using (var ms = new MemoryStream())
        {
            await file.CopyToAsync(ms, ct);
            originalBytes = ms.ToArray();
        }

        var compressed = CompressGzip(originalBytes);

        await File.WriteAllBytesAsync(fullPath, compressed, ct);

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

        // Stream do arquivo armazenado (gzip)
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        // Se Compactado = true, devolve stream descompactado; senão devolve o arquivo puro
        Stream result = decompress
            ? new GZipStream(fileStream, CompressionMode.Decompress, leaveOpen: false)
            : fileStream;

        return Task.FromResult(result);
    }

    private static byte[] CompressGzip(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();

        using (var gzip = new GZipStream(output, CompressionMode.Compress, leaveOpen: true))
        {
            input.CopyTo(gzip);
        }

        return output.ToArray();
    }

    public static byte[] DecompressGzip(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzip.CopyTo(output);
        return output.ToArray();
    }
}