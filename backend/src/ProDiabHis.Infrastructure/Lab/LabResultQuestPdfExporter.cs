using Dapper;
using ProDiabHis.Application.Common;
using ProDiabHis.Application.Reports;
using ProDiabHis.Domain.Entities;
using ProDiabHis.Infrastructure.Reports;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ProDiabHis.Infrastructure.Lab;

/// <summary>
/// QuestPDF implementation cho ILabResultPdfExporter. Nang cap: letterhead + barcode + block
/// benh nhan/chan doan tai dung ReportPdfCommon (dong bo khung thuong hieu diaB voi cac phieu khac).
/// </summary>
public class LabResultQuestPdfExporter : ILabResultPdfExporter
{
    private readonly IDapperConnectionFactory _db;

    static LabResultQuestPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public LabResultQuestPdfExporter(IDapperConnectionFactory db) => _db = db;

    public async Task<byte[]> ExportLabResultAsync(LabResult entity, CancellationToken ct = default)
    {
        using var conn = (System.Data.IDbConnection)_db.CreateConnection();

        // Ket qua XN co the thuoc encounter/patient duoc gan truc tiep (cot moi) hoac chi lien ket
        // qua lab_order legacy (diab_his_lab_orders.encounter_id) — COALESCE ca hai nguon.
        var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT COALESCE(lr.encounter_id, lo.encounter_id) AS encounter_id
              FROM diab_his_lab_results lr
              LEFT JOIN diab_his_lab_orders lo ON lo.id = lr.order_id
              WHERE lr.id = @Id",
            new { Id = entity.Id.ToString() });

        string? encounterId = row?.encounter_id;

        dynamic? enc = null;
        IEnumerable<dynamic> diagnoses = Enumerable.Empty<dynamic>();
        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            enc = await conn.QueryFirstOrDefaultAsync<dynamic>(
                @"SELECT pat.code AS patient_code, pat.full_name AS patient_full_name, pat.gender AS patient_gender,
                         pat.date_of_birth AS patient_dob, pat.street AS patient_address,
                         doc.full_name AS doctor_full_name
                  FROM diab_his_enc_encounters e
                  LEFT JOIN diab_his_pat_patients pat ON pat.id = e.patient_id AND pat.tenant_id = e.tenant_id
                  LEFT JOIN diab_his_sec_users doc ON doc.id = e.doctor_id
                  WHERE e.id = @encId",
                new { encId = encounterId });

            diagnoses = await conn.QueryAsync<dynamic>(
                @"SELECT icd10_code, name FROM diab_his_enc_diagnoses
                  WHERE encounter_id = @encId AND deleted_at IS NULL ORDER BY created_at",
                new { encId = encounterId });
        }

        var lh = await conn.QueryFirstOrDefaultAsync<LetterheadDto>(
            @"SELECT name AS ClinicName, cskcb_code AS CskcbCode, company_name AS CompanyName, address AS Address,
                     phone AS Phone, email AS Email, email_support AS EmailSupport, logo_url AS LogoUrl,
                     slogan AS Slogan, website AS Website
              FROM diab_his_sys_tenants WHERE id = @tenantId",
            new { tenantId = entity.TenantId });
        lh ??= new LetterheadDto("Pro-Diab HIS", null, null, null, null, null, null, null);

        var diagnosisText = string.Join("; ", diagnoses.Select(d => $"{(string)d.icd10_code} – {(string)d.name}"));

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);
                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, lh, null));

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "PHIẾU KẾT QUẢ XÉT NGHIỆM"));
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, entity.TestCode));

                    col.Item().PaddingTop(8).Border(1).BorderColor(ReportPdfCommon.LineColor).Background("#F8FAFC").Padding(9).Column(info =>
                    {
                        if (enc is not null)
                        {
                            info.Item().Text(t =>
                            {
                                t.Span("Họ tên: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span((string?)enc.patient_full_name ?? "—").Bold().FontSize(10);
                                t.Span("   Giới tính: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span(GenderLabel((string?)enc.patient_gender)).FontSize(9);
                            });
                            info.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("Mã BN: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span((string?)enc.patient_code ?? "—").FontSize(9);
                                if ((string?)enc.doctor_full_name is { } docName)
                                {
                                    t.Span("   Bác sĩ chỉ định: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                    t.Span(docName).FontSize(9);
                                }
                            });
                            if (!string.IsNullOrWhiteSpace(diagnosisText))
                                info.Item().PaddingTop(2).Text(t =>
                                {
                                    t.Span("Chẩn đoán: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                    t.Span(diagnosisText).FontColor(ReportPdfCommon.Brand).SemiBold().FontSize(9);
                                });
                        }
                        info.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("Ngày thực hiện: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span($"{entity.PerformedAt:HH:mm dd/MM/yyyy}").FontSize(9);
                            if (entity.VerifiedAt.HasValue)
                            {
                                t.Span("   Xác thực lúc: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span($"{entity.VerifiedAt.Value:HH:mm dd/MM/yyyy}").FontSize(9);
                            }
                        });
                    });

                    col.Item().PaddingTop(10).Text("Kết quả xét nghiệm").Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Brand);
                    col.Item().PaddingTop(3).Table(tbl =>
                    {
                        tbl.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1.3f);
                            cols.RelativeColumn(1.3f);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(0.8f);
                        });
                        tbl.Header(h =>
                        {
                            ReportPdfCommon.HText(h.Cell(), "Tên xét nghiệm");
                            ReportPdfCommon.HText(h.Cell(), "Kết quả");
                            ReportPdfCommon.HText(h.Cell(), "Đơn vị");
                            ReportPdfCommon.HText(h.Cell(), "Khoảng tham chiếu");
                            ReportPdfCommon.HText(h.Cell(), "Cờ");
                        });

                        var flagColor = entity.Flag switch
                        {
                            "HIGH" or "CRITICAL_HIGH" => "#C0392B",
                            "LOW" or "CRITICAL_LOW" => "#1A5FB4",
                            _ => ReportPdfCommon.Ink
                        };
                        var refRange = (entity.ReferenceRangeLow, entity.ReferenceRangeHigh) switch
                        {
                            (not null, not null) => $"{entity.ReferenceRangeLow} – {entity.ReferenceRangeHigh}",
                            (not null, null) => $"≥ {entity.ReferenceRangeLow}",
                            (null, not null) => $"≤ {entity.ReferenceRangeHigh}",
                            _ => "—"
                        };

                        ReportPdfCommon.BodyCell(tbl.Cell(), 0).Text(entity.TestName).FontSize(9.5f);
                        ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(entity.Value).FontColor(flagColor).Bold().FontSize(9.5f);
                        ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(entity.Unit ?? "").FontSize(9);
                        ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(refRange).FontSize(9);
                        ReportPdfCommon.BodyCell(tbl.Cell(), 0).AlignCenter().Text(FlagLabel(entity.Flag)).FontColor(flagColor).Bold().FontSize(9);
                    });

                    if (!string.IsNullOrWhiteSpace(entity.Method))
                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("Phương pháp: ").FontColor(ReportPdfCommon.Muted).FontSize(8.5f);
                            t.Span(entity.Method).FontSize(8.5f);
                        });

                    if (!string.IsNullOrWhiteSpace(entity.Note))
                        col.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("Ghi chú: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(entity.Note).FontSize(9);
                        });

                    col.Item().PaddingTop(16).Element(c => ReportPdfCommon.RenderFooter(c, null));
                });
            });
        }).GeneratePdf();

        return pdf;
    }

    public async Task<byte[]> ExportRadResultAsync(dynamic row, CancellationToken ct = default)
    {
        using var conn = (System.Data.IDbConnection)_db.CreateConnection();

        var lh = new LetterheadDto(
            (string?)TryGet(row, "clinic_name") ?? "Pro-Diab HIS",
            (string?)TryGet(row, "cskcb_code"), (string?)TryGet(row, "company_name"), (string?)TryGet(row, "clinic_address"),
            (string?)TryGet(row, "clinic_phone"), (string?)TryGet(row, "clinic_email"), (string?)TryGet(row, "clinic_email_support"),
            (string?)TryGet(row, "clinic_logo_url"),
            (string?)TryGet(row, "clinic_slogan"), (string?)TryGet(row, "clinic_website"));

        string procedureName = (string?)TryGet(row, "procedure_name") ?? "—";
        string modality = (string?)TryGet(row, "modality") ?? "—";
        string? bodyPart = (string?)TryGet(row, "body_part");
        bool contrast = ((bool?)TryGet(row, "contrast")) ?? false;
        string? description = (string?)TryGet(row, "description");
        string? impression = (string?)TryGet(row, "impression");
        string? recommendation = (string?)TryGet(row, "recommendation");
        DateTime? performedAt = (DateTime?)TryGet(row, "performed_at");
        string? performedBy = (string?)TryGet(row, "performed_by");
        string? patientName = (string?)TryGet(row, "patient_full_name");
        string? patientCode = (string?)TryGet(row, "patient_code");
        string? patientGender = (string?)TryGet(row, "patient_gender");
        DateTime? patientDob = (DateTime?)TryGet(row, "patient_dob");
        string? doctorName = (string?)TryGet(row, "doctor_full_name");
        string idStr = (string)TryGet(row, "id")!;

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                ReportPdfCommon.ConfigureA4Page(page);
                page.Header().Element(c => ReportPdfCommon.RenderLetterhead(c, lh, null));

                page.Content().PaddingTop(6).Column(col =>
                {
                    col.Item().Element(c => ReportPdfCommon.RenderTitle(c, "PHIẾU KẾT QUẢ CHẨN ĐOÁN HÌNH ẢNH"));
                    col.Item().Element(c => ReportPdfCommon.RenderBarcode(c, idStr.Length >= 8 ? idStr[..8].ToUpper() : idStr));

                    col.Item().PaddingTop(8).Border(1).BorderColor(ReportPdfCommon.LineColor).Background("#F8FAFC").Padding(9).Column(info =>
                    {
                        info.Item().Text(t =>
                        {
                            t.Span("Họ tên: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(patientName ?? "—").Bold().FontSize(10);
                            t.Span("   Giới tính: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(GenderLabel(patientGender)).FontSize(9);
                        });
                        info.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("Mã BN: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(patientCode ?? "—").FontSize(9);
                            if (doctorName is not null)
                            {
                                t.Span("   Bác sĩ chỉ định: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span(doctorName).FontSize(9);
                            }
                        });
                        info.Item().PaddingTop(2).Text(t =>
                        {
                            t.Span("Kỹ thuật: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span($"{procedureName} ({modality})").FontSize(9);
                            if (!string.IsNullOrWhiteSpace(bodyPart))
                            {
                                t.Span("   Vùng chụp: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span(bodyPart).FontSize(9);
                            }
                            t.Span("   Cản quang: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(contrast ? "Có" : "Không").FontSize(9);
                        });
                        if (performedAt.HasValue)
                            info.Item().PaddingTop(2).Text(t =>
                            {
                                t.Span("Ngày thực hiện: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                                t.Span($"{performedAt.Value:HH:mm dd/MM/yyyy}").FontSize(9);
                            });
                    });

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        col.Item().PaddingTop(10).Text("Mô tả hình ảnh").Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Brand);
                        col.Item().PaddingTop(2).Text(description).FontSize(9.5f);
                    }
                    if (!string.IsNullOrWhiteSpace(impression))
                    {
                        col.Item().PaddingTop(8).Text("Nhận định").Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Brand);
                        col.Item().PaddingTop(2).Border(1).BorderColor(ReportPdfCommon.LineColor).Padding(8).Text(impression).FontSize(9.5f);
                    }
                    if (!string.IsNullOrWhiteSpace(recommendation))
                    {
                        col.Item().PaddingTop(8).Text("Khuyến nghị").Bold().FontSize(10.5f).FontColor(ReportPdfCommon.Brand);
                        col.Item().PaddingTop(2).Text(recommendation).FontSize(9.5f);
                    }
                    if (!string.IsNullOrWhiteSpace(performedBy))
                        col.Item().PaddingTop(6).Text(t =>
                        {
                            t.Span("Người thực hiện: ").FontColor(ReportPdfCommon.Muted).FontSize(9);
                            t.Span(performedBy).FontSize(9);
                        });

                    col.Item().PaddingTop(16).Element(c => ReportPdfCommon.RenderFooter(c, null));
                });
            });
        }).GeneratePdf();

        await Task.CompletedTask;
        return pdf;
    }

    private static object? TryGet(dynamic row, string key)
    {
        try
        {
            var dict = (IDictionary<string, object>)row;
            return dict.TryGetValue(key, out var v) ? v : null;
        }
        catch
        {
            return null;
        }
    }

    private static string GenderLabel(string? gender) => gender switch
    {
        "MALE" => "Nam",
        "FEMALE" => "Nữ",
        "OTHER" => "Khác",
        _ => "—"
    };

    private static string FlagLabel(string flag) => flag switch
    {
        "HIGH" => "H",
        "LOW" => "L",
        "CRITICAL_HIGH" => "HH",
        "CRITICAL_LOW" => "LL",
        "NORMAL" => "",
        _ => flag
    };
}
