#!/usr/bin/env bash
# Smoke test runner — Pro-Diab HIS
# Phiên: 2026-05-23  |  Lead: Lành  |  Runner: Phượng
# Usage: ./smoke-test.sh | tee smoke-2026-05-23.tsv
# Output TSV cols: module | action | http_code | error_code | result | note

set -u
BASE="${BASE:-http://localhost:5000}"
EMAIL="${EMAIL:-admin@prodiab.local}"
PASS="${PASS:-Admin@123}"

# ---------- helpers ----------
row() { printf "%s\t%s\t%s\t%s\t%s\t%s\n" "$1" "$2" "$3" "$4" "$5" "$6"; }

call() {
  # $1=module $2=action $3=method $4=path $5=body(json or '') $6=note
  local mod="$1" act="$2" m="$3" p="$4" body="$5" note="$6"
  local tmp http err res
  tmp=$(mktemp)
  if [ -z "$body" ]; then
    http=$(curl -sk -o "$tmp" -w "%{http_code}" -X "$m" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Tenant-Id: $TENANT" \
      "$BASE$p")
  else
    http=$(curl -sk -o "$tmp" -w "%{http_code}" -X "$m" \
      -H "Authorization: Bearer $TOKEN" \
      -H "X-Tenant-Id: $TENANT" \
      -H "Content-Type: application/json" \
      -d "$body" "$BASE$p")
  fi
  err=$(jq -r '.error.code // ""' "$tmp" 2>/dev/null)
  if [[ "$http" =~ ^2 ]]; then res="PASS"
  elif grep -qE "^(drug_master|pha_|bil_|cli_lab_results|sec_audit_logs|bhyt_export|notif_)" <<<"$note"; then res="EXPECTED_FAIL"
  else res="FAIL"
  fi
  row "$mod" "$act" "$http" "$err" "$res" "$note"
  rm -f "$tmp"
}

# ---------- login ----------
LOGIN_RES=$(curl -sk -X POST "$BASE/api/v1/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"$EMAIL\",\"password\":\"$PASS\"}")
TOKEN=$(echo "$LOGIN_RES" | jq -r '.data.accessToken // .accessToken // .token // empty')
TENANT=$(echo "$LOGIN_RES" | jq -r '.data.tenantId // .tenantId // empty')
if [ -z "$TOKEN" ]; then
  echo "LOGIN_FAILED: $LOGIN_RES" >&2; exit 1
fi

# header
row "module" "action" "http" "error_code" "result" "note"

# ---------- P0 BLOCKER ----------
call Auth         me      GET  /api/v1/auth/me                  ''  p0
call Dashboard    summary GET  /api/v1/dashboard/summary        ''  p0
call Patient      list    GET  /api/v1/patients?page=1          ''  p0
PID=$(curl -sk -H "Authorization: Bearer $TOKEN" -H "X-Tenant-Id: $TENANT" "$BASE/api/v1/patients?page=1" | jq -r '.data[0].id // empty')
call Patient      detail  GET  /api/v1/patients/$PID            ''  p0
call Patient      create  POST /api/v1/patients '{"fullName":"Smoke Test","gender":"M","dob":"1990-01-01","phone":"0900000000"}' p0
call Patient      update  PUT  /api/v1/patients/$PID '{"phone":"0900000001"}' p0
call Encounter    list    GET  /api/v1/encounters?page=1        ''  p0
EID=$(curl -sk -H "Authorization: Bearer $TOKEN" -H "X-Tenant-Id: $TENANT" "$BASE/api/v1/encounters?page=1" | jq -r '.data[0].id // empty')
call Encounter    detail  GET  /api/v1/encounters/$EID          ''  p0
call VitalSign    list    GET  /api/v1/encounters/$EID/vitals   ''  p0
call VitalSign    create  POST /api/v1/encounters/$EID/vitals '{"systolic":120,"diastolic":80,"pulse":72,"tempC":36.7}' p0
call DiabetesA    list    GET  /api/v1/diabetes-assessments     ''  p0

# ---------- P1 CRITICAL ----------
call Tenant       list    GET  /api/v1/tenants                  ''  p1
call User         list    GET  /api/v1/users                    ''  p1
call Role         list    GET  /api/v1/roles                    ''  p1
call Permission   list    GET  /api/v1/permissions              ''  p1
call AuditLog     list    GET  /api/v1/audit-logs               ''  sec_audit_logs-schema-old
call Allergy      list    GET  /api/v1/patients/$PID/allergies  ''  p1
call Insurance    list    GET  /api/v1/patients/$PID/insurances ''  p1
call EmContact    list    GET  /api/v1/patients/$PID/emergency-contacts '' p1
call Consent      list    GET  /api/v1/patients/$PID/consents   ''  p1
call Reception    list    GET  /api/v1/receptions               ''  p1
call EMR          get     GET  /api/v1/encounters/$EID/emr      ''  p1
call Diagnosis    list    GET  /api/v1/encounters/$EID/diagnoses '' p1
call LabOrder     list    GET  /api/v1/lab-orders               ''  p1
call RadOrder     list    GET  /api/v1/rad-orders               ''  p1
call LabResult    list    GET  /api/v1/lab-orders/_any/results  ''  cli_lab_results-schema
call LabPartner   list    GET  /api/v1/lab-partners             ''  p1
call Prescription list    GET  /api/v1/prescriptions            ''  pha_prescriptions-schema
call Prescription create  POST /api/v1/prescriptions "{\"encounterId\":\"$EID\",\"items\":[]}" pha_prescriptions-schema
call Drug         list    GET  /api/v1/drugs                    ''  drug_master-schema
call Drug         create  POST /api/v1/drugs '{"code":"SMK01","name":"Smoke Drug","unit":"viên"}' drug_master-schema
call Stock        list    GET  /api/v1/stocks                   ''  pha_stock_lots-schema
call Dispense     list    GET  /api/v1/dispenses                ''  p1
call Service      list    GET  /api/v1/services                 ''  p1
call Billing      list    GET  /api/v1/billings                 ''  bil_billing-schema
call Payment      list    GET  /api/v1/payments                 ''  p1
call EInvoice     list    GET  /api/v1/einvoices                ''  p1
call CashierShift list    GET  /api/v1/cashier/shifts           ''  p1
call BHYTExport   list    GET  /api/v1/bhyt/exports             ''  bhyt_export-xsd
call BHYTExport   create  POST /api/v1/bhyt/exports '{"from":"2026-05-01","to":"2026-05-22"}' bhyt_export-xsd

# ---------- P2 NICE ----------
call Notification list    GET  /api/v1/notifications            ''  notif_-not-exist
call ApiPartner   list    GET  /api/v1/api-partners             ''  p2
call PortalAuth   me      GET  /api/v1/portal/me                ''  p2
call Reports      revenue GET  /api/v1/reports/revenue?from=2026-05-01&to=2026-05-22 '' p2

# ---------- DELETE smoke patient cuối ----------
call Patient      delete  DELETE /api/v1/patients/$PID          ''  p0-cleanup
