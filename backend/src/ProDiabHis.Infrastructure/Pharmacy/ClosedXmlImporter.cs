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
///   K: price, L: requires_prescription (1/0), M: is_psychotropic (1/0), N: is_narcotic (1/0),
///   O: route (duong dung, tuy chon - vd uong|tiem_bap|tiem_tinh_mach...)
///
/// LUU Y (migration 9180 - Quyet dinh chot bo cot 9005 la nguon su that):
///   Importer GHI VAO BANG CANONICAL diab_his_pha_drugs (KHONG dung view pha_drug_master
///   vi view tao o migration 9009 bang SELECT * nen KHONG expose cot route/bhyt_code
///   them sau nay). Ghi bo cot 9005 (name/drug_form/sell_price/requires_rx/is_controlled)
///   la nguon su that, dong thoi van ghi bo cot 9010 legacy (name_vi/name_en/form/price/
///   requires_prescription/is_narcotic/is_psychotropic) de bao cao doc COALESCE 2 chieu
///   khong bi mat du lieu cho toi khi hoan tat deprecate.
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
                // Cot O (15): duong dung - tuy chon. Khong hardcode; rong => NULL.
                var routeRaw = sheet.Cell(row, 15).GetString()?.Trim();
                var route = string.IsNullOrWhiteSpace(routeRaw) ? null : routeRaw;

                var validForms = new[] { "TABLET", "CAPSULE", "SYRUP", "INJ", "CREAM", "OINTMENT", "DROP", "INHALER", "POWDER", "SUPPOSITORY", "OTHER" };
                if (!string.IsNullOrWhiteSpace(form) && !validForms.Contains(form.ToUpper()))
                    form = "OTHER";
                var drugForm = form?.ToUpper() ?? "OTHER";
                // is_controlled (bo 9005) = HOP cua is_narcotic / is_psychotropic (xem N4 tai lieu 3.2.1)
                var isControlled = (psycho == 1 || narcotic == 1) ? 1 : 0;

                // Check existing - GHI VAO BANG CANONICAL diab_his_pha_drugs (id CHAR(36))
                var existingId = await conn.ExecuteScalarAsync<string?>(
                    "SELECT id FROM diab_his_pha_drugs WHERE tenant_id = @tenantId AND code = @code AND deleted_at IS NULL",
                    new { tenantId, code });

                if (existingId != null && mode == "INSERT")
                {
                    errors.Add(new DrugImportError(row, $"Ma thuoc '{code}' da ton tai (mode=INSERT)."));
                    failed++;
                    continue;
                }

                if (existingId != null)
                {
                    await conn.ExecuteAsync(
                        // Bo 9005 (nguon su that) + route; dong bo bo 9010 legacy de bao cao khong mat du lieu.
                        @"UPDATE diab_his_pha_drugs SET
                          name=@nameVi, drug_form=@drugForm, strength=@strength, unit=@unit,
                          generic_name=@genericName, atc_code=@atcCode, sell_price=@price,
                          requires_rx=@rx, is_controlled=@isControlled, route=@route,
                          name_vi=@nameVi, name_en=@nameEn, form=@form, manufacturer=@manufacturer,
                          country=@country, price=@price, requires_prescription=@rx,
                          is_psychotropic=@psycho, is_narcotic=@narcotic, updated_at=NOW()
                          WHERE id=@id",
                        new { nameVi, nameEn, genericName, atcCode, strength, unit, drugForm, form,
                              manufacturer, country, price, rx, psycho, narcotic, isControlled, route,
                              id = existingId });
                    updated++;
                }
                else
                {
                    await conn.ExecuteAsync(
                        @"INSERT INTO diab_his_pha_drugs
                          (id, tenant_id, code, name, drug_form, strength, unit, generic_name, atc_code,
                           sell_price, requires_rx, is_controlled, route,
                           name_vi, name_en, form, manufacturer, country, price,
                           requires_prescription, is_psychotropic, is_narcotic, status, is_active,
                           created_at, updated_at)
                          VALUES
                          (UUID(), @tenantId, @code, @nameVi, @drugForm, @strength, @unit, @genericName, @atcCode,
                           @price, @rx, @isControlled, @route,
                           @nameVi, @nameEn, @form, @manufacturer, @country, @price,
                           @rx, @psycho, @narcotic, 'ACTIVE', 1,
                           NOW(), NOW())",
                        new { tenantId, code, nameVi, nameEn, genericName, atcCode, strength, unit, drugForm, form,
                              manufacturer, country, price, rx, psycho, narcotic, isControlled, route });
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
