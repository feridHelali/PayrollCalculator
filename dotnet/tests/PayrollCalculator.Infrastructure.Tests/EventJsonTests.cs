using System.Text.Json;
using System.Text.Json.Nodes;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;
using PayrollCalculator.Infrastructure.Persistence.Events;

namespace PayrollCalculator.Infrastructure.Tests;

/// <summary>05.11: the stored form of every event type reads back to the same event.</summary>
public class EventJsonTests
{
    private static readonly GridLineCode Line = new("WORKERS");
    private static readonly CategoryCode C1 = new("C1");
    private static readonly Citation Source = new(3, 12, "2");
    private static readonly DateOnly Day = new(2024, 5, 1);

    private static Money M(decimal dinars) => Money.FromDinars(dinars);

    private static GridCell Cell(string category, int degree, decimal amount) => new(new CategoryCode(category), degree, M(amount));

    public static TheoryData<RuleEvent> RuleEvents() =>
    [
        new GridLineDeclared(Day, Source, Line, "العملة", PayUnit.Hourly, [new CategoryDefinition(C1, "الصنف 1"), new CategoryDefinition(new("C2"), "الصنف 2")]),
        new SalaryGridEnacted(Day, Source, Line, [Cell("C1", 1, 1.875m), Cell("C1", 2, 1.9m)]),
        new SalaryGridRevalued(Day, Source, Line, new PercentForAll(new Percentage(6.5m)), RoundingRule.Truncate, [Cell("C1", 1, 81.462m)]),
        new SalaryGridRevalued(Day, Source, Line, new PercentByCategory(new Dictionary<CategoryCode, Percentage> { [C1] = new(11.6m), [new("C2")] = new(9.67m) }), RoundingRule.HalfUp),
        new SalaryGridRevalued(Day, Source, Line, new FixedIncrease(M(5.25m)), RoundingRule.HalfUp),
        new CareerRuleChanged(Day, Source, Line, new Dictionary<int, int> { [1] = 12, [2] = 24 }),
        new WorkingTimeChanged(Day, Source, new RegimeCode("H48"), 208m),
        new AllowanceIntroduced(Day, Source, new ComponentCode("TRANSPORT"), "منحة النقل", new FixedMonthly(M(76.491m))),
        new AllowanceIntroduced(Day, Source, new ComponentCode("HOURLY"), "منحة", new PerHour(M(0.125m))),
        new AllowanceIntroduced(Day, Source, new ComponentCode("MEAL"), "منحة القفة", new PerDayWorked(M(2.5m), 26)),
        new AllowanceIntroduced(Day, Source, new ComponentCode("OUTPUT"), "منحة الإنتاج", new PercentOfBase(new Percentage(10))),
        new AllowanceIntroduced(Day, Source, new ComponentCode("SENIORITY"), "منحة الأقدمية",
            new SeniorityBrackets([new SeniorityBracket(5, new Percentage(5)), new SeniorityBracket(10, new Percentage(10))])),
        new AllowanceRevalued(Day, Source, new ComponentCode("TRANSPORT"), new FixedMonthly(M(81.462m)), new Percentage(6.5m), RoundingRule.Truncate),
        new AllowanceRevalued(Day, Source, new ComponentCode("TRANSPORT"), null, new Percentage(6.5m)),
        new AllowanceAbolished(Day, new Citation(3, 1), new ComponentCode("TRANSPORT")),
    ];

    [Theory]
    [MemberData(nameof(RuleEvents))]
    public void Every_rule_event_type_round_trips(RuleEvent original)
    {
        var json = EventJson.Serialize(original);
        var read = EventJson.DeserializeRuleEvent(json);

        Assert.Equal(original.GetType(), read.GetType());
        Assert.Equal((original.Id, original.EffectiveFrom, original.Source, original.Kind), (read.Id, read.EffectiveFrom, read.Source, read.Kind));
        Assert.Equal(json, EventJson.Serialize(read));
    }

    public static TheoryData<CareerEvent> CareerEvents() =>
    [
        new Hired(new DateOnly(2000, 1, 15), Line, C1, 2, new RegimeCode("H48"), 6),
        new Reclassified(new DateOnly(2005, 3, 1), new CategoryCode("C2"), 1),
        new DegreeOverridden(new DateOnly(2006, 1, 1), 4),
        new RegimeChanged(new DateOnly(2007, 1, 1), new RegimeCode("H40")),
        new UnpaidAbsence(new DateRange(new DateOnly(2008, 1, 1), new DateOnly(2008, 2, 15))),
        new Separated(new DateOnly(2010, 12, 31)),
    ];

    [Theory]
    [MemberData(nameof(CareerEvents))]
    public void Every_career_event_type_round_trips(CareerEvent original)
    {
        var json = EventJson.Serialize(original);
        var read = EventJson.DeserializeCareerEvent(json);

        Assert.Equal(original, read);
        Assert.Equal(json, EventJson.Serialize(read));
    }

    [Fact]
    public void The_stored_form_is_versioned_readable_json_with_amounts_in_dinars()
    {
        var json = JsonNode.Parse(EventJson.Serialize(new SalaryGridEnacted(Day, Source, Line, [Cell("C1", 1, 81.462m)])))!;

        Assert.Equal(EventJson.CurrentSchema, json["schema"]!.GetValue<int>());
        Assert.Equal("SalaryGridEnacted", json["kind"]!.GetValue<string>());
        Assert.Equal("2024-05-01", json["effectiveFrom"]!.GetValue<string>());
        Assert.Equal(81.462m, json["cells"]![0]!["amount"]!.GetValue<decimal>());
    }

    [Fact]
    public void A_newer_or_missing_schema_is_refused_rather_than_misread()
    {
        var json = JsonNode.Parse(EventJson.Serialize(new AllowanceAbolished(Day, Source, new ComponentCode("X"))))!.AsObject();
        json["schema"] = EventJson.CurrentSchema + 1;
        var newer = json.ToJsonString();
        json.Remove("schema");
        var unversioned = json.ToJsonString();

        Assert.Throws<NotSupportedException>(() => EventJson.DeserializeRuleEvent(newer));
        Assert.Throws<JsonException>(() => EventJson.DeserializeRuleEvent(unversioned));
    }

    [Fact]
    public void A_stored_amount_with_more_than_three_decimals_is_refused()
    {
        var json = EventJson.Serialize(new SalaryGridEnacted(Day, Source, Line, [Cell("C1", 1, 1.5m)])).Replace("1.5", "1.5004", StringComparison.Ordinal);

        Assert.Throws<DomainException>(() => EventJson.DeserializeRuleEvent(json));
    }
}
