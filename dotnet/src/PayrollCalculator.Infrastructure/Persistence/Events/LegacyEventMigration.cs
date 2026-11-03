using System.Globalization;
using Microsoft.EntityFrameworkCore;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Migration;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Infrastructure.Persistence.Events;

/// <summary>What the migration to events did.</summary>
public sealed record EventMigrationReport(int Agreements, int RuleEvents, int Careers, IReadOnlyList<TranslationWarning> Warnings, IReadOnlyList<string> Failures);

/// <summary>
/// 05.12: writes the salary tables and affairs of the first model as events. Idempotent: an agreement that already has
/// rule events, or an affair that already has career events, is left alone. The old tables are not changed.
/// </summary>
/// <remarks>
/// The old data has no legal source, so each agreement gets one placeholder source ("legacy-{id}") that every
/// migrated event cites. The events are approved by "migration", since the data was already in use; a reviewer can
/// retract and replace them once the real texts are entered.
/// </remarks>
public sealed class LegacyEventMigration(PayrollDbContext db, TimeProvider clock)
{
    public const string Actor = "migration";

    public async Task<EventMigrationReport> MigrateAsync(CancellationToken cancellationToken = default)
    {
        var warnings = new List<TranslationWarning>();
        var failures = new List<string>();
        var now = clock.GetUtcNow().UtcDateTime;
        int agreements = 0, ruleEvents = 0, careers = 0;

        var withEvents = (await db.RuleEvents.Select(e => e.AgreementId).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();
        var tables = await db.SalaryTables.ToListAsync(cancellationToken).ConfigureAwait(false);
        foreach (var agreement in await db.Agreements.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            var own = tables.Where(t => t.AgreementId == agreement.Id).ToList();
            if (own.Count == 0 || withEvents.Contains(agreement.Id))
            {
                continue;
            }

            var source = LegalSource.Register(LegalSourceKind.Agreement, string.Create(CultureInfo.InvariantCulture, $"legacy-{agreement.Id}"),
                agreement.Name + " (جداول مدخلة في التطبيق القديم)", own.Min(t => t.ApplicationPeriod.Start), null, null);
            db.LegalSources.Add(source);
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var translated = new List<TranslationWarning>();
            IReadOnlyList<RuleEvent> events;
            try
            {
                events = LegacyTranslator.RuleEvents(own, new Citation(source.Id, 1), translated);
                _ = Rulebook.From(events); // the events must fold before they are stored
            }
            catch (DomainException error)
            {
                failures.Add($"{agreement.Name}: {error.Message}");
                db.LegalSources.Remove(source);
                await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                continue;
            }

            var at = now;
            foreach (var e in events)
            {
                var recorded = RecordedRuleEvent.Draft(agreement.Id, e, Actor, at = at.AddTicks(1));
                recorded.Approve(Actor, now);
                db.RuleEvents.Add(recorded);
            }

            warnings.AddRange(translated);
            agreements++;
            ruleEvents += events.Count;
        }

        var store = new CareerEventStore(db);
        var withCareer = (await db.Set<CareerEventRow>().Select(r => r.AffairId).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)).ToHashSet();
        foreach (var affair in await db.Affairs.ToListAsync(cancellationToken).ConfigureAwait(false))
        {
            if (withCareer.Contains(affair.Id))
            {
                continue;
            }

            var career = LegacyTranslator.CareerEvents(affair, tables.FirstOrDefault(t => t.Id == affair.SalaryTableId), warnings);
            if (career is null)
            {
                continue;
            }

            foreach (var e in career)
            {
                store.Add(affair.Id, e);
            }

            careers++;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new EventMigrationReport(agreements, ruleEvents, careers, warnings, failures);
    }
}
