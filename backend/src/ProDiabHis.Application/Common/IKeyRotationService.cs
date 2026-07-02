namespace ProDiabHis.Application.Common;

public record RotateKeyResult(
    long NewKeyId,
    int NewVersion,
    int OldKeysDeactivated
);

/// <summary>Thuc hien rotation encryption key</summary>
public interface IKeyRotationService
{
    /// <summary>
    /// Gen key moi, danh is_active=0 cho key cu, luu key moi vao storage.
    /// tenantId=null nghia la global key.
    /// </summary>
    Task<RotateKeyResult> RotateKeyAsync(int? tenantId, KeyPurpose purpose, CancellationToken ct = default);

    /// <summary>Rotate tat ca key qua 365 ngay (Hangfire job trigger)</summary>
    Task RotateExpiredKeysAsync(CancellationToken ct = default);
}
