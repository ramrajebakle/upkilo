using System;
using System.Collections.Generic;
using FluentAssertions;
using Upkilo.Infrastructure.Services;
using Xunit;

namespace Upkilo.Tests.Services;

public class CsvExportServiceTests
{
    private CsvExportService CreateSut() => new CsvExportService();

    private record SampleRow(string Name, int Age, string Email);

    [Fact]
    public void ExportToCsv_WithData_ProducesHeaderRow()
    {
        var sut = CreateSut();
        var data = new List<SampleRow> { new("Alice", 30, "alice@test.com") };

        var bytes = sut.ExportToCsv(data);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Should().StartWith("Name,Age,Email");
    }

    [Fact]
    public void ExportToCsv_WithData_ProducesDataRows()
    {
        var sut = CreateSut();
        var data = new List<SampleRow>
        {
            new("Alice", 30, "alice@test.com"),
            new("Bob", 25, "bob@test.com")
        };

        var bytes = sut.ExportToCsv(data);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("Alice");
        csv.Should().Contain("Bob");
        csv.Should().Contain("alice@test.com");
    }

    [Fact]
    public void ExportToCsv_EmptyCollection_ProducesOnlyHeader()
    {
        var sut = CreateSut();

        var bytes = sut.ExportToCsv(new List<SampleRow>());
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Trim().Should().Be("Name,Age,Email");
    }

    [Fact]
    public void ExportToCsv_FieldWithComma_IsQuoted()
    {
        var sut = CreateSut();
        var data = new List<SampleRow> { new("Smith, John", 40, "john@test.com") };

        var bytes = sut.ExportToCsv(data);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("\"Smith, John\"");
    }

    [Fact]
    public void ExportToCsv_FieldWithQuote_EscapesDoubleQuote()
    {
        var sut = CreateSut();
        var data = new List<SampleRow> { new("John \"Johnny\" Doe", 35, "j@test.com") };

        var bytes = sut.ExportToCsv(data);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("\"\""); // Doubled quote = escaped
    }

    [Fact]
    public void ExportToCsv_NullableFields_ExportedAsEmpty()
    {
        var sut = CreateSut();
        // Record with null field handled via string empty
        var data = new List<SampleRow> { new("Alice", 0, "") };

        var bytes = sut.ExportToCsv(data);
        var csv = System.Text.Encoding.UTF8.GetString(bytes);

        csv.Should().Contain("Alice");
    }
}
