# Evidence P0-1 — Browser test luồng 2FA login (2026-08-30)

Frontend Next.js dev (localhost:3001) tro backend build moi (localhost:5199). Tai khoan test bacsi.test da bat 2FA (secret sinh qua me/2fa/setup + me/2fa/enable that).

| Buoc | Thao tac | Ket qua quan sat tren browser |
|---|---|---|
| 1 | Login bacsi.test@prodiab.test / Test@123 | KHONG vao dashboard. Hien man "Xac thuc 2 lop — Nhap ma 6 so tu ung dung Authenticator, hoac ma khoi phuc dang xxxxx-xxxxx" + o nhap ma + nut "Xac minh" + link "Quay lai dang nhap" |
| 2 | Nhap ma SAI 000000 -> Xac minh | Toast do "Ma xac thuc 2 lop khong dung". Van o man 2FA, KHONG vao dashboard |
| 3 | Nhap ma DUNG (TOTP hien tai) -> Xac minh | Vao dashboard "Tong quan / Hoat dong phong kham hom nay" — dang nhap thanh cong |
| 4 | Logout, login letan.test@prodiab.test (CHUA bat 2FA) | Vao thang dashboard, KHONG hoi ma 2FA — tai khoan khong bat 2FA khong bi anh huong |

Ket luan: 2FA da duoc THUC THI THAT o backend (token day du chi cap sau khi verify TOTP dung), khop yeu cau P0-1. Screenshot tung buoc da chup trong phien lam viec.

Luu y trang thai: sau test da tat lai 2FA cho bacsi.test (two_fa_enabled=0) de khoi phuc seed sach.

## Bug phu phat hien & fix trong dot nay (chan 2FA hoat dong):
- Cot two_fa_recovery_codes kieu JSON khong luu duoc ciphertext -> migration 9186 doi sang TEXT + UserConfiguration bo HasColumnType("json"). Truoc fix: POST me/2fa/enable luon 500 "Invalid JSON text".
- Verify2faRequest phai nhan camelCase mfaPendingToken (khop token login tra ve) -> them [JsonPropertyName].
