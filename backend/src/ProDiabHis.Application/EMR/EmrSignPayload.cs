using System.Text;

namespace ProDiabHis.Application.EMR;

/// <summary>
/// §5.8.3 — Dong goi payload chu ky so EMR o MOT cho duy nhat (BE verify + FE ky phai dung cung dinh nghia).
///
/// Ranh gioi v1/v2 (§5.8.4):
///  - v2: ban ghi co it nhat 1 trong (structured_values_json, schema_snapshot_json) KHAC NULL
///        => payload = UTF8("v2\n" + content_json + "\n" + (structured_values_json ?? "") + "\n" + (schema_snapshot_json ?? "")).
///  - v1: ban ghi cu, ca 2 cot deu NULL => payload = UTF8(content_json) (giu nguyen duong verify cu).
///
/// QUAN TRONG: LUON hash dung chuoi JSON DA LUU trong DB, KHONG serialize lai (tranh lech canonical).
/// </summary>
public static class EmrSignPayload
{
    public const string V2Prefix = "v2\n";

    /// <summary>Payload v1 (chi content_json) — cho ban ghi cu, giu nguyen duong verify.</summary>
    public static byte[] BuildV1(string contentJson)
        => Encoding.UTF8.GetBytes(contentJson);

    /// <summary>Payload v2 (gop 3 phan) — cho ban ghi co structured_values / schema_snapshot.</summary>
    public static byte[] BuildV2(string contentJson, string? structuredValuesJson, string? schemaSnapshotJson)
        => Encoding.UTF8.GetBytes(
            V2Prefix + contentJson + "\n" + (structuredValuesJson ?? "") + "\n" + (schemaSnapshotJson ?? ""));

    /// <summary>
    /// Chon dung payload theo ranh gioi v1/v2 (§5.8.4).
    /// Ban ghi co bat ky cot structured_values_json / schema_snapshot_json khac NULL => v2, nguoc lai v1.
    /// </summary>
    public static byte[] Build(string contentJson, string? structuredValuesJson, string? schemaSnapshotJson)
    {
        var isV2 = structuredValuesJson is not null || schemaSnapshotJson is not null;
        return isV2
            ? BuildV2(contentJson, structuredValuesJson, schemaSnapshotJson)
            : BuildV1(contentJson);
    }
}
