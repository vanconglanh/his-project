namespace ProDiabHis.Application.CLS;

/// <summary>Sinh PDF Phieu chi dinh XN / CDHA (QuestPDF), dung chung khung thuong hieu diaB.</summary>
public interface IClsOrderSlipPdfBuilder
{
    byte[] BuildLabOrderSlip(ClsOrderSlipData data);
    byte[] BuildRadOrderSlip(ClsOrderSlipData data);
}
