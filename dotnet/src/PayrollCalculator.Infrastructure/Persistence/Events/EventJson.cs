using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using PayrollCalculator.Domain.Careers;
using PayrollCalculator.Domain.Common;
using PayrollCalculator.Domain.LegalSources;
using PayrollCalculator.Domain.Rules;

namespace PayrollCalculator.Infrastructure.Persistence.Events;

/// <summary>
/// The stored form of rule and career events: one JSON object per event, with a <c>schema</c> version and a <c>kind</c>.
/// Written by hand (no reflection) so that the format is explicit, stable across refactorings of the domain,
/// and readable by the MCP server. Amounts are in dinars with three decimals at most.
/// </summary>
/// <remarks>To change the format: bump <see cref="CurrentSchema"/> and upgrade the older objects in <see cref="Upgrade"/>.</remarks>
public static class EventJson
{
    public const int CurrentSchema = 1;

    private static readonly JsonSerializerOptions Compact = new() { WriteIndented = false };

    // ---- rule events ----

    public static string Serialize(RuleEvent ruleEvent)
    {
        ArgumentNullException.ThrowIfNull(ruleEvent);
        var json = Envelope(ruleEvent.Kind.ToString(), ruleEvent.Id);
        json["effectiveFrom"] = Date(ruleEvent.EffectiveFrom);
        json["source"] = new JsonObject
        {
            ["legalSourceId"] = ruleEvent.Source.LegalSourceId,
            ["page"] = ruleEvent.Source.Page,
            ["article"] = ruleEvent.Source.Article,
        };

        switch (ruleEvent)
        {
            case GridLineDeclared e:
                json["line"] = e.Line.Value;
                json["name"] = e.Name;
                json["unit"] = e.Unit.ToString();
                json["categories"] = new JsonArray(e.Categories.Select(c => (JsonNode)new JsonObject { ["code"] = c.Code.Value, ["label"] = c.Label }).ToArray());
                break;
            case SalaryGridEnacted e:
                json["line"] = e.Line.Value;
                json["cells"] = Cells(e.Cells);
                break;
            case SalaryGridRevalued e:
                json["line"] = e.Line.Value;
                json["rule"] = Rule(e.Rule);
                json["rounding"] = e.Rounding.ToString();
                json["published"] = Cells(e.Published);
                break;
            case CareerRuleChanged e:
                json["line"] = e.Line.Value;
                var months = new JsonObject();
                foreach (var (degree, count) in e.MonthsInDegree.OrderBy(p => p.Key))
                {
                    months[degree.ToString(CultureInfo.InvariantCulture)] = count;
                }

                json["monthsInDegree"] = months;
                break;
            case WorkingTimeChanged e:
                json["regime"] = e.Regime.Code.Value;
                json["hoursPerMonth"] = e.Regime.HoursPerMonth;
                break;
            case AllowanceIntroduced e:
                json["code"] = e.Code.Value;
                json["name"] = e.Name;
                json["basis"] = Basis(e.Basis);
                break;
            case AllowanceRevalued e:
                json["code"] = e.Code.Value;
                json["published"] = e.Published is null ? null : Basis(e.Published);
                json["percent"] = e.Percentage?.Value;
                json["rounding"] = e.Rounding.ToString();
                break;
            case AllowanceAbolished e:
                json["code"] = e.Code.Value;
                break;
            default:
                throw new NotSupportedException($"No stored form for {ruleEvent.GetType().Name}.");
        }

        return json.ToJsonString(Compact);
    }

    public static RuleEvent DeserializeRuleEvent(string payload)
    {
        var json = Open(payload);
        var id = json["id"]!.GetValue<Guid>();
        var effectiveFrom = ReadDate(json["effectiveFrom"]);
        var s = json["source"]!.AsObject();
        var source = new Citation(s["legalSourceId"]!.GetValue<int>(), s["page"]!.GetValue<int>(), s["article"]?.GetValue<string>());

        return Enum.Parse<RuleEventKind>(Text(json, "kind")) switch
        {
            RuleEventKind.GridLineDeclared => new GridLineDeclared(effectiveFrom, source, new GridLineCode(Text(json, "line")), Text(json, "name"),
                Enum.Parse<PayUnit>(Text(json, "unit")),
                json["categories"]!.AsArray().Select(c => new CategoryDefinition(new CategoryCode(Text(c!, "code")), Text(c!, "label"))), id),
            RuleEventKind.SalaryGridEnacted => new SalaryGridEnacted(effectiveFrom, source, new GridLineCode(Text(json, "line")), ReadCells(json["cells"]), id),
            RuleEventKind.SalaryGridRevalued => new SalaryGridRevalued(effectiveFrom, source, new GridLineCode(Text(json, "line")),
                ReadRule(json["rule"]!.AsObject()), Enum.Parse<RoundingRule>(Text(json, "rounding")), ReadCells(json["published"]), id),
            RuleEventKind.CareerRuleChanged => new CareerRuleChanged(effectiveFrom, source, new GridLineCode(Text(json, "line")),
                json["monthsInDegree"]!.AsObject().ToDictionary(p => int.Parse(p.Key, CultureInfo.InvariantCulture), p => p.Value!.GetValue<int>()), id),
            RuleEventKind.WorkingTimeChanged => new WorkingTimeChanged(effectiveFrom, source, new RegimeCode(Text(json, "regime")),
                json["hoursPerMonth"]!.GetValue<decimal>(), id),
            RuleEventKind.AllowanceIntroduced => new AllowanceIntroduced(effectiveFrom, source, new ComponentCode(Text(json, "code")), Text(json, "name"),
                ReadBasis(json["basis"]!.AsObject()), id),
            RuleEventKind.AllowanceRevalued => new AllowanceRevalued(effectiveFrom, source, new ComponentCode(Text(json, "code")),
                json["published"] is JsonObject published ? ReadBasis(published) : null,
                json["percent"] is { } percent ? new Percentage(percent.GetValue<decimal>()) : null,
                Enum.Parse<RoundingRule>(Text(json, "rounding")), id),
            RuleEventKind.AllowanceAbolished => new AllowanceAbolished(effectiveFrom, source, new ComponentCode(Text(json, "code")), id),
            var kind => throw new NotSupportedException($"Unknown rule event kind {kind}."),
        };
    }

    // ---- career events ----

    public static string Serialize(CareerEvent careerEvent)
    {
        ArgumentNullException.ThrowIfNull(careerEvent);
        var json = Envelope(CareerKind(careerEvent), careerEvent.Id);
        json["date"] = Date(careerEvent.Date);
        switch (careerEvent)
        {
            case Hired e:
                json["line"] = e.Line.Value;
                json["category"] = e.Category.Value;
                json["degree"] = e.Degree;
                json["regime"] = e.Regime.Value;
                json["monthsAlreadyInDegree"] = e.MonthsAlreadyInDegree;
                break;
            case Reclassified e:
                json["category"] = e.Category.Value;
                json["degree"] = e.Degree;
                break;
            case DegreeOverridden e:
                json["degree"] = e.Degree;
                break;
            case RegimeChanged e:
                json["regime"] = e.Regime.Value;
                break;
            case UnpaidAbsence e:
                json["end"] = Date(e.Period.End!.Value);
                break;
            case Separated:
                break;
        }

        return json.ToJsonString(Compact);
    }

    public static string CareerKind(CareerEvent careerEvent) => careerEvent switch
    {
        Hired => "Hired",
        Reclassified => "Reclassified",
        DegreeOverridden => "DegreeOverridden",
        RegimeChanged => "RegimeChanged",
        UnpaidAbsence => "UnpaidAbsence",
        Separated => "Separated",
        _ => throw new NotSupportedException($"No stored form for {careerEvent?.GetType().Name}."),
    };

    public static CareerEvent DeserializeCareerEvent(string payload)
    {
        var json = Open(payload);
        var id = json["id"]!.GetValue<Guid>();
        var date = ReadDate(json["date"]);
        return Text(json, "kind") switch
        {
            "Hired" => new Hired(date, new GridLineCode(Text(json, "line")), new CategoryCode(Text(json, "category")), json["degree"]!.GetValue<int>(),
                new RegimeCode(Text(json, "regime")), json["monthsAlreadyInDegree"]?.GetValue<int>() ?? 0, id),
            "Reclassified" => new Reclassified(date, new CategoryCode(Text(json, "category")), json["degree"]!.GetValue<int>(), id),
            "DegreeOverridden" => new DegreeOverridden(date, json["degree"]!.GetValue<int>(), id),
            "RegimeChanged" => new RegimeChanged(date, new RegimeCode(Text(json, "regime")), id),
            "UnpaidAbsence" => new UnpaidAbsence(new DateRange(date, ReadDate(json["end"])), id),
            "Separated" => new Separated(date, id),
            var kind => throw new NotSupportedException($"Unknown career event kind {kind}."),
        };
    }

    // ---- versioning ----

    private static JsonObject Envelope(string kind, Guid id) => new()
    {
        ["schema"] = CurrentSchema,
        ["kind"] = kind,
        ["id"] = id,
    };

    private static JsonObject Open(string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        var json = JsonNode.Parse(payload)?.AsObject() ?? throw new JsonException("The event payload is not an object.");
        var schema = json["schema"]?.GetValue<int>() ?? throw new JsonException("The event payload has no schema version.");
        return Upgrade(json, schema);
    }

    /// <summary>Brings a stored object up to <see cref="CurrentSchema"/>. There is one schema so far.</summary>
    private static JsonObject Upgrade(JsonObject json, int schema) => schema switch
    {
        CurrentSchema => json,
        _ => throw new NotSupportedException(string.Create(CultureInfo.InvariantCulture,
            $"Event schema {schema} is not supported (this version reads up to {CurrentSchema}).")),
    };

    // ---- parts ----

    private static JsonArray Cells(IEnumerable<GridCell> cells) =>
        new(cells.Select(c => (JsonNode)new JsonObject { ["category"] = c.Category.Value, ["degree"] = c.Degree, ["amount"] = c.Amount.Dinars }).ToArray());

    private static List<GridCell> ReadCells(JsonNode? cells) =>
        cells is null
            ? []
            : cells.AsArray().Select(c => new GridCell(new CategoryCode(Text(c!, "category")), c!["degree"]!.GetValue<int>(), ReadMoney(c["amount"]))).ToList();

    private static JsonObject Rule(RevaluationRule rule) => rule switch
    {
        PercentForAll r => new JsonObject { ["type"] = "percentForAll", ["percent"] = r.Percentage.Value },
        PercentByCategory r => new JsonObject
        {
            ["type"] = "percentByCategory",
            ["percents"] = new JsonObject(r.Percentages.OrderBy(p => p.Key.Value, StringComparer.Ordinal)
                .Select(p => KeyValuePair.Create(p.Key.Value, (JsonNode?)p.Value.Value))),
        },
        FixedIncrease r => new JsonObject { ["type"] = "fixedIncrease", ["amount"] = r.Increase.Dinars },
        _ => throw new NotSupportedException($"No stored form for {rule.GetType().Name}."),
    };

    private static RevaluationRule ReadRule(JsonObject rule) => Text(rule, "type") switch
    {
        "percentForAll" => new PercentForAll(new Percentage(rule["percent"]!.GetValue<decimal>())),
        "percentByCategory" => new PercentByCategory(rule["percents"]!.AsObject()
            .ToDictionary(p => new CategoryCode(p.Key), p => new Percentage(p.Value!.GetValue<decimal>()))),
        "fixedIncrease" => new FixedIncrease(ReadMoney(rule["amount"])),
        var type => throw new NotSupportedException($"Unknown revaluation rule {type}."),
    };

    private static JsonObject Basis(AllowanceBasis basis) => basis switch
    {
        FixedMonthly b => new JsonObject { ["type"] = "fixedMonthly", ["amount"] = b.Value.Dinars },
        PerHour b => new JsonObject { ["type"] = "perHour", ["rate"] = b.Rate.Dinars },
        PercentOfBase b => new JsonObject { ["type"] = "percentOfBase", ["percent"] = b.Percentage.Value },
        SeniorityBrackets b => new JsonObject
        {
            ["type"] = "seniorityBrackets",
            ["brackets"] = new JsonArray(b.Brackets.Select(x => (JsonNode)new JsonObject { ["fromYears"] = x.FromYears, ["percent"] = x.Percentage.Value }).ToArray()),
        },
        _ => throw new NotSupportedException($"No stored form for {basis.GetType().Name}."),
    };

    private static AllowanceBasis ReadBasis(JsonObject basis) => Text(basis, "type") switch
    {
        "fixedMonthly" => new FixedMonthly(ReadMoney(basis["amount"])),
        "perHour" => new PerHour(ReadMoney(basis["rate"])),
        "percentOfBase" => new PercentOfBase(new Percentage(basis["percent"]!.GetValue<decimal>())),
        "seniorityBrackets" => new SeniorityBrackets(basis["brackets"]!.AsArray()
            .Select(x => new SeniorityBracket(x!["fromYears"]!.GetValue<int>(), new Percentage(x["percent"]!.GetValue<decimal>())))),
        var type => throw new NotSupportedException($"Unknown allowance basis {type}."),
    };

    private static string Date(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateOnly ReadDate(JsonNode? node) =>
        DateOnly.ParseExact(node?.GetValue<string>() ?? throw new JsonException("A date is missing."), "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static Money ReadMoney(JsonNode? node) => Money.FromDinars(node?.GetValue<decimal>() ?? throw new JsonException("An amount is missing."));

    private static string Text(JsonNode node, string name) =>
        node[name]?.GetValue<string>() ?? throw new JsonException($"The field '{name}' is missing.");
}
