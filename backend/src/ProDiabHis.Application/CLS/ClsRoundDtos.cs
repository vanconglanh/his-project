namespace ProDiabHis.Application.CLS;

/// <summary>Dich vu XN trong 1 dot chi dinh</summary>
public record ClsRoundLabItemRequest(
    string TestCode,
    string? TestName,
    string? SampleType,
    string? Priority,
    string? Note,
    // BM-16: bac si tick "Khong thu phi" cho RIENG dich vu nay ngay luc tao dot -
    // khac voi "Mien phi" ap dung ca dot o buoc Chot (WaiveClsRoundCommand, giu nguyen).
    bool IsFree = false);

/// <summary>Dich vu CDHA trong 1 dot chi dinh</summary>
public record ClsRoundRadItemRequest(
    string Modality,
    string? BodyPart,
    bool Contrast,
    string ProcedureCode,
    string? ProcedureName,
    string? Priority,
    string? Note,
    bool IsFree = false);

public record CreateClsRoundRequest(
    string? Note,
    IReadOnlyList<ClsRoundLabItemRequest>? LabTests,
    IReadOnlyList<ClsRoundRadItemRequest>? RadOrders);

public record PayClsRoundRequest(Guid? BillingId, string? Method, decimal? Amount, string? Note);

public record WaiveClsRoundRequest(string Reason);

public record CancelClsRoundRequest(string? Reason);

public record ClsRoundOrderItemResponse(
    Guid Id,
    string Kind,          // LAB | RAD
    string Code,
    string Name,
    string Status,
    decimal UnitPrice,
    bool IsFree = false); // BM-16: dich vu nay khong thu phi (khong tinh vao total_amount)

public record ClsRoundProgressResponse(int Total, int Done, int Pending);

public record ClsRoundResponse(
    Guid Id,
    Guid EncounterId,
    int RoundNo,
    string Status,
    string PaymentStatus,
    decimal TotalAmount,
    Guid? BillingId,
    DateTime? PaidAt,
    string? WaivedReason,
    string? Note,
    DateTime CreatedAt,
    IReadOnlyList<ClsRoundOrderItemResponse> LabOrders,
    IReadOnlyList<ClsRoundOrderItemResponse> RadOrders,
    ClsRoundProgressResponse Progress);

public record ClsRoundListResponse(
    IReadOnlyList<ClsRoundResponse> Rounds,
    int Total,
    int UnpaidRounds,
    decimal UnpaidAmount);
