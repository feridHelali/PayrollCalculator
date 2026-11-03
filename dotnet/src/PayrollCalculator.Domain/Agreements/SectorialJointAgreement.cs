using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Domain.Agreements;

/// <summary>A sectoral joint agreement (الاتفاقية القطاعية المشتركة) that owns salary tables.</summary>
public sealed class SectorialJointAgreement : Entity
{
    private SectorialJointAgreement(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public static SectorialJointAgreement Create(string name, string? description) =>
        new(Guard.Required(name, "Agreement name"), Guard.Optional(description));

    public void Rename(string name) => Name = Guard.Required(name, "Agreement name");

    public void ChangeDescription(string? description) => Description = Guard.Optional(description);
}

public interface IAgreementRepository : IRepository<SectorialJointAgreement>;
