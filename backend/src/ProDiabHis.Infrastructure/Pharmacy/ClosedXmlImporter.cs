using System.Data;
using ClosedXML.Excel;
using Dapper;
using Microsoft.Extensions.Logging;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Pharmacy;

namespace ProDiabHis.Infrastructure.Pharmacy;

/// <summary>
/// Excel importer for drug master using ClosedXML.
/// Expected columns (row 1 = header):
///   A: code, B: name_vi, C: name_en, D: generic_name, E: atc_code,
///   F: strength, G: unit, H: form, I: manufacturer, J: country,
///   K: price, L: requires_prescription (1/0), M: is_psychotropic (1/0), N: is_narcotic (1/0)
/// </summary>
public class ClosedXmlImporter : IExcelImporter
{
    private readonly IDapperConnectionFactory _db;
    private readonly ILogger<ClosedXmlImporter> _logger;

    public ClosedXmlImporter(IDapperConnectionFactory db, ILogger<ClosedXmlImporter> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DrugImportResult> ImportDrugsAsync(Stream excelStream, string mode, int tenantId, int userId, CancellationToken ct = default)
    {
        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(excelStream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Invalid Excel file");
            throw new InvalidOperationException("DRUG_IMPORT_INVALID_FORMAT:Dinh dang file Excel khong hop le.");
        }

        var sheet = workbook.Worksheets.FirstOrDefault();
        if (sheet == null)
            throw new InvalidOperationException("DRUG_IMPORT_INVALID_FORMAT:File Excel khong co sheet nao.");

        int totalRows = 0, inserted = 0, updated = 0, failed = 0;
        var errors = new List<DrugImportError>();

        using var conn = (IDbConnection)_db.CreateConnection();

        var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (int row = 2; row <= lastRow; row++) // skip header row 1
        {
            totalRows++;
            try
            {
                var code = sheet.Cell(row, 1).GetString()?.Trim();
                var nameVi = sheet.Cell(row, 2).GetString()?.Trim();
                var unit = sheet.Cell(row, 7).GetString()?.Trim();
                var form = sheet.Cell(row, 8).GetString()?.Trim();

                if (string.IsNullOrWhiteSpace(code))
                {
                    errors.Add(new DrugImportError(row, "Ma thuoc khong duoc de trong."));
                    failed++;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(nameVi))
                {
                    errors.Add(new DrugImportError(row, "Ten thuoc (name_vi) khong duoc de trong."));
                    failed++;
                    continue;
                }

                var nameEn = sheet.Cell(row, 3).GetString()?.Trim();
                var genericName = sheet.Cell(row, 4).GetString()?.Trim();
                var atcCode = sheet.Cell(row, 5).GetString()?.Trim();
                var strength = sheet.Cell(row, 6).GetString()?.Trim();
                var manufacturer = sheet.Cell(row, 9).GetString()?.Trim();
                var country = sheet.Cell(row, 10).GetString()?.Trim();
                decimal.TryParse(sheet.Cell(row, 11).GetString(), out var price);
                int.TryParse(sheet.Cell(row, 12).GetString(), out var rx);
                int.TryParse(sheet.Cell(row, 13).GetString(), out var psycho);
                int.TryParse(sheet.Cell(row, 14).GetString(), out var narcotic);

                var validForms = new[] { "TABLET", "CAPSULE", "SYRUP", "INJ", "CREAM", "OINTMENT", "DROP", "INHALER", "POWDER", "SUPPOSITORY", "OTHER" };
                if (!string.IsNullOrWhiteSpace(form) && !validForms.Contains(form.ToUpper()))
                    form = "OTHER";

                // Check existing
                var existingId = await conn.ExecuteScalarAsync<int?>(
                    "SELECT ID FROM pha_drug_master WHERE tenant_id = @tenantId AND CODE = @code AND DELETED_AT IS NULL",
                    new { tenantId, code });

                if (existingId.HasValue && mode == "INSERT")
                {
                    errors.Add(new DrugImportError(row, $"Ma thuoc '{code}' da ton tai (mode=INSERT)."));
                    failed++;
                    continue;
                }

                if (existingId.HasValue)
                {
                    await conn.ExecuteAsync(
                        @"UPDATE pha_drug_master SET DRUG_NAME=@nameVi, name_en=@nameEn, generic_name=@genericName,
                          atc_code=@atcCode, STRENGTH=@strength, UNIT=@unit, form=@form,
                          MANUFACTURER=@manufacturer, COUNTRY=@country, price=@price,
                          requires_prescription=@rx, is_psychotropic=@psycho, is_narcotic=@narcotic, UPDATED_AT=NOW()
                          WHERE ID=@id",
                        new { nameVi, nameEn, genericName, atcCode, strength, unit, form = form?.ToUpper() ?? "OTHER",
                              manufacturer, country, price, rx, psycho, narcotic, id = existingId.Value });
                    updated++;
                }
                else
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO pha_drug_master (tenant_id, CODE, DRUG_NAME, name_en, generic_name, atc_code,
                          STRENGTH, UNIT, form, MANUFACTURER, COUNTRY, price, requires_prescription,
                          is_psychotropic, is_narcotic, status, CREATED_AT, UPDATED_AT)
                          VALUES (@tenantId, @code, @nameVi, @nameEn, @genericName, @atcCode,
                          @strength, @unit, @form, @manufacturer, @country, @price, @rx, @psycho, @narcotic, 'ACTIVE', NOW(), NOW())",
                        new { tenantId, code, nameVi, nameEn, genericName, atcCode, strength, unit, form = form?.ToUpper() ?? "OTHER",
                              manufacturer, country, price, rx, psycho, narcotic });
                    inserted++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Drug import row {Row} error", row);
                errors.Add(new DrugImportError(row, ex.Message));
                failed++;
            }
        }

        workbook.Dispose();
        return new DrugImportResult(totalRows, inserted, updated, failed, errors);
    }
}
