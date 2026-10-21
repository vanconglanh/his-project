namespace ProDiabHis.Application.Billing;

// BM-95 voucher DTOs. JSON se duoc serialize snake_case (global policy).

public record CreateVoucherRequest(
    string Code,
    string? Description,
    decimal DiscountPercent,
    string AppliesTo,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? MaxUses,
    bool IsActive = true);

public record UpdateVoucherRequest(
    string? Description,
    decimal DiscountPercent,
    string AppliesTo,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? MaxUses,
    bool IsActive);

public record VoucherResponse(
    Guid Id,
    string Code,
    string? Description,
    decimal DiscountPercent,
    string AppliesTo,
    DateOnly? ValidFrom,
    DateOnly? ValidTo,
    int? MaxUses,
    int UsedCount,
    bool IsActive,
    DateTime CreatedAt);

public record ApplyVoucherRequest(string Code);
