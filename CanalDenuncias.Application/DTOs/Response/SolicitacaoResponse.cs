namespace CanalDenuncias.Application.DTOs.Response;

public class SolicitacaoResponse
{
    public int Id { get; set; }
    public string Protocolo { get; set; } = string.Empty;
    public DateTime DataCriacao { get; set; }
    public DateTime DataOCorrencia { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool Anonima { get; set; }
    public string? Nome { get; set; }
    public string? Email { get; set; }
    public string? Telefone { get; set; }
    public string? Cpf { get; set; }
    public IEnumerable<SolicitacaoAnexoResponse>? Anexos { get; set; }
}