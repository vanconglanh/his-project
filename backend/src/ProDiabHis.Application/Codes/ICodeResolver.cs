namespace ProDiabHis.Application.Codes;

/// <summary>1 muc trong danh muc ma da RESOLVE theo tenant hien tai (tenant override thang global).</summary>
public record CodeItem(string Code, string Name, string? Extra = null);

/// <summary>
/// N1 (audit-hardcode-vs-master-data, Viec 1) - resolve danh muc ma theo tenant:
///   lay ma chuan he thong (tenant_id IS NULL) + ma rieng cua tenant hien tai,
///   neu trung `code` thi ban cua tenant THANG; loai bo is_hidden=1 hoac is_active=0.
/// </summary>
public interface ICodeResolver
{
    Task<IReadOnlyList<CodeItem>> GetAsync(string groupId, CancellationToken ct = default);

    Task<bool> IsValidAsync(string groupId, string? code, CancellationToken ct = default);

    Task<string> LabelAsync(string groupId, string code, CancellationToken ct = default);
}
