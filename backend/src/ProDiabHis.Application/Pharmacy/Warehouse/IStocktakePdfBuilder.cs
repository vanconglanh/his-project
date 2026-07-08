using ProDiabHis.Application.Reports;

namespace ProDiabHis.Application.Pharmacy.Warehouse;

/// <summary>Sinh PDF Phieu kiem ke kho (QuestPDF), kho A4, dung chung khung thuong hieu diaB.</summary>
public interface IStocktakePdfBuilder
{
    byte[] Build(StocktakeData data);
}

/// <summary>Du lieu render Phieu kiem ke kho.</summary>
public record StocktakeData(
    LetterheadDto Letterhead,
    string? StocktakeCode,
    DateOnly StocktakeDate,
    string? Location,
    string? Note,
    IReadOnlyList<StocktakeItemRow> Items);

public record StocktakeItemRow(
    int Stt,
    string? DrugCode,
    string DrugName,
    string? Unit,
    string? LotNumber,
    int SystemQty,
    int CountedQty,
    int Difference);
