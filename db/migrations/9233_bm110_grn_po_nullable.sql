-- BM-110: cho phep phieu nhap kho (GRN) khong can don dat hang (PO).
-- Idempotent: MODIFY lap lai khong doi ket qua; giu nguyen FK/index.
ALTER TABLE diab_his_pha_grn MODIFY COLUMN purchase_order_id CHAR(36) NULL;
