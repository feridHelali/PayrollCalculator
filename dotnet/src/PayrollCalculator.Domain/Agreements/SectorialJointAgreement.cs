using System.Text.RegularExpressions;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Agreements;

/// <summary>Where reference data comes from (sprint 07, R3).</summary>
public enum DataOrigin
{
    /// <summary>Entered in this installation.</summary>
    User = 0,

    /// <summary>Shipped with the product; read-only in the app (D4), changed only by a new seed release.</summary>
    Seed = 1,
}

/// <summary>A sectoral joint agreement (الاتفاقية المشتركة القطاعية).</summary>
public sealed partial class SectorialJointAgreement : Entity
{
    private SectorialJointAgreement(string code, string name, string? description, DataOrigin origin, int? sortOrder)
    {
        Code = code;
        Name = name;
        Description = description;
        Origin = origin;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Stable identifier, the same in every installation (sprint 07, R2): seed files and updates find an agreement by
    /// it, never by its database id. A lowercase slug such as <c>textile</c> or <c>agences_de_voyages</c>.
    /// </summary>
    public string Code { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public DataOrigin Origin { get; private set; }

    /// <summary>Display order of the seeded list; null for agreements entered by a user.</summary>
    public int? SortOrder { get; private set; }

    public bool IsSeeded => Origin == DataOrigin.Seed;

    /// <summary>An agreement entered by a user; it gets a generated code.</summary>
    public static SectorialJointAgreement Create(string name, string? description) =>
        new("custom_" + Guid.NewGuid().ToString("N")[..8], Guard.Required(name, DomainField.AgreementName), Guard.Optional(description),
            DataOrigin.User, null);

    /// <summary>An agreement shipped with the product.</summary>
    public static SectorialJointAgreement FromSeed(string code, string name, int sortOrder) =>
        new(ValidCode(code), Guard.Required(name, DomainField.AgreementName), null, DataOrigin.Seed, sortOrder);

    public void Rename(string name)
    {
        EnsureEditable();
        Name = Guard.Required(name, DomainField.AgreementName);
    }

    public void ChangeDescription(string? description)
    {
        EnsureEditable();
        Description = Guard.Optional(description);
    }

    /// <summary>Applies a newer seed release to a seeded agreement.</summary>
    public void UpdateFromSeed(string name, int sortOrder)
    {
        if (!IsSeeded)
        {
            throw new DomainException(DomainError.NotApplicable, DomainField.Agreement, Code);
        }

        Name = Guard.Required(name, DomainField.AgreementName);
        SortOrder = sortOrder;
    }

    /// <summary>Throws when the agreement is shipped with the product and so cannot be changed here.</summary>
    public void EnsureEditable()
    {
        if (IsSeeded)
        {
            throw new DomainException(DomainError.ReadOnlySeed, DomainField.Agreement, Code);
        }
    }

    private static string ValidCode(string code)
    {
        var clean = Guard.Required(code, DomainField.AgreementCode);
        return CodePattern().IsMatch(clean) ? clean : throw new DomainException(DomainError.OutOfRange, DomainField.AgreementCode, clean);
    }

    [GeneratedRegex("^[a-z][a-z0-9_]{1,79}$")]
    private static partial Regex CodePattern();
}

public interface IAgreementRepository : IRepository<SectorialJointAgreement>
{
    Task<SectorialJointAgreement?> FindByCodeAsync(string code, CancellationToken cancellationToken = default);
}
