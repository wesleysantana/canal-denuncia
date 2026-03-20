using CanalDenuncias.Domain.Entities.Base;

namespace CanalDenuncias.Domain.Entities;

public class Vinculo : EntityBase
{
    public string Descricao { get; private set; } = string.Empty;
    public virtual ICollection<Solicitacao> Solicitacoes { get; private set; } = new HashSet<Solicitacao>();
} 