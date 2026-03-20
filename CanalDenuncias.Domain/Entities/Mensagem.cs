using CanalDenuncias.Domain.Entities.Base;
using CanalDenuncias.Domain.Exceptions;

namespace CanalDenuncias.Domain.Entities;

public class Mensagem : AuditableEntityBase
{
    public string Conteudo { get; private set; }
    public int SolicitacaoId { get; set; }
    public string? Autor { get; set; }
    public virtual Solicitacao Solicitacao { get; set; }

    public Mensagem(string conteudo, Solicitacao solicitacao, string? autor)
    {
        Conteudo = conteudo;
        Solicitacao = solicitacao;
        Autor = autor;

        Validate();       
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private Mensagem()
    { }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Conteudo))
            throw new DomainException("O conteúdo da mensagem não pode ser nulo ou vazio.");

        if (Conteudo.Length < 25 || Conteudo.Length > 2_000)
            throw new DomainException("O conteúdo da mensagem deve conter entre 25 e 2000 caracteres.");
    }
}