using CanalDenuncias.Domain.Entities.Base;
using CanalDenuncias.Domain.Exceptions;
using CanalDenuncias.Domain.Utils;

namespace CanalDenuncias.Domain.Entities;

public class Usuario : AuditableEntityBase
{
    public string Nome { get; private set; }
    public string Telefone { get; private set; }
    public string Email { get; private set; }
    public string CPF { get; private set; }
    public virtual ICollection<Solicitacao> Solicitacoes { get; private set; } = new HashSet<Solicitacao>();

    public Usuario(string nome, string telefone, string email, string cPF)
    {
        Nome = nome;
        Telefone = telefone;
        Email = email;
        CPF = cPF;

        Validate();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private Usuario()
    { }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private void Validate()
    {
        if (string.IsNullOrWhiteSpace(Nome))
            throw new DomainException("O nome não pode ser vazio");

        if (Nome.Length < 3 || Nome.Length > 100)
            throw new DomainException("O nome deve conter entre 3 e 100 caracteres.");

        if (string.IsNullOrWhiteSpace(Telefone))
            throw new DomainException("O telefone não pode ser vazio.");

        if (Telefone.Length < 8 || Telefone.Length > 15)
            throw new DomainException("O telefone deve conter entre 8 e 15 caracteres.");

        if (!DomainValidator.IsValidEmail(Email))
            throw new DomainException("O email é inválido.");

        if (!DomainValidator.CPFValidator(CPF))
            throw new DomainException("O CPF é inválido.");
    }
}