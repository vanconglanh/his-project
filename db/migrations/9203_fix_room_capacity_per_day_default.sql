-- =====================================================================
-- 9203_fix_room_capacity_per_day_default.sql
-- Muc dich: dong bo ngu nghia cot diab_his_sys_rooms.capacity voi logic F04.
--
-- Boi canh (ket luan PO ngay 2026-09-05):
--   Fix F04 (ReceptionHandlers.cs:61-68) dung capacity nhu "TONG SO LUOT
--   check-in trong NGAY" (quota/ngay) - dung chu y nghiep vu, hop dong OpenAPI
--   docs/api/openapi/reception.yaml:288 ghi max_per_day example=40.
--   NHUNG cot DB capacity duoc tao voi DEFAULT 1 va comment "so BN toi da cung
--   luc" (ngu nghia concurrent cu). Hau qua: moi phong EXAM cu co capacity=1 se
--   bi F04 tu choi ngay benh nhan thu 2 trong ngay (409 RECEPTION_ROOM_FULL) -
--   "phong day" gia.
--
-- Migration nay:
--   1. Doi DEFAULT cot capacity 1 -> 40 va sua comment cho khop ngu nghia moi.
--   2. Backfill: nang capacity cac phong EXAM dang <= 1 len 40 (quota/ngay hop ly
--      cho phong kham 1 bac si). Chi dong toi phong con hoat dong, chua xoa mem.
--
-- LUU Y con so 40: lay theo example trong OpenAPI reception.yaml. Neu phong kham
--   thuc te co dinh muc khac (vd 50/60) thi sua bien @DefaultQuota o dau phan
--   thuc thi ben duoi - CHI MOT CHO, ca ALTER lan UPDATE deu doc bien nay.
--
-- PHAM VI TENANT (co y, khong phai thieu sot): UPDATE ben duoi CO TINH chay
--   XUYEN TENANT, khong loc tenant_id. Ly do: capacity=1 khong phai lua chon
--   nghiep vu cua bat ky phong kham nao ma la DEFAULT SAI cua schema cu (ngu
--   nghia concurrent). Loc theo tung tenant se bo sot tenant moi tao.
--   RUI RO da can nhac: neu MOT tenant CO Y dat quota 1 luot/ngay (vd phong tu
--   van theo hop dong) thi se bi nang len 40 ngoai y muon. Truoc khi chay tren
--   moi truong that, DBA nen kiem tra danh sach phong bi anh huong bang cau
--   SELECT o buoc 0 ben duoi, va them dieu kien loai tru neu can, vi du:
--       AND tenant_id NOT IN (...)
-- Idempotent: chay lai nhieu lan an toan (chi update phong con <=1; ALTER dat lai
--   dung gia tri dich nen lap lai khong doi ket qua).
-- =====================================================================

-- --- 0. Dinh muc mac dinh: SUA O DAY neu phong kham co dinh muc khac ---
SET @DefaultQuota := 40;

-- --- 0b. Kiem tra truoc khi chay: liet ke phong SE bi anh huong ---
--     Chay rieng cau nay trong moi truong that de DBA soat lai, dam bao khong
--     co phong nao CO Y giu quota thap. Khong anh huong ket qua migration.
SELECT tenant_id, id, name, room_type, capacity
  FROM `diab_his_sys_rooms`
 WHERE `room_type` = 'EXAM' AND `capacity` <= 1 AND `deleted_at` IS NULL;

-- --- 1. Doi DEFAULT + comment cot capacity (idempotent: dat ve dung dich) ---
--     ALTER khong nhan bien nen phai dung PREPARE de @DefaultQuota la nguon duy nhat.
SET @ddl := CONCAT(
    'ALTER TABLE `diab_his_sys_rooms` MODIFY COLUMN `capacity` INT NOT NULL DEFAULT ',
    @DefaultQuota,
    ' COMMENT ''So luot check-in toi da/phong/ngay (max_per_day, xem F04)''');
PREPARE _stmt FROM @ddl;
EXECUTE _stmt;
DEALLOCATE PREPARE _stmt;

-- --- 2. Backfill phong EXAM con dang giu default cu (capacity <= 1) ---
-- Chi nang phong con hoat dong, chua xoa mem. Cac loai phong khac (LAB/RADIOLOGY/
-- WAITING/CASHIER) khong bi rang buoc F04 tai check-in nen khong dong toi.
UPDATE `diab_his_sys_rooms`
   SET `capacity` = @DefaultQuota,
       `updated_at` = CURRENT_TIMESTAMP
 WHERE `room_type` = 'EXAM'
   AND `capacity` <= 1
   AND `deleted_at` IS NULL;
