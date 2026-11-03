using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.Rules;
using static PayrollCalculator.Domain.Tests.RuleFixtures;

namespace PayrollCalculator.Domain.Tests;

/// <summary>05.11: the review lifecycle of a rule event.</summary>
public class RecordedRuleEventTests
{
    private static readonly DateTime Now = new(2026, 11, 3, 10, 0, 0, DateTimeKind.Utc);

    private static RecordedRuleEvent Draft(RuleEvent? e = null) => RecordedRuleEvent.Draft(1, e ?? Declare(D(2000)), "mcp", Now);

    [Fact]
    public void A_new_event_is_a_draft_that_copies_its_kind_date_and_source()
    {
        var recorded = Draft(Enact(D(2024, 5), ("C1", 1, 300m)));

        Assert.Equal(RuleEventStatus.Draft, recorded.Status);
        Assert.Equal((RuleEventKind.SalaryGridEnacted, D(2024, 5), 1), (recorded.Kind, recorded.EffectiveFrom, recorded.LegalSourceId));
        Assert.Equal(recorded.Event.Id, recorded.Id);
        Assert.False(recorded.IsInForce);
    }

    [Fact]
    public void A_draft_is_approved_by_a_reviewer()
    {
        var recorded = Draft();

        recorded.Approve("admin", Now.AddHours(1));

        Assert.Equal((RuleEventStatus.Approved, "admin", Now.AddHours(1)), (recorded.Status, recorded.ReviewedBy, recorded.ReviewedAt));
        Assert.True(recorded.IsInForce);
    }

    [Fact]
    public void Approving_needs_a_reviewer()
    {
        var error = Assert.Throws<DomainException>(() => Draft().Approve(" ", Now));

        Assert.Equal((DomainError.Required, DomainField.Reviewer), (error.Error, error.Field));
    }

    [Fact]
    public void A_draft_can_be_revised_but_not_an_approved_event()
    {
        var first = Enact(D(2024, 5), ("C1", 1, 300m));
        var recorded = Draft(first);
        var revised = new SalaryGridEnacted(D(2024, 5), Cite(2), Workers, Cells(("C1", 1, 310m)), first.Id);

        recorded.Revise(revised);
        recorded.Approve("admin", Now);

        Assert.Same(revised, recorded.Event);
        var error = Assert.Throws<DomainException>(() => recorded.Revise(revised));
        Assert.Equal(DomainError.ImmutableOnceApproved, error.Error);
    }

    [Fact]
    public void A_revision_must_keep_the_event_id()
    {
        var recorded = Draft();

        Assert.Throws<DomainException>(() => recorded.Revise(Declare(D(2001))));
    }

    [Fact]
    public void Only_an_approved_event_can_be_retracted_and_only_a_draft_rejected()
    {
        var approved = Draft();
        approved.Approve("admin", Now);
        approved.Retract("admin", Now, "remplacé par le rectificatif");
        var rejected = Draft();
        rejected.Reject("admin", Now, "lecture erronée");

        Assert.Equal((RuleEventStatus.Retracted, "remplacé par le rectificatif"), (approved.Status, approved.Note));
        Assert.Equal(RuleEventStatus.Rejected, rejected.Status);
        Assert.Equal(DomainError.InvalidStatusTransition, Assert.Throws<DomainException>(() => Draft().Retract("admin", Now, null)).Error);
        Assert.Throws<DomainException>(() => rejected.Approve("admin", Now));
        Assert.Throws<DomainException>(() => approved.Approve("admin", Now));
    }

    [Fact]
    public void The_rulebook_keeps_only_approved_events_in_recorded_order()
    {
        var declared = Draft(Declare(D(2000)));
        var enacted = RecordedRuleEvent.Draft(1, Enact(D(2000), ("C1", 1, 300m)), "mcp", Now.AddMinutes(1));
        var draftRaise = RecordedRuleEvent.Draft(1, Enact(D(2001), ("C1", 1, 999m)), "mcp", Now.AddMinutes(2));
        declared.Approve("admin", Now);
        enacted.Approve("admin", Now);

        var rulebook = new[] { draftRaise, enacted, declared }.ToRulebook();

        Assert.Equal([declared.Id, enacted.Id], rulebook.Events.Select(e => e.Id));
        Assert.Equal(M(300m), rulebook.At(D(2002)).Line(Workers)!.Grid!.AmountFor(C1, 1));
    }
}
