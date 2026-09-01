-- ============================================================================
-- SEED DATA cho verify LIVE 13 bao cao P1/P2 (tenant 1) — 2026-09-01
-- Tag chung de cleanup: request_id/note/reason = 'SEED-P1P2', created_by sentinel.
-- Cleanup: chay db/seeds/cleanup_reports_p1p2_tenant1.sql
-- CHI dung cho DEV — INSERT truc tiep, KHONG phai migration.
-- ============================================================================
SET @t := 1;
SET @seed := 'SEED-P1P2';

-- ---------------------------------------------------------------------------
-- A-01: Audit logs (bang rong) — 12 dong, nhieu action/resource_type
-- ---------------------------------------------------------------------------
INSERT INTO diab_his_sec_audit_logs
 (id, tenant_id, user_id, user_email, action, resource_type, resource_id, ip_address, user_agent, severity, cross_tenant_attempt, request_id, created_at, branch_id)
VALUES
 (UUID(),@t,'e210a28b-062d-4d90-98f9-693936cbcc5d','bacsi.test@prodiab.test','VIEW','PATIENT','f0000000-0000-0000-0000-000000000001','192.168.1.10','Mozilla/5.0','INFO',0,@seed,'2026-08-05 08:12:00',1),
 (UUID(),@t,'e210a28b-062d-4d90-98f9-693936cbcc5d','bacsi.test@prodiab.test','VIEW','PATIENT','f0000000-0000-0000-0000-000000000002','192.168.1.10','Mozilla/5.0','INFO',0,@seed,'2026-08-05 08:20:00',1),
 (UUID(),@t,'e210a28b-062d-4d90-98f9-693936cbcc5d','bacsi.test@prodiab.test','UPDATE','ENCOUNTER','enc-0001','192.168.1.10','Mozilla/5.0','INFO',0,@seed,'2026-08-06 09:05:00',1),
 (UUID(),@t,'14ca565a-1e49-4add-bb59-c8d343013dbc','letan.test@prodiab.test','CREATE','PATIENT','f0000000-0000-0000-0000-000000000003','192.168.1.11','Mozilla/5.0','INFO',0,@seed,'2026-08-07 10:30:00',1),
 (UUID(),@t,'14ca565a-1e49-4add-bb59-c8d343013dbc','letan.test@prodiab.test','VIEW','PATIENT','f0000000-0000-0000-0000-000000000004','192.168.1.11','Mozilla/5.0','INFO',0,@seed,'2026-08-08 11:00:00',1),
 (UUID(),@t,'394ec0a7-ccdc-448b-9a1b-43356b8abbef','ketoan.test@prodiab.test','EXPORT','REPORT','revenue-daily','192.168.1.12','Mozilla/5.0','INFO',0,@seed,'2026-08-10 14:15:00',1),
 (UUID(),@t,'394ec0a7-ccdc-448b-9a1b-43356b8abbef','ketoan.test@prodiab.test','VIEW','BILLING','bil-0001','192.168.1.12','Mozilla/5.0','INFO',0,@seed,'2026-08-12 15:40:00',1),
 (UUID(),@t,'e210a28b-062d-4d90-98f9-693936cbcc5d','bacsi.test@prodiab.test','VIEW','PRESCRIPTION','pre-0001','192.168.1.10','Mozilla/5.0','INFO',0,@seed,'2026-08-14 08:50:00',1),
 (UUID(),@t,'e210a28b-062d-4d90-98f9-693936cbcc5d','bacsi.test@prodiab.test','UPDATE','PATIENT','f0000000-0000-0000-0000-000000000005','192.168.1.10','Mozilla/5.0','WARNING',0,@seed,'2026-08-18 09:10:00',1),
 (UUID(),@t,'a0000000-0000-0000-0000-000000000001','admin@prodiab.test','DELETE','APPOINTMENT','appt-0009','192.168.1.99','Mozilla/5.0','WARNING',0,@seed,'2026-08-20 16:00:00',1),
 (UUID(),@t,'a0000000-0000-0000-0000-000000000001','admin@prodiab.test','VIEW','AUDIT','-','192.168.1.99','Mozilla/5.0','INFO',0,@seed,'2026-08-22 17:20:00',1),
 (UUID(),@t,'29f2838b-bebe-401e-9d0a-22fd39563864','duocsi.test@prodiab.test','VIEW','PATIENT','f0000000-0000-0000-0000-000000000006','192.168.1.13','Mozilla/5.0','INFO',0,@seed,'2026-08-25 10:05:00',1);

-- ---------------------------------------------------------------------------
-- P-02 / PackageUtilization: entitlement_balances (bang rong) cho 6 subscription active
-- item_type: VISIT (dinh muc luot kham). total/used/remaining.
-- ---------------------------------------------------------------------------
INSERT INTO diab_his_pkg_entitlement_balances
 (id, tenant_id, subscription_id, definition_id, item_type, item_ref_id, item_code, item_name, unit,
  total_quantity, used_quantity, unit_price_snapshot, version, created_at, created_by)
VALUES
 (UUID(),@t,'276eb289-a60e-11f1-9293-ee8160e16766',UUID(),'VISIT',UUID(),'KHAM','Luot kham dinh ky','luot',12,10,200000,1,'2026-08-01 08:00:00','SEED-P1P2'),
 (UUID(),@t,'276ec3b9-a60e-11f1-9293-ee8160e16766',UUID(),'VISIT',UUID(),'KHAM','Luot kham dinh ky','luot',6,1,200000,1,'2026-08-01 08:00:00','SEED-P1P2'),
 (UUID(),@t,'276ec9a1-a60e-11f1-9293-ee8160e16766',UUID(),'VISIT',UUID(),'KHAM','Luot kham dinh ky','luot',24,20,200000,1,'2026-08-01 08:00:00','SEED-P1P2'),
 (UUID(),@t,'276ecea5-a60e-11f1-9293-ee8160e16766',UUID(),'VISIT',UUID(),'KHAM','Luot kham dinh ky','luot',12,3,200000,1,'2026-08-01 08:00:00','SEED-P1P2'),
 (UUID(),@t,'276ed237-a60e-11f1-9293-ee8160e16766',UUID(),'VISIT',UUID(),'KHAM','Luot kham dinh ky','luot',6,6,200000,1,'2026-08-01 08:00:00','SEED-P1P2'),
 (UUID(),@t,'276ed535-a60e-11f1-9293-ee8160e16766',UUID(),'SERVICE',UUID(),'XN','Xet nghiem HbA1c','lan',4,1,150000,1,'2026-08-01 08:00:00','SEED-P1P2');

-- ---------------------------------------------------------------------------
-- O-01: queue tickets (bang rong) — thoi gian cho dang ky->goi->ket thuc
-- ---------------------------------------------------------------------------
INSERT INTO diab_his_rcp_queue_tickets
 (id, tenant_id, patient_id, room_id, doctor_id, ticket_no, ticket_date, status, priority, reason_for_visit, note,
  checked_in_at, called_at, started_at, finished_at, created_at, branch_id)
VALUES
 (UUID(),@t,'f0000000-0000-0000-0000-000000000001','c0000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000002','A01','2026-08-11','FINISHED','NORMAL','Tai kham DTD','SEED-P1P2','2026-08-11 07:30:00','2026-08-11 07:50:00','2026-08-11 07:52:00','2026-08-11 08:10:00','2026-08-11 07:30:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000002','c0000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000002','A02','2026-08-11','FINISHED','NORMAL','Kham moi','SEED-P1P2','2026-08-11 07:35:00','2026-08-11 08:15:00','2026-08-11 08:16:00','2026-08-11 08:40:00','2026-08-11 07:35:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000003','c0000000-0000-0000-0000-000000000002','e210a28b-062d-4d90-98f9-693936cbcc5d','B01','2026-08-11','FINISHED','NORMAL','Tai kham','SEED-P1P2','2026-08-11 08:00:00','2026-08-11 08:12:00','2026-08-11 08:13:00','2026-08-11 08:35:00','2026-08-11 08:00:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000004','c0000000-0000-0000-0000-000000000002','e210a28b-062d-4d90-98f9-693936cbcc5d','B02','2026-08-12','FINISHED','HIGH','Cap cuu nhe','SEED-P1P2','2026-08-12 09:00:00','2026-08-12 09:05:00','2026-08-12 09:06:00','2026-08-12 09:30:00','2026-08-12 09:00:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000005','c0000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000002','A03','2026-08-12','FINISHED','NORMAL','Tai kham DTD','SEED-P1P2','2026-08-12 09:10:00','2026-08-12 09:55:00','2026-08-12 09:57:00','2026-08-12 10:20:00','2026-08-12 09:10:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000006','c0000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000002','A04','2026-08-13','FINISHED','NORMAL','Tu van','SEED-P1P2','2026-08-13 10:00:00','2026-08-13 10:18:00','2026-08-13 10:19:00','2026-08-13 10:33:00','2026-08-13 10:00:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000007','c0000000-0000-0000-0000-000000000002','e210a28b-062d-4d90-98f9-693936cbcc5d','B03','2026-08-13','FINISHED','NORMAL','Tai kham','SEED-P1P2','2026-08-13 10:30:00','2026-08-13 11:20:00','2026-08-13 11:22:00','2026-08-13 11:45:00','2026-08-13 10:30:00',1),
 (UUID(),@t,'f0000000-0000-0000-0000-000000000008','c0000000-0000-0000-0000-000000000001','a0000000-0000-0000-0000-000000000002','A05','2026-08-14','FINISHED','NORMAL','Kham moi','SEED-P1P2','2026-08-14 08:00:00','2026-08-14 08:25:00','2026-08-14 08:27:00','2026-08-14 08:55:00','2026-08-14 08:00:00',1);

-- ---------------------------------------------------------------------------
-- O-03 / O-04: cap nhat 10 appointments — set doctor_ref + status + dua ve thang 8/2026
-- (goc: status='' , doctor_id=0). Cleanup se reset ve NULL/'' .
-- ---------------------------------------------------------------------------
UPDATE diab_his_sch_appointments SET doctor_ref='a0000000-0000-0000-0000-000000000002', status='CHECKED_IN', appointment_at='2026-08-11 08:00:00', updated_at='2026-08-11 08:02:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=1;
UPDATE diab_his_sch_appointments SET doctor_ref='a0000000-0000-0000-0000-000000000002', status='CHECKED_IN', appointment_at='2026-08-11 09:00:00', updated_at='2026-08-11 09:20:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=2;
UPDATE diab_his_sch_appointments SET doctor_ref='a0000000-0000-0000-0000-000000000002', status='NO_SHOW', appointment_at='2026-08-12 08:30:00', updated_at='2026-08-12 09:00:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=3;
UPDATE diab_his_sch_appointments SET doctor_ref='a0000000-0000-0000-0000-000000000002', status='CANCELLED', appointment_at='2026-08-12 10:00:00', updated_at='2026-08-12 09:50:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=4;
UPDATE diab_his_sch_appointments SET doctor_ref='e210a28b-062d-4d90-98f9-693936cbcc5d', status='CHECKED_IN', appointment_at='2026-08-13 08:30:00', updated_at='2026-08-13 08:40:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=5;
UPDATE diab_his_sch_appointments SET doctor_ref='e210a28b-062d-4d90-98f9-693936cbcc5d', status='NO_SHOW', appointment_at='2026-08-13 10:30:00', updated_at='2026-08-13 11:00:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=6;
UPDATE diab_his_sch_appointments SET doctor_ref='e210a28b-062d-4d90-98f9-693936cbcc5d', status='CHECKED_IN', appointment_at='2026-08-14 09:00:00', updated_at='2026-08-14 09:35:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=7;
UPDATE diab_his_sch_appointments SET doctor_ref='e210a28b-062d-4d90-98f9-693936cbcc5d', status='CANCELLED', appointment_at='2026-08-15 15:00:00', updated_at='2026-08-15 14:30:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=8;
UPDATE diab_his_sch_appointments SET doctor_ref='a0000000-0000-0000-0000-000000000002', status='CONFIRMED', appointment_at='2026-08-16 08:30:00', updated_at='2026-08-15 08:00:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=9;
UPDATE diab_his_sch_appointments SET doctor_ref='a0000000-0000-0000-0000-000000000002', status='CHECKED_IN', appointment_at='2026-08-16 10:00:00', updated_at='2026-08-16 10:05:00', note=CONCAT(COALESCE(note,''),' SEED-P1P2') WHERE tenant_id=@t AND id=10;

-- ---------------------------------------------------------------------------
-- D-02 / D-04: stock_movements (bang rong) — EXPORT nhieu ky cho phan tich
-- Mot so thuoc XUAT gan day (khong slow), mot so KHONG co export (slow > 90 ngay).
-- Nhieu thang cho D-04 XYZ (bien dong tieu thu).
-- ---------------------------------------------------------------------------
-- Drug 0001: xuat deu dan (X - on dinh), gan day => khong slow
INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reason, movement_at, created_at, branch_id) VALUES
 (@t,'630f7f48-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',100,5000,'SEED-P1P2','2026-06-05 09:00:00','2026-06-05 09:00:00',1),
 (@t,'630f7f48-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',110,5000,'SEED-P1P2','2026-07-05 09:00:00','2026-07-05 09:00:00',1),
 (@t,'630f7f48-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',95, 5000,'SEED-P1P2','2026-08-05 09:00:00','2026-08-05 09:00:00',1),
 (@t,'630f7f48-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',105,5000,'SEED-P1P2','2026-08-25 09:00:00','2026-08-25 09:00:00',1);
-- Drug 0002: xuat bien dong manh (Z), gan day => khong slow
INSERT INTO diab_his_pha_stock_movements (tenant_id, stock_id, warehouse_id, movement_type, quantity, unit_price, reason, movement_at, created_at, branch_id) VALUES
 (@t,'630f867c-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',10, 12000,'SEED-P1P2','2026-06-10 09:00:00','2026-06-10 09:00:00',1),
 (@t,'630f867c-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',200,12000,'SEED-P1P2','2026-07-15 09:00:00','2026-07-15 09:00:00',1),
 (@t,'630f867c-a599-11f1-9293-ee8160e16766','WH-MAIN','EXPORT',5,  12000,'SEED-P1P2','2026-08-20 09:00:00','2026-08-20 09:00:00',1);
-- (Cac stock/thuoc con lai KHONG co movement => slow-moving > 90 ngay, dung de test D-02)
-- Backdate created_at cua vai stock KHONG co export -> > 90 ngay khong xuat (test D-02).
-- Tag qua location='SLOW-P1P2' de cleanup revert.
UPDATE diab_his_pha_stock SET created_at='2026-02-01 00:00:00', location='SLOW-P1P2'
 WHERE tenant_id=@t AND id IN ('630f8818-a599-11f1-9293-ee8160e16766','630f8add-a599-11f1-9293-ee8160e16766','s0100000-0000-0000-0000-000000000004','s0100000-0000-0000-0000-000000000005');

-- ---------------------------------------------------------------------------
-- C-02 / C-04: complications json cho mot so assessment (bang co du lieu hba1c, complications NULL)
-- Format: mang chuoi ma bien chung. C-04 dung hba1c + co/khong bien chung.
-- ---------------------------------------------------------------------------
UPDATE diab_his_cli_diabetes_assessments SET complications = JSON_ARRAY('RETINOPATHY','NEPHROPATHY') WHERE tenant_id=@t AND id=1;
UPDATE diab_his_cli_diabetes_assessments SET complications = JSON_ARRAY('NEUROPATHY')                WHERE tenant_id=@t AND id=3;
UPDATE diab_his_cli_diabetes_assessments SET complications = JSON_ARRAY('NEPHROPATHY','CVD')         WHERE tenant_id=@t AND id=6;
UPDATE diab_his_cli_diabetes_assessments SET complications = JSON_ARRAY('RETINOPATHY')               WHERE tenant_id=@t AND id=8;
UPDATE diab_his_cli_diabetes_assessments SET complications = JSON_ARRAY('DIABETIC_FOOT')             WHERE tenant_id=@t AND id=10;

-- ---------------------------------------------------------------------------
-- F-03: them vai payment hoan tra co ly do (note) khac nhau de group-by co y nghia
-- Bam vao billing co san. Lay 3 billing dau tien co payment.
-- ---------------------------------------------------------------------------
INSERT INTO diab_his_bil_payments (id, tenant_id, billing_id, amount, method, status, note, refunded_amount, paid_at, paid_by, created_at, branch_id)
SELECT UUID(), @t, b.id, 0, 'CASH', 'REFUNDED', N'Khách đổi ý không dùng dịch vụ', 100000, '2026-08-15 10:00:00', 'e210a28b-062d-4d90-98f9-693936cbcc5d', '2026-08-15 10:00:00', 1
 FROM diab_his_bil_billing b WHERE b.tenant_id=@t AND b.deleted_at IS NULL ORDER BY b.created_at LIMIT 1;
INSERT INTO diab_his_bil_payments (id, tenant_id, billing_id, amount, method, status, note, refunded_amount, paid_at, paid_by, created_at, branch_id)
SELECT UUID(), @t, b.id, 0, 'BANK_TRANSFER', 'REFUNDED', N'Hủy chỉ định do chống chỉ định', 250000, '2026-08-18 14:00:00', '394ec0a7-ccdc-448b-9a1b-43356b8abbef', '2026-08-18 14:00:00', 1
 FROM diab_his_bil_billing b WHERE b.tenant_id=@t AND b.deleted_at IS NULL ORDER BY b.created_at LIMIT 1 OFFSET 1;
INSERT INTO diab_his_bil_payments (id, tenant_id, billing_id, amount, method, status, note, refunded_amount, paid_at, paid_by, created_at, branch_id)
SELECT UUID(), @t, b.id, 0, 'CASH', 'REFUNDED', N'Khách đổi ý không dùng dịch vụ', 75000, '2026-08-20 09:00:00', 'e210a28b-062d-4d90-98f9-693936cbcc5d', '2026-08-20 09:00:00', 1
 FROM diab_his_bil_billing b WHERE b.tenant_id=@t AND b.deleted_at IS NULL ORDER BY b.created_at LIMIT 1 OFFSET 2;

-- ---------------------------------------------------------------------------
-- D-03: prescription_items hop le (du lieu goc bi cat prescription_id='70000001', drug_id='0')
-- Seed item lien ket dung UUID prescription + drug that + line_total. Tag note='SEED-P1P2'.
-- ---------------------------------------------------------------------------
INSERT INTO diab_his_pha_prescription_items
 (id, tenant_id, prescription_id, drug_id, dosage, frequency, route, duration_days, quantity,
  drug_name, unit, unit_price, line_total, note, created_at)
VALUES
 (UUID(),@t,'70000001-0000-0000-0000-000000000001','d0000000-0000-0000-0000-000000000001','500mg','2 lần/ngày','PO',30,60,'Metformin 500mg','vien',500, 30000,'SEED-P1P2','2026-08-02 00:09:23'),
 (UUID(),@t,'70000001-0000-0000-0000-000000000001','d0000000-0000-0000-0000-000000000002','5mg','1 lần/ngày','PO',30,30,'Amlodipine 5mg','vien',1500,45000,'SEED-P1P2','2026-08-02 00:09:23'),
 (UUID(),@t,'70000001-0000-0000-0000-000000000003','d0000000-0000-0000-0000-000000000001','500mg','2 lần/ngày','PO',30,60,'Metformin 500mg','vien',500, 30000,'SEED-P1P2','2026-08-07 00:09:23'),
 (UUID(),@t,'70000001-0000-0000-0000-000000000003','d0000000-0000-0000-0000-000000000003','20mg','1 lần/ngày','PO',30,30,'Atorvastatin 20mg','vien',3500,105000,'SEED-P1P2','2026-08-07 00:09:23'),
 (UUID(),@t,'70000001-0000-0000-0000-000000000004','d0000000-0000-0000-0000-000000000001','500mg','2 lần/ngày','PO',30,60,'Metformin 500mg','vien',500, 30000,'SEED-P1P2','2026-08-10 00:09:23'),
 (UUID(),@t,'70000001-0000-0000-0000-000000000005','d0000000-0000-0000-0000-000000000002','5mg','1 lần/ngày','PO',30,30,'Amlodipine 5mg','vien',1500,45000,'SEED-P1P2','2026-08-14 00:09:23');

-- ---------------------------------------------------------------------------
-- C-04: dam bao co tang "Nguy co cao" — set assessment MOI NHAT cua BN 1 & 2 = HbA1c cao + bien chung
-- ---------------------------------------------------------------------------
UPDATE diab_his_cli_diabetes_assessments a
  JOIN (SELECT patient_id, MAX(assessed_at) mx FROM diab_his_cli_diabetes_assessments WHERE tenant_id=@t AND patient_id IN (1,2) GROUP BY patient_id) x
    ON a.patient_id=x.patient_id AND a.assessed_at=x.mx
  SET a.hba1c=9.60, a.complications=JSON_ARRAY('NEPHROPATHY')
  WHERE a.tenant_id=@t;

SELECT 'SEED-P1P2 done' AS status;
