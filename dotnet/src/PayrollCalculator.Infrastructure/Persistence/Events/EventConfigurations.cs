using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using PayrollCalculator.Domain.Affairs;
using PayrollCalculator.Domain.Agreements;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Ledger;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Infrastructure.Persistence.Events;

internal sealed class LegalSourceConfiguration : IEntityTypeConfiguration<LegalSource>
{
    public void Configure(EntityTypeBuilder<LegalSource> b)
    {
        b.ToTable("LegalSources");
        b.HasKey(s => s.Id);
        b.Ignore(s => s.IsTransient);
        b.Property(s => s.Kind).HasConversion<string>().HasMaxLength(30);
        b.Property(s => s.Number).IsRequired().HasMaxLength(100);
        b.Property(s => s.Title).IsRequired().HasMaxLength(1000);
        b.Property(s => s.FileName).HasMaxLength(500);
        b.HasIndex(s => new { s.Kind, s.Number }).IsUnique();
        b.OwnsOne(s => s.Jort, j =>
        {
            j.Property(x => x.IssueNumber).HasColumnName("JortIssue");
            j.Property(x => x.IssueDate).HasColumnName("JortDate");
        });
    }
}

internal sealed class RuleEventConfiguration : IEntityTypeConfiguration<RecordedRuleEvent>
{
    public void Configure(EntityTypeBuilder<RecordedRuleEvent> b)
    {
        b.ToTable("RuleEvents");
        b.HasKey(e => e.Id);
        b.Property(e => e.Event).HasColumnName("Payload").IsRequired()
            .HasConversion(new ValueConverter<RuleEvent, string>(e => EventJson.Serialize(e), s => EventJson.DeserializeRuleEvent(s)))
            .Metadata.SetValueComparer(EventStorage.ByReference<RuleEvent>());
        b.Property(e => e.Kind).HasConversion<string>().HasMaxLength(40);
        b.Property(e => e.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(e => e.RecordedBy).IsRequired().HasMaxLength(100);
        b.Property(e => e.ReviewedBy).HasMaxLength(100);
        b.Property(e => e.Note).HasMaxLength(2000);
        b.Ignore(e => e.IsInForce);
        b.HasOne<SectorialJointAgreement>().WithMany().HasForeignKey(e => e.AgreementId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<LegalSource>().WithMany().HasForeignKey(e => e.LegalSourceId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(e => new { e.AgreementId, e.EffectiveFrom });
    }
}

/// <summary>A career event as stored: the payload, plus the columns needed to query and order.</summary>
internal sealed class CareerEventRow
{
    /// <summary>Recorded order.</summary>
    public int Id { get; set; }

    public Guid EventId { get; set; }

    public int AffairId { get; set; }

    public string Kind { get; set; } = string.Empty;

    public DateOnly Date { get; set; }

    public string Payload { get; set; } = string.Empty;
}

internal sealed class CareerEventConfiguration : IEntityTypeConfiguration<CareerEventRow>
{
    public void Configure(EntityTypeBuilder<CareerEventRow> b)
    {
        b.ToTable("CareerEvents");
        b.HasKey(e => e.Id);
        b.HasIndex(e => e.EventId).IsUnique();
        b.HasIndex(e => e.AffairId);
        b.Property(e => e.Kind).IsRequired().HasMaxLength(40);
        b.Property(e => e.Payload).IsRequired();
        b.HasOne<Affair>().WithMany().HasForeignKey(e => e.AffairId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class LedgerSnapshotConfiguration : IEntityTypeConfiguration<LedgerSnapshot>
{
    public void Configure(EntityTypeBuilder<LedgerSnapshot> b)
    {
        b.ToTable("LedgerSnapshots");
        b.HasKey(s => s.Id);
        b.Ignore(s => s.IsTransient);
        b.Property(s => s.Purpose).HasConversion<string>().HasMaxLength(20);
        b.Property(s => s.Hash).IsRequired().HasMaxLength(64);
        b.Property(s => s.TakenBy).IsRequired().HasMaxLength(100);
        b.Property(s => s.Total).HasConversion(new ValueConverter<Money, long>(m => m.Millimes, v => Money.FromMillimes(v))).HasColumnName("TotalMillimes");
        b.Property(s => s.RuleEventIds).IsRequired()
            .HasConversion(new ValueConverter<IReadOnlyList<Guid>, string>(ids => EventStorage.Ids(ids), s => EventStorage.Ids(s)))
            .Metadata.SetValueComparer(EventStorage.ByReference<IReadOnlyList<Guid>>());
        b.Property(s => s.CareerEvents).IsRequired()
            .HasConversion(new ValueConverter<IReadOnlyList<CareerEvent>, string>(e => EventStorage.Career(e), s => EventStorage.Career(s)))
            .Metadata.SetValueComparer(EventStorage.ByReference<IReadOnlyList<CareerEvent>>());
        b.ComplexProperty(s => s.Settings, p =>
        {
            p.Property(x => x.Proration).HasColumnName("Proration").HasConversion<string>().HasMaxLength(20);
            p.Property(x => x.Rounding).HasColumnName("Rounding").HasConversion<string>().HasMaxLength(20);
            p.Property(x => x.ResetTimeInDegreeOnReclassification).HasColumnName("ResetTimeInDegreeOnReclassification");
            p.Property(x => x.AbsenceSuspendsSeniority).HasColumnName("AbsenceSuspendsSeniority");
            p.Ignore(x => x.Career);
        });
        b.HasOne<Affair>().WithMany().HasForeignKey(s => s.AffairId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(s => s.AffairId);
    }
}

internal static class EventStorage
{
    /// <summary>Stored values are immutable and replaced as a whole, so a change is a new reference.</summary>
    public static ValueComparer<T> ByReference<T>() where T : class =>
        new((a, b) => ReferenceEquals(a, b), v => v.GetHashCode(), v => v);

    public static string Ids(IReadOnlyList<Guid> ids) => JsonSerializer.Serialize(ids);

    public static IReadOnlyList<Guid> Ids(string json) => JsonSerializer.Deserialize<List<Guid>>(json) ?? [];

    public static string Career(IReadOnlyList<CareerEvent> events) =>
        "[" + string.Join(",", events.Select(EventJson.Serialize)) + "]";

    public static IReadOnlyList<CareerEvent> Career(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().Select(e => EventJson.DeserializeCareerEvent(e.GetRawText())).ToList();
    }
}
