namespace PayrollCalculator.Domain.Common;

/// <summary>Generic aggregate repository port (implemented in Infrastructure).</summary>
public interface IRepository<T> where T : Entity
{
    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);

    Task<T?> GetAsync(int id, CancellationToken cancellationToken = default);

    void Add(T aggregate);

    void Remove(T aggregate);
}

/// <summary>Commits all changes tracked by the repositories as one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
