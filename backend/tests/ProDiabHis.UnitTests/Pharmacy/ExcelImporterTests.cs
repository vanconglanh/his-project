using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using ProDiabHis.Application.Common;
using ProDiabHis.Infrastructure.Pharmacy;
using System.IO;
using Xunit;

namespace ProDiabHis.UnitTests.Pharmacy;

public class ExcelImporterTests
{
    private static Stream CreateValidExcel(IEnumerable<(string code, string nameVi, string unit, string form)> rows)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("Drugs");
        // Header
        ws.Cell(1, 1).Value = "code";
        ws.Cell(1, 2).Value = "name_vi";
        ws.Cell(1, 3).Value = "name_en";
        ws.Cell(1, 4).Value = "generic_name";
        ws.Cell(1, 5).Value = "atc_code";
        ws.Cell(1, 6).Value = "strength";
        ws.Cell(1, 7).Value = "unit";
        ws.Cell(1, 8).Value = "form";
        ws.Cell(1, 9).Value = "manufacturer";
        ws.Cell(1, 10).Value = "country";
        ws.Cell(1, 11).Value = "price";
        ws.Cell(1, 12).Value = "requires_prescription";
        ws.Cell(1, 13).Value = "is_psychotropic";
        ws.Cell(1, 14).Value = "is_narcotic";

        int row = 2;
        foreach (var (code, nameVi, unit, form) in rows)
        {
            ws.Cell(row, 1).Value = code;
            ws.Cell(row, 2).Value = nameVi;
            ws.Cell(row, 7).Value = unit;
            ws.Cell(row, 8).Value = form;
            ws.Cell(row, 12).Value = 1;
            row++;
        }

        var ms = new MemoryStream();
        wb.SaveAs(ms);
        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void ClosedXmlImporter_is_constructable()
    {
        var db = Substitute.For<IDapperConnectionFactory>();
        var importer = new ClosedXmlImporter(db, NullLogger<ClosedXmlImporter>.Instance);
        importer.Should().NotBeNull();
    }

    [Fact]
    public void CreateValidExcel_produces_readable_workbook()
    {
        var stream = CreateValidExcel([("TH001", "Thuoc A", "vien", "TABLET")]);
        stream.Length.Should().BeGreaterThan(0);

        var wb = new XLWorkbook(stream);
        var ws = wb.Worksheets.First();
        ws.Cell(2, 1).GetString().Should().Be("TH001");
        ws.Cell(2, 2).GetString().Should().Be("Thuoc A");
    }

    [Fact]
    public async Task Importer_throws_on_invalid_format()
    {
        var db = Substitute.For<IDapperConnectionFactory>();
        var importer = new ClosedXmlImporter(db, NullLogger<ClosedXmlImporter>.Instance);

        var invalidStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is not excel"));

        var act = async () => await importer.ImportDrugsAsync(invalidStream, "UPSERT", 1, 0, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*DRUG_IMPORT_INVALID_FORMAT*");
    }

    [Fact]
    public void DrugImportError_row_and_message()
    {
        var err = new Application.Pharmacy.DrugImportError(5, "Ma thuoc khong duoc de trong.");
        err.Row.Should().Be(5);
        err.Message.Should().Contain("Ma thuoc");
    }
}
