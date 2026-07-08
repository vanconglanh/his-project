using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Pharmacy.Dispensing;

/// <summary>Sinh PDF Phieu phat thuoc (QuestPDF), kho A5, dung chung khung thuong hieu diaB.</summary>
public interface IPharmacyDispenseReceiptPdfBuilder
{
    byte[] Build(DispenseReceiptData data);
}

/// <summary>Du lieu render Phieu phat thuoc.</summary>
public record DispenseReceiptData(
    LetterheadDto Letterhead,
    string DispenseRecordId,
    string PrescriptionId,
    string? PatientName,
    string? PatientCode,
    DateTime DispensedAt,
    string? Note,
    IReadOnlyList<DispenseReceiptItem> Items,
    decimal TotalAmount);

public record DispenseReceiptItem(
    int Stt,
    string DrugName,
    string? Unit,
    decimal Quantity,
    string? BatchNo,
    DateOnly? ExpiryDate);
