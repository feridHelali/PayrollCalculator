using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.SalaryTables;

namespace PayrollCalculator.Infrastructure.Persistence;

internal sealed class AgreementConfiguration : IEntityTypeConfiguration<SectorialJointAgreement>
{
    public void Configure(EntityTypeBuilder<SectorialJointAgreement> b)
    {
        b.ToTable("Agreements");
        b.HasKey(a => a.Id);
        b.Property(a => a.Name).IsRequired().HasMaxLength(500);
        b.Property(a => a.Description).HasMaxLength(4000);
        b.Property(a => a.Code).IsRequired().HasMaxLength(80);
        b.HasIndex(a => a.Code).IsUnique();
        b.Property(a => a.Origin).HasConversion<string>().HasMaxLength(10);
        b.Ignore(a => a.IsTransient);
        b.Ignore(a => a.IsSeeded);
        b.HasData(AgreementSeed.All.Select(a => new
        {
            a.Id,
            a.Code,
            a.Name,
            Description = (string?)null,
            Origin = DataOrigin.Seed,
            SortOrder = (int?)a.Id,
        }));
    }
}

internal sealed class SalaryTableConfiguration : IEntityTypeConfiguration<SalaryTable>
{
    public void Configure(EntityTypeBuilder<SalaryTable> b)
    {
        b.ToTable("SalaryTables");
        b.HasKey(t => t.Id);
        b.Ignore(t => t.IsTransient);
        b.Property(t => t.TableNumber).IsRequired().HasMaxLength(50);
        b.Property(t => t.SalaryType).HasConversion<string>().HasMaxLength(20);
        b.Property(t => t.ConcernedEmployees).HasMaxLength(1000);
        b.HasOne<SectorialJointAgreement>().WithMany().HasForeignKey(t => t.AgreementId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(t => new { t.AgreementId, t.TableNumber });

        b.ComplexProperty(t => t.ApplicationPeriod, p =>
        {
            p.Property(r => r.Start).HasColumnName("ApplicationStart");
            p.Property(r => r.End).HasColumnName("ApplicationEnd");
            p.Ignore(r => r.IsOpenEnded);
        });

        b.OwnsMany(t => t.Categories, c =>
        {
            c.ToTable("SalaryTableCategories");
            c.WithOwner().HasForeignKey("SalaryTableId");
            c.Property<int>("Id");
            c.HasKey("Id");
            c.Property(x => x.Key).IsRequired().HasMaxLength(64);
            c.Property(x => x.Label).IsRequired().HasMaxLength(500);
        });
        b.Navigation(t => t.Categories).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(t => t.Degrees, d =>
        {
            d.ToTable("SalaryTableDegrees");
            d.WithOwner().HasForeignKey("SalaryTableId");
            d.Property<int>("Id");
            d.HasKey("Id");
            d.Property(x => x.Key).IsRequired().HasMaxLength(64);
        });
        b.Navigation(t => t.Degrees).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(t => t.Cells, c =>
        {
            c.ToTable("SalaryCells");
            c.WithOwner().HasForeignKey("SalaryTableId");
            c.Property<int>("Id");
            c.HasKey("Id");
            c.Property(x => x.CategoryKey).IsRequired().HasMaxLength(64);
            c.Property(x => x.DegreeKey).IsRequired().HasMaxLength(64);
            c.Property(x => x.Amount).HasPrecision(18, 4);
        });
        b.Navigation(t => t.Cells).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class AffairConfiguration : IEntityTypeConfiguration<Affair>
{
    public void Configure(EntityTypeBuilder<Affair> b)
    {
        b.ToTable("Affairs");
        b.HasKey(a => a.Id);
        b.Ignore(a => a.IsTransient);
        b.Property(a => a.AffairNumber).IsRequired().HasMaxLength(100);
        b.Property(a => a.Title).HasMaxLength(500);
        b.Property(a => a.Claimant).IsRequired().HasMaxLength(500);
        b.HasOne<SectorialJointAgreement>().WithMany().HasForeignKey(a => a.AgreementId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<SalaryTable>().WithMany().HasForeignKey(a => a.SalaryTableId).OnDelete(DeleteBehavior.SetNull);

        b.ComplexProperty(a => a.EmploymentPeriod, p =>
        {
            p.Property(r => r.Start).HasColumnName("EmploymentStart");
            p.Property(r => r.End).HasColumnName("EmploymentEnd");
            p.Ignore(r => r.IsOpenEnded);
        });

        b.ComplexProperty(a => a.InitialPosition, p =>
        {
            p.Property(x => x.Category).HasColumnName("InitialCategory").IsRequired().HasMaxLength(500);
            p.Property(x => x.Degree).HasColumnName("InitialDegree");
        });
    }
}
