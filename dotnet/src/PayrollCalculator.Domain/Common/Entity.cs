namespace PayrollCalculator.Domain.Common;

/// <summary>Base class for entities identified by a database-generated integer id.</summary>
public abstract class Entity
{
    public int Id { get; private set; }

    public bool IsTransient => Id == 0;

    /// <summary>
    /// Sets the identity of a reconstituted entity (tests, data import).
    /// Normal persistence assigns the id through the ORM.
    /// </summary>
    public void AssignIdentity(int id)
    {
        if (!IsTransient)
        {
            throw new DomainException(DomainError.IdentityAlreadyAssigned, DomainField.Id);
        }

        Id = Guard.Positive(id, DomainField.Id);
    }
}
