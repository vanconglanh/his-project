# E2E Bao cao — Pro-Diab HIS

Moi truong: https://his.diab.com.vn/api/v1 · khoang ngay: 2026-01-01..2026-12-31

**Ket qua: 58/61 PASS**

| # | Bao cao | Loai | Ket qua | Chi tiet |
|---|---|---|---|---|
| 1 | [Financial] revenue-daily — BÁO CÁO DOANH THU NGÀY | engine | PASS | data:200 cols=15 rows=0 kpis=3 \| pdf:200 PDF 124692B -> engine-revenue-daily.pdf |
| 2 | [Financial] refund-receipts — BÁO CÁO HOÀN TRẢ PHIẾU THU | engine | PASS | data:200 cols=7 rows=0 kpis=2 \| pdf:200 PDF 123191B -> engine-refund-receipts.pdf |
| 3 | [Financial] void-receipts — BÁO CÁO HỦY PHIẾU THU | engine | PASS | data:200 cols=7 rows=0 kpis=2 \| pdf:200 PDF 123410B -> engine-void-receipts.pdf |
| 4 | [Financial] advances — BÁO CÁO TẠM ỨNG | engine | PASS | data:200 cols=7 rows=0 kpis=2 \| pdf:200 PDF 122764B -> engine-advances.pdf |
| 5 | [Financial] fee-detail — BÁO CÁO CHI TIẾT VIỆN PHÍ | engine | PASS | data:200 cols=9 rows=10 kpis=2 \| pdf:200 PDF 146820B -> engine-fee-detail.pdf |
| 6 | [Financial] lab-summary — BÁO CÁO TỔNG HỢP XÉT NGHIỆM | engine | PASS | data:200 cols=5 rows=4 kpis=2 \| pdf:200 PDF 135000B -> engine-lab-summary.pdf |
| 7 | [Financial] revenue-monthly — BÁO CÁO DOANH THU THEO THÁNG | engine | PASS | data:200 cols=7 rows=2 kpis=2 \| pdf:200 PDF 132470B -> engine-revenue-monthly.pdf |
| 8 | [Financial] debts — BÁO CÁO CÔNG NỢ BỆNH NHÂN | engine | PASS | data:200 cols=7 rows=4 kpis=2 \| pdf:200 PDF 138107B -> engine-debts.pdf |
| 9 | [Financial] so-quy-tien-mat — SỔ QUỸ TIỀN MẶT | engine | PASS | data:200 cols=7 rows=4 kpis=3 \| pdf:200 PDF 137646B -> engine-so-quy-tien-mat.pdf |
| 10 | [Clinical] ctdv-kham-benh — BÁO CÁO CTDV BN KHÁM BỆNH | engine | PASS | data:200 cols=7 rows=26 kpis=2 \| pdf:200 PDF 153271B -> engine-ctdv-kham-benh.pdf |
| 11 | [Clinical] ctdv-sieu-am — BÁO CÁO CTDV BN SIÊU ÂM | engine | PASS | data:200 cols=7 rows=0 kpis=2 \| pdf:200 PDF 124300B -> engine-ctdv-sieu-am.pdf |
| 12 | [Clinical] ctdv-xquang — BÁO CÁO CTDV BN XQUANG | engine | PASS | data:200 cols=7 rows=0 kpis=2 \| pdf:200 PDF 123634B -> engine-ctdv-xquang.pdf |
| 13 | [Clinical] ctdv-noi-soi — BÁO CÁO CTDV BN NỘI SOI | engine | PASS | data:200 cols=7 rows=0 kpis=2 \| pdf:200 PDF 123230B -> engine-ctdv-noi-soi.pdf |
| 14 | [Clinical] ctdv-thu-thuat — BÁO CÁO CTDV BN THỦ THUẬT | engine | PASS | data:200 cols=6 rows=0 kpis=2 \| pdf:200 PDF 123075B -> engine-ctdv-thu-thuat.pdf |
| 15 | [Clinical] ctdv-xet-nghiem — BÁO CÁO CTDV BN XÉT NGHIỆM | engine | PASS | data:200 cols=7 rows=10 kpis=2 \| pdf:200 PDF 142640B -> engine-ctdv-xet-nghiem.pdf |
| 16 | [Clinical] so-kham-benh — BÁO CÁO SỔ KHÁM BỆNH | engine | PASS | data:200 cols=9 rows=26 kpis=1 \| pdf:200 PDF 154516B -> engine-so-kham-benh.pdf |
| 17 | [Clinical] so-sieu-am — BÁO CÁO SỔ SIÊU ÂM | engine | PASS | data:200 cols=7 rows=0 kpis=1 \| pdf:200 PDF 121138B -> engine-so-sieu-am.pdf |
| 18 | [Clinical] so-xquang — BÁO CÁO SỔ XQUANG | engine | PASS | data:200 cols=7 rows=0 kpis=1 \| pdf:200 PDF 121320B -> engine-so-xquang.pdf |
| 19 | [Clinical] so-noi-soi — BÁO CÁO SỔ NỘI SOI | engine | PASS | data:200 cols=7 rows=0 kpis=1 \| pdf:200 PDF 120342B -> engine-so-noi-soi.pdf |
| 20 | [Clinical] so-thu-thuat — BÁO CÁO SỔ THỦ THUẬT | engine | PASS | data:200 cols=6 rows=0 kpis=1 \| pdf:200 PDF 121080B -> engine-so-thu-thuat.pdf |
| 21 | [Clinical] so-xet-nghiem — BÁO CÁO SỔ XÉT NGHIỆM | engine | PASS | data:200 cols=7 rows=10 kpis=1 \| pdf:200 PDF 140597B -> engine-so-xet-nghiem.pdf |
| 22 | [Clinical] so-dien-tim — BÁO CÁO SỔ ĐIỆN TIM | engine | PASS | data:200 cols=7 rows=0 kpis=1 \| pdf:200 PDF 121215B -> engine-so-dien-tim.pdf |
| 23 | [Clinical] benh-dien-tien — BÁO CÁO BỆNH DIỄN TIẾN | engine | PASS | data:200 cols=7 rows=26 kpis=1 \| pdf:200 PDF 141149B -> engine-benh-dien-tien.pdf |
| 24 | [Statistics] luot-kham-theo-bs — BÁO CÁO LƯỢT KHÁM THEO BÁC SĨ | engine | PASS | data:200 cols=6 rows=3 kpis=2 \| pdf:200 PDF 139322B -> engine-luot-kham-theo-bs.pdf |
| 25 | [Statistics] luot-kham-theo-pk — BÁO CÁO LƯỢT KHÁM THEO PHÒNG KHÁM | engine | PASS | data:200 cols=4 rows=3 kpis=2 \| pdf:200 PDF 135641B -> engine-luot-kham-theo-pk.pdf |
| 26 | [Statistics] icd10-stats — BÁO CÁO THỐNG KÊ ICD-10 | engine | PASS | data:200 cols=5 rows=14 kpis=2 \| pdf:200 PDF 144468B -> engine-icd10-stats.pdf |
| 27 | [Statistics] top-drugs — BÁO CÁO TOP THUỐC KÊ NHIỀU | engine | PASS | data:200 cols=6 rows=3 kpis=2 \| pdf:200 PDF 133719B -> engine-top-drugs.pdf |
| 28 | [Statistics] top-services — BÁO CÁO TOP DỊCH VỤ | engine | PASS | data:200 cols=5 rows=2 kpis=2 \| pdf:200 PDF 133353B -> engine-top-services.pdf |
| 29 | [Statistics] patient-source-summary — BÁO CÁO TỔNG HỢP NGUỒN KHÁCH | engine | PASS | data:200 cols=3 rows=1 kpis=2 \| pdf:200 PDF 128519B -> engine-patient-source-summary.pdf |
| 30 | [Statistics] cls-indication-stats — BÁO CÁO THỐNG KÊ CHỈ ĐỊNH CLS | engine | PASS | data:200 cols=3 rows=4 kpis=1 \| pdf:200 PDF 134265B -> engine-cls-indication-stats.pdf |
| 31 | [Statistics] luot-kham-theo-gio — BÁO CÁO LƯỢT KHÁM THEO GIỜ | engine | PASS | data:200 cols=3 rows=4 kpis=2 \| pdf:200 PDF 129954B -> engine-luot-kham-theo-gio.pdf |
| 32 | [Statistics] ty-le-no-show — BÁO CÁO TỶ LỆ NO-SHOW LỊCH HẸN | engine | PASS | data:200 cols=3 rows=0 kpis=2 \| pdf:200 PDF 123266B -> engine-ty-le-no-show.pdf |
| 33 | [Statistics] su-dung-khang-sinh — BÁO CÁO THỐNG KÊ SỬ DỤNG KHÁNG SINH | engine | PASS | data:200 cols=4 rows=0 kpis=2 \| pdf:200 PDF 124156B -> engine-su-dung-khang-sinh.pdf |
| 34 | [Statistics] tat-cls — BÁO CÁO THỜI GIAN TRẢ KẾT QUẢ CLS (TAT) | engine | PASS | data:200 cols=4 rows=4 kpis=2 \| pdf:200 PDF 137730B -> engine-tat-cls.pdf |
| 35 | [Bhyt] nghi-huong-bhxh — BÁO CÁO NGHỈ HƯỞNG BHXH | engine | PASS | data:200 cols=9 rows=0 kpis=1 \| pdf:200 PDF 122075B -> engine-nghi-huong-bhxh.pdf |
| 36 | [Pharmacy] ton-kho — BÁO CÁO TỒN KHO HIỆN TẠI | engine | PASS | data:200 cols=6 rows=30 kpis=2 \| pdf:200 PDF 154847B -> engine-ton-kho.pdf |
| 37 | [Pharmacy] the-kho-lo — BÁO CÁO THẺ KHO CHI TIẾT THEO LÔ | engine | PASS | data:200 cols=9 rows=50 kpis=2 \| pdf:200 PDF 176574B -> engine-the-kho-lo.pdf |
| 38 | [Pharmacy] thuoc-can-date — BÁO CÁO THUỐC CẬN DATE / HẾT HẠN | engine | PASS | data:200 cols=6 rows=19 kpis=2 \| pdf:200 PDF 147371B -> engine-thuoc-can-date.pdf |
| 39 | [Pharmacy] xuat-nhap-ton — BÁO CÁO XUẤT - NHẬP - TỒN | engine | PASS | data:200 cols=6 rows=0 kpis=2 \| pdf:200 PDF 122484B -> engine-xuat-nhap-ton.pdf |
| 40 | [Pharmacy] danh-muc-thuoc — BÁO CÁO DANH MỤC THUỐC | engine | PASS | data:200 cols=9 rows=40 kpis=1 \| pdf:200 PDF 162269B -> engine-danh-muc-thuoc.pdf |
| 41 | [Pharmacy] thuoc-kiem-soat — BÁO CÁO THUỐC KIỂM SOÁT ĐẶC BIỆT | engine | PASS | data:200 cols=6 rows=0 kpis=2 \| pdf:200 PDF 123159B -> engine-thuoc-kiem-soat.pdf |
| 42 | [Pharmacy] thuoc-duoi-dinh-muc — BÁO CÁO THUỐC DƯỚI ĐỊNH MỨC TỒN | engine | PASS | data:200 cols=6 rows=17 kpis=2 \| pdf:200 PDF 144178B -> engine-thuoc-duoi-dinh-muc.pdf |
| 43 | [Pharmacy] kiem-ke-kho — BÁO CÁO KIỂM KÊ KHO | engine | PASS | data:200 cols=9 rows=5 kpis=2 \| pdf:200 PDF 139511B -> engine-kiem-ke-kho.pdf |
| 44 | revenue | dedicated | PASS | http:200 {"data":{"period":"MONTH","from":"2026-01-01","to":"2026-12-31","total_revenue":1460000.00,"total_invoices":2,"total_refunds":0,"net_revenue":1460000. |
| 45 | revenue/by-doctor | dedicated | FAIL | http:500 {"error":{"code":"INTERNAL_ERROR","message":"Loi he thong, vui long thu lai sau","details":{}}} |
| 46 | revenue/by-service | dedicated | PASS | http:200 {"data":[]} |
| 47 | revenue/by-payment-method | dedicated | PASS | http:200 {"data":[]} |
| 48 | cashier/daily-summary | dedicated | FAIL | http:500 {"error":{"code":"INTERNAL_ERROR","message":"Loi he thong, vui long thu lai sau","details":{}}} |
| 49 | debts/aging | dedicated | PASS | http:200 {"data":{"bucket_0_30":685000.00,"bucket_30_60":0,"bucket_60_90":0,"bucket_over_90":0,"total":685000.00,"details":[{"bill_no":"HD-2026-00003","patient |
| 50 | bhyt/summary | dedicated | FAIL | http:500 {"error":{"code":"INTERNAL_ERROR","message":"Loi he thong, vui long thu lai sau","details":{}}} |
| 51 | clinical/diabetes-cohort | dedicated | PASS | http:200 {"data":{"period_key":"2026-01","total_patients":2,"avg_hb_a1c":0,"pct_controlled":100.0,"pct_uncontrolled":0.0,"buckets":[{"label":"<6","patient_coun |
| 52 | diabetes/cohort | dedicated | PASS | http:200 {"data":{"as_of":"2026-07-08","total_patients":1,"by_type":{"t1":0,"t2":1,"gdm":0},"hba1c_distribution":{"lt_7":0,"between_7_8":0,"between_8_9":0,"gt_ |
| 53 | encounters/count | dedicated | PASS | http:200 {"data":[{"period_label":"2026-06","count":15},{"period_label":"2026-07","count":11}]} |
| 54 | diagnoses/top | dedicated | PASS | http:200 {"data":[{"icd10_code":"E11.9","icd10_name":"Đái tháo đường type 2 không có biến chứng","count":5,"percentage":25.0},{"icd10_code":"I10", |
| 55 | clinical/visits | dedicated | PASS | http:200 {"data":[{"encounter_id":"5ada2a2f-b3ae-4cc5-99b9-da757c93c7fd","patient_name":"Nguyen Van Benh","doctor_name":"BS. Quản trị viên Hệ thống"," |
| 56 | clinical/icd10 | dedicated | PASS | http:200 {"data":[{"icd10_code":"E11.9","icd10_name":"Đái tháo đường type 2 không có biến chứng","count":5,"percentage":25.0},{"icd10_code":"I10", |
| 57 | pharmacy/top-drugs | dedicated | PASS | http:200 {"data":[{"drug_id":"d0000000-0000-0000-0000-000000000001","drug_name":"","active_ingredient":"Metformin HCl","quantity_dispensed":120,"total_revenue" |
| 58 | pharmacy/inventory-value | dedicated | PASS | http:200 {"data":{"total_value":13243600.00,"total_skus":28,"by_category":[{"category":"Đái tháo đường","value":5468000.00,"sku_count":4},{"category":"K |
| 59 | A4 financial/pdf | pdf-a4 | PASS | http:200 137049B |
| 60 | A4 clinical/pdf | pdf-a4 | PASS | http:200 145684B |
| 61 | A4 pharmacy/pdf | pdf-a4 | PASS | http:200 171015B |
