# Evidence P0-1 — Thuc thi 2FA that o backend (2026-08-30, API test tren build moi)

Luu y: fix them 2 bug chan 2FA phat hien khi verify:
  (a) cot two_fa_recovery_codes kieu JSON khong chua duoc ciphertext -> migration 9186 doi sang TEXT (enable 2FA truoc do luon 500).
  (b) request body verify phai khop camelCase mfaPendingToken (login tra camelCase) -> [JsonPropertyName] tren Verify2faRequest.

## Buoc 1: Login bacsi.test (DA bat 2FA) -> KHONG cap token, tra mfaPendingToken
```json
{
  "requires2fa": true,
  "accessToken_empty": true,
  "has_mfaPendingToken": true
}
```

## Buoc 2: verify ma SAI -> 401 AUTH_MFA_INVALID_CODE
```json
{
  "code": "AUTH_MFA_INVALID_CODE",
  "message": "Mã xác thực 2 lớp không đúng",
  "details": {}
}
```

## Buoc 3: verify ma DUNG (TOTP) -> cap AccessToken + RefreshToken day du
```json
{
  "accessToken_len": 2333,
  "has_refreshToken": true,
  "requires2fa": false,
  "email": "bacsi.test@prodiab.test"
}
```

## Buoc 4: Login letan.test (CHUA 2FA, khong mandatory) -> token day du (khong bi anh huong)
```json
{
  "requires2fa": false,
  "mfaSetupRequired": false,
  "accessToken_len": 1484
}
```

## Buoc 5: Login qc.admin (role admin BAT BUOC 2FA, chua bat) -> CHAN token day du, tra mfaSetupToken
```json
{
  "mfaSetupRequired": true,
  "accessToken_empty": true,
  "has_mfaSetupToken": true
}
```
