namespace CanalDenuncias.Application.DTOs.Response;

public class SolicitacaoAnexoResponse
{
    public int Id { get; set; }
    public string NomeOriginal { get; set; } = string.Empty;
    public string NomeArquivo { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long TamanhoOriginal { get; set; }
    public DateTime DataCriacao { get; set; }
}