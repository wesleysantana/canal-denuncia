using CanalDenuncias.Domain.Entities.Base;
using CanalDenuncias.Domain.Exceptions;

namespace CanalDenuncias.Domain.Entities;

public class Solicitacao : AuditableEntityBase
{
    public string Protocolo { get; private set; }
    public bool Anonima { get; private set; }
    public string? IdentificadorColaboradorOuSetor { get; private set; }
    public string? Testemunhas { get; private set; }
    public string? Evidencias { get; private set; }    
    public DateTime DataOcorrencia { get; private set; }
    public int? UsuarioId { get; private set; }
    public Usuario? Usuario { get; private set; }
    public int StatusSolicitacaoId { get; private set; }
    public virtual StatusSolicitacao? StatusSolicitacao { get; private set; }
    public int VinculoId { get; private set; }
    public virtual Vinculo? Vinculo { get; private set; }
    public virtual ICollection<Mensagem> Mensagens { get; private set; } = new HashSet<Mensagem>();
    public virtual IEnumerable<SolicitacaoAnexo> Anexos { get; private set; } = new HashSet<SolicitacaoAnexo>();

    public Solicitacao(
        bool anonima,
        string identificadorColaboradorOuSetor,
        string? testemunhas,
        string? evidencias,
        Usuario? usuario,
        DateTime dataOcorrido,
        int vinculoId
        )
    {
        Protocolo = GenerateProtocolo();
        Anonima = anonima;
        DataOcorrencia = dataOcorrido;
        IdentificadorColaboradorOuSetor = identificadorColaboradorOuSetor;
        Testemunhas = testemunhas;
        Evidencias = evidencias;
        Usuario = usuario;
        StatusSolicitacaoId = 1;
        VinculoId = vinculoId;

        Validate();
    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private Solicitacao()
    { }

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    private string GenerateProtocolo()
    {
        string datePart = DateTime.Now.ToString("yyMMmm");
        string guidNumbers = new(Guid.NewGuid().ToString().Where(char.IsDigit).Take(5).ToArray());
        string resultado = datePart + guidNumbers;
        return resultado;
    }

    public void AtualizarStatus(int statusSolicitacaoId)
    {
        if(statusSolicitacaoId < 1)
            throw new DomainException("O ID do status da solicitação deve ser maior que zero.");

        StatusSolicitacaoId = statusSolicitacaoId;
    }

    private void Validate()
    {
        if (IdentificadorColaboradorOuSetor?.Length < 3 || IdentificadorColaboradorOuSetor?.Length > 100)
            throw new DomainException("O identificador do colaborador ou setor deve conter entre 3 e 100 caracteres.");

        if (Testemunhas?.Length < 3 || Testemunhas?.Length > 500)
            throw new DomainException("As testemunhas devem conter entre 3 e 500 caracteres.");

        if (Evidencias?.Length < 5 || Evidencias?.Length > 500)
            throw new DomainException("As evidências devem conter entre 5 e 500 caracteres.");

        if (Anonima && Usuario != null)
            throw new DomainException("Uma solicitação anônima não pode ter um usuário associado.");

        if (!Anonima && Usuario == null)
            throw new DomainException("Uma solicitação não anônima deve ter um usuário associado.");

        if (DataOcorrencia > DateTime.Now)
            throw new DomainException("A data do ocorrido não pode ser no futuro.");
    }
}