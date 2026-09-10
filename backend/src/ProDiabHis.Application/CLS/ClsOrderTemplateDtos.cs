namespace ProDiabHis.Application.CLS;

// BM-12 (Lark Bug-Feedback): "Luu bo chi dinh mau" - PO xac nhan (2026-09-10) lam ngay,
// uu tien cao, luu RIENG TUNG BAC SI (doctor_id lay tu ICurrentUser.UserId, khong trust
// input client). Items luu nguyen JSON mang cart FE (ClsCatalogItem[] + is_free) - khong
// can strongly-type o BE vi noi dung chi doc lai de FE tu rehydrate cart, khong dung de
// tinh toan/insert truc tiep (luc "Ap dung" van goi lai API tao dot binh thuong, van duoc
// validate ma dich vu nhu moi lan tao dot khac).

/// <summary>Request tao 1 bo chi dinh mau. Items la JSON tuy y (mang item da chon o FE).</summary>
public record CreateClsOrderTemplateRequest(string Name, object Items);

public record ClsOrderTemplateResponse(
    Guid Id,
    string Name,
    object? Items,
    DateTime CreatedAt);

public record ClsOrderTemplateListResponse(IReadOnlyList<ClsOrderTemplateResponse> Templates);
