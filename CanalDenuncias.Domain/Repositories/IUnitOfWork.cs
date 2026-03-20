namespace CanalDenuncias.Domain.Repositories;

public interface IUnitOfWork
{
    Task<bool> CommitAsync(CancellationToken cancellationToken);
}