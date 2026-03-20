using CanalDenuncias.Domain.Entities;
using CanalDenuncias.Domain.Entities.Base;

public class SolicitacaoAnexo : EntityBase
{
    public int SolicitacaoId { get; private set; }
    public virtual Solicitacao Solicitacao { get; private set; }
    public string Protocolo { get; private set; } = default!;
    public string NomeArquivo { get; private set; } = default!;
    public string NomeOriginal { get; private set; } = default!;
    public string? ContentType { get; private set; }
    public long TamanhoOriginal { get; private set; }
    public bool Compactado { get; private set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private SolicitacaoAnexo()
    { }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    public SolicitacaoAnexo(
        Solicitacao solicitacao,
        string protocolo,
        string nomeArquivo,
        string nomeOriginal,
        string? contentType,
        long tamanhoOriginal,
        bool compactado)
    {
        Solicitacao = solicitacao;
        Protocolo = protocolo;
        NomeArquivo = nomeArquivo;
        NomeOriginal = nomeOriginal;
        ContentType = contentType;
        TamanhoOriginal = tamanhoOriginal;
        Compactado = compactado;
    }
}