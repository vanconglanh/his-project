-- Cleanup seed_reports_p1p2_tenant1.sql (tenant 1). Chay khi khong con can data verify.
SET @t := 1;
DELETE FROM diab_his_sec_audit_logs           WHERE tenant_id=@t AND request_id='SEED-P1P2';
DELETE FROM diab_his_pkg_entitlement_balances WHERE tenant_id=@t AND created_by='SEED-P1P2';
DELETE FROM diab_his_rcp_queue_tickets        WHERE tenant_id=@t AND note='SEED-P1P2';
DELETE FROM diab_his_pha_stock_movements      WHERE tenant_id=@t AND reason='SEED-P1P2';
DELETE FROM diab_his_bil_payments             WHERE tenant_id=@t AND status='REFUNDED' AND paid_at IN ('2026-08-15 10:00:00','2026-08-18 14:00:00','2026-08-20 09:00:00') AND amount=0;
-- Reset UPDATE (appointments ve trang thai goc: status='', doctor_ref NULL)
UPDATE diab_his_sch_appointments SET status='PENDING', doctor_ref=NULL, note=REPLACE(note,' SEED-P1P2','') WHERE tenant_id=@t AND note LIKE '%SEED-P1P2%';
DELETE FROM diab_his_pha_prescription_items WHERE tenant_id=@t AND note='SEED-P1P2';
-- Goc: moi assessment tenant 1 deu co complications=NULL -> revert toan bo. (hba1c da sua tren 2 dong latest cua BN 1,2 khong khoi phuc — du lieu dev, chap nhan.)
UPDATE diab_his_cli_diabetes_assessments SET complications=NULL WHERE tenant_id=@t;
UPDATE diab_his_pha_stock SET created_at='2026-09-01 00:09:23', location=NULL WHERE tenant_id=@t AND location='SLOW-P1P2';
SELECT 'CLEANUP-P1P2 done' AS status;
