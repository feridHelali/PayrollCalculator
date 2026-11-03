using PayrollCalculator.Application.SalaryTables;
using PayrollCalculator.Domain.Common;

namespace PayrollCalculator.Application.Tests;

public class GridCsvTests
{
    [Fact]
    public void A_comma_separated_grid_gives_degrees_years_and_rows()
    {
        var grid = GridCsv.Parse("﻿category,1,2,3\r\nyears_in_degree,1,1,2\r\nI,2.850,2.896,2.899\r\nII,3.033,,3.043\r\n");

        Assert.Equal([1, 2, 3], grid.Degrees);
        Assert.Equal([1, 1, 2], grid.YearsInDegree);
        Assert.Equal(["I", "II"], grid.Rows.Select(r => r.Category));
        Assert.Equal([2.850m, 2.896m, 2.899m], grid.Rows[0].Amounts);
        Assert.Null(grid.Rows[1].Amounts[1]);
    }

    [Fact]
    public void A_semicolon_grid_accepts_decimal_commas_and_spaced_thousands()
    {
        var grid = GridCsv.Parse("الصنف;1;2\nمدة البقاء;1;1\n1أ;595,401;1 204,815\n");

        Assert.Equal([595.401m, 1204.815m], grid.Rows[0].Amounts);
        Assert.Equal("1أ", grid.Rows[0].Category);
    }

    [Theory]
    [InlineData("category,1,2\nyears,1,1\n", "3")]                // no category row
    [InlineData("category,1,1\nyears,1,1\nI,1,2\n", "1")]         // a degree twice
    [InlineData("category,1,2\nyears,1\nI,1,2\n", "2")]           // years row too short
    [InlineData("category,1,2\nyears,1,1\nI,1,abc\n", "3")]       // not a number
    [InlineData("category,1,2\nyears,1,1\nI,1,2\nII,1\n", "4")]   // row too short
    [InlineData("category,1,2\nyears,1,1\nI,-1,2\n", "3")]        // negative amount
    public void A_malformed_file_names_the_line_to_fix(string text, string line)
    {
        var error = Assert.Throws<DomainException>(() => GridCsv.Parse(text));

        Assert.Equal((DomainError.InvalidImportFile, DomainField.ImportFile, line), (error.Error, error.Field, error.Detail));
    }
}
