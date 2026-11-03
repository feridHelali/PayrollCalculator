using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Application.Tests;

/// <summary>Test double for the persistence ports: ids are assigned on save, like a database would.</summary>
internal sealed class InMemoryStore : IUnitOfWork
{
    private int _nextId = 1;

    public AgreementRepo Agreements { get; } = new();

    public TableRepo SalaryTables { get; } = new();

    public AffairRepo Affairs { get; } = new();

    public int SaveCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<Entity> all = [.. Agreements.Items, .. SalaryTables.Items, .. Affairs.Items];
        foreach (var entity in all.Where(e => e.IsTransient))
        {
            entity.AssignIdentity(_nextId++);
        }

        SaveCount++;
        return Task.CompletedTask;
    }

    internal abstract class Repo<T> : IRepository<T> where T : Entity
    {
        public List<T> Items { get; } = [];

        public Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<T>>(Items.ToList());

        public Task<T?> GetAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public void Add(T aggregate) => Items.Add(aggregate);

        public void Remove(T aggregate) => Items.Remove(aggregate);
    }

    internal sealed class AgreementRepo : Repo<SectorialJointAgreement>, IAgreementRepository;

    internal sealed class AffairRepo : Repo<Affair>, IAffairRepository;

    internal sealed class TableRepo : Repo<SalaryTable>, ISalaryTableRepository
    {
        public Task<IReadOnlyList<SalaryTable>> ListByAgreementAsync(int agreementId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SalaryTable>>(Items.Where(t => t.AgreementId == agreementId).ToList());
    }
}
