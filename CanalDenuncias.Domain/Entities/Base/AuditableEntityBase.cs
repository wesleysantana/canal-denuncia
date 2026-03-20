namespace CanalDenuncias.Domain.Entities.Base;

public abstract class AuditableEntityBase : EntityBase
{
    public DateTime DataCriacao { get; protected set; } = DateTime.Now;
}