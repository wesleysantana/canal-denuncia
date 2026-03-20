namespace CanalDenuncias.Application.DTOs.Response;

public class MensagemResponse
{
    public int Id { get; set; }
    public string Conteudo { get; set; } = string.Empty;
    public string Protocolo { get; set; } = string.Empty;
    public DateTime DataMensagem { get; set; }
    public bool PublicadoPeloAutor { get; set; }
}