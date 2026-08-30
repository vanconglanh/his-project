# Evidence — Ẩn/hiện + override giá theo chi nhánh (thuốc + dịch vụ)
Ngày: 2026-08-30. Tài khoản: qc.admin@prodiab.test (tenant 1). Stack local (docker compose), migration 9185 đã apply.

## Dữ liệu seed
- Thuốc: TH001 "Paracetamol 500mg" (hiện mọi CN), TH002 "Amoxicillin 500mg" (sẽ ẩn ở CN2). Có TH001..TH009.
- Dịch vụ: DV-HIEN "Siêu âm ổ bụng", DV-AN "Chụp X-quang phổi" (sẽ ẩn ở CN2).
- Chi nhánh: 1 (mặc định), 2 (Quận 7).

## Kịch bản THUỐC (DrugAutocomplete = GET /api/v1/drugs/search)
1. Baseline (chưa override): branch1 và branch2 đều thấy TH001..TH009.
2. Tạo override ẩn TH002 ở branch2:
   POST /api/v1/drug-price-overrides {drug_id:TH002, scope:BRANCH, branch_id:2, is_active:false, ...} -> HTTP 201.
3. Sau override:
   - search @branch1 -> ['TH001','TH002',...] (TH002 in: True)  ✅ VẪN HIỆN
   - search @branch2 -> ['TH001','TH003',...] (TH002 in: False) ✅ ĐÃ ẨN
4. PRICE_OVERLAP: tạo override trùng khoảng cùng thuốc/branch -> HTTP 409, code=PRICE_OVERLAP ✅

## Kịch bản DỊCH VỤ (GET /api/v1/services/search)
1. Tạo override ẩn DV-AN ở branch2 -> HTTP 201.
2. search @branch1 -> ['DV-AN','DV-HIEN'] (DV-AN in: True)  ✅ VẪN HIỆN
   search @branch2 -> ['DV-HIEN']            (DV-AN in: False) ✅ ĐÃ ẨN

## Kết luận
- Cờ is_active theo chi nhánh hoạt động đúng cho cả thuốc và dịch vụ: item bị tắt ở 1 chi nhánh không xuất hiện
  trong autocomplete tại chi nhánh đó nhưng vẫn hiện ở chi nhánh khác.
- Chống trùng lặp (PRICE_OVERLAP) trả 409 rõ ràng.
- Branch context lấy từ header X-Branch-Id (BranchScopeMiddleware); resolver + filter dùng chung cho 2 loại item.
