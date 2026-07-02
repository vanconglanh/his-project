using System.Text.Json.Nodes;

namespace ProDiabHis.Application.Bhyt;

// ── Request DTOs ─────────────────────────────────────────────────────────────

public record CreateBhytExportRequest(
    string PeriodMonth,       // YYYY-MM
    JsonObject? ScopeFilter,
    string? Note);

public record SignBhytExportRequest(
    string? CertThumbprint,
    string? Pin);

public record DisputeReconcileItemRequest(
    string Reason,
    string? EvidenceFilePath);

public record AcceptReconcileItemRequest(
    string? Note);

// ── Response DTOs ─────────────────────────────────────────────────────────────

public record BhytExportResponse(
    int Id,
    int TenantId,
    string PeriodMonth,
    object? ScopeFilterJson,
    string Status,
    int EncounterCount,
    decimal TotalRequestedAmount,
    decimal TotalApprovedAmount,
    decimal TotalRejectedAmount,
    DateTime? GeneratedAt,
    DateTime? ValidatedAt,
    DateTime? SignedAt,
    DateTime? SubmittedAt,
    DateTime? ResponseAt,
    string? ResponseMessage,
    string? XmlFilePath,
    string? BhytReference,
    DateTime CreatedAt,
    Guid? CreatedBy,
    DateTime UpdatedAt,
    Guid? UpdatedBy);

public record BhytValidationError(
    int TableNo,
    int RowIndex,
    string Field,
    string Message);

public record BhytValidationResultResponse(
    bool Valid,
    IReadOnlyList<BhytValidationError> Errors);

public record BhytExportItemResponse(
    int Id,
    int ExportId,
    int TableNo,
    int RecordIndex,
    object? RowDataJson,
    string? SourceEncounterId,
    string? SourceBillingId,
    string? MaLienKet,
    decimal RequestAmount,
    decimal? ApprovedAmount,
    string? RejectionCode,
    string? RejectionReason);

public record ReconcileUploadResponse(
    Guid Id,
    int TenantId,
    int ExportId,
    string FilePath,
    DateTime UploadedAt,
    DateTime? ParsedAt,
    string ParseStatus,
    string? ParseError,
    DateTime CreatedAt,
    Guid? CreatedBy);

public record ReconcileItemResponse(
    Guid Id,
    int ExportId,
    int? ExportItemId,
    int TableNo,
    string MaLienKet,
    decimal RequestAmount,
    decimal ApprovedAmount,
    decimal RejectedAmount,
    string? RejectionCode,
    string? RejectionReason,
    string Status,
    string? DisputeReason,
    string? DisputeEvidencePath,
    DateTime UpdatedAt);

public record ReconcileSummaryByTable(
    int TableNo,
    decimal Requested,
    decimal Approved,
    decimal Rejected);

public record ReconcileTopRejection(
    string Code,
    string Reason,
    int Count,
    decimal Amount);

public record ReconcileSummaryResponse(
    int ExportId,
    string PeriodMonth,
    int TotalItems,
    int ApprovedItems,
    int RejectedItems,
    int AdjustedItems,
    int DisputedItems,
    decimal TotalRequestedAmount,
    decimal TotalApprovedAmount,
    decimal TotalRejectedAmount,
    IReadOnlyList<ReconcileSummaryByTable> ByTable,
    IReadOnlyList<ReconcileTopRejection> TopRejectionReasons);

// ── Bảng Row DTOs (QD 4750) ──────────────────────────────────────────────────

public record BhytTable1Row(
    string MaLienKet, string MaBn, string HoTen, string NgaySinh,
    int GioiTinh, string MaTheBhyt, string MaDkbd,
    string GtTheTu, string GtTheDen, int MaLoaiKcb,
    DateTime NgayVao, DateTime NgayRa, int SoNgayDtri,
    int KetQuaDtri, string MaBenh, string? MaBenhPhu,
    string LyDoVvien, string ChanDoanRv,
    decimal TThuoc, decimal TVtyt, decimal TTongchi,
    decimal TBhtt, decimal TBntt, decimal TBncct);

public record BhytTable2Row(
    string MaLienKet, string MaThuoc, string TenThuoc,
    string DonViTinh, string HamLuong, string DuongDung,
    string LieuDung, string SoDangKy, string MaNhaThau,
    int PhamViTt, decimal SoLuong, decimal DonGia,
    decimal ThanhTien, decimal TBhtt, decimal TNguonkhac,
    decimal TNguonkhacBhtt, decimal TNguonkhacKhac,
    int MucHuong, DateTime NgayYl, string MaPhong,
    string MaBs, string? MaDichvuKem, string? MahieuLo,
    string? HanDung, string? SoHop);

public record BhytTable3Row(
    string MaLienKet, string MaDichVu, string? MaVatTu,
    string? TenVatTu, string DonViTinh, int PhamVi,
    decimal SoLuong, decimal DonGia, string? TtThau,
    decimal ThanhTien, decimal TBhtt, int MucHuong,
    DateTime NgayYl, string MaPhong, string MaBs,
    string MaBenh, DateTime? NgayKq);

public record BhytTable4Row(
    string MaLienKet, string MaDichVu, string TenDichVu,
    string DonViTinh, decimal SoLuong, decimal DonGia,
    decimal ThanhTien, decimal TBhtt, int MucHuong,
    DateTime NgayYl, string MaPhong, string MaBs, string MaBenh);

public record BhytTable5Row(
    string MaLienKet, string MaChiPhi, string TenChiPhi,
    int NhomChiPhi, decimal ThanhTien, decimal TBhtt,
    decimal TBntt, decimal TNguonkhac);
