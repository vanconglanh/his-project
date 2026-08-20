using ProDiabHis.Domain.Common;

namespace ProDiabHis.Domain.Entities.Pharmacy;

/// <summary>
/// Dong thuoc thuc te da cap phat (FEFO pick, co so lo/han dung). Map bang
/// diab_his_pha_dispense_items (migration 0038_create_dispense_records.sql).
/// Dung lam nguon MAHIEU_LO / HAN_DUNG cho XML Bang 2 QD 3176 (xem BhytXmlSql.PrescriptionItems).
/// </summary>
public class DispenseItem : BaseEntity, ITenantScoped
{
    public int TenantId { get; set; }
    public Guid DispenseRecordId { get; set; }
    public Guid PrescriptionItemId { get; set; }
    public int DrugId { get; set; }
    public string BatchNo { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public bool IsReturned { get; set; }
    public decimal ReturnedQuantity { get; set; }
}
