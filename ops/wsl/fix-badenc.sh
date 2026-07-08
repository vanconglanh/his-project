#!/usr/bin/env bash
# Soft-delete encounter co id khong phai GUID hex ('e2eenc01...') — lam EF Guid.TryParse fail -> list encounters 500.
CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
mysql $CONN -e "UPDATE diab_his_enc_encounters SET deleted_at = NOW() WHERE id NOT REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$' AND deleted_at IS NULL;" 2>/dev/null
echo "-- remaining bad-id active encounters (expect 0) --"
mysql $CONN -N -e "SELECT COUNT(*) FROM diab_his_enc_encounters WHERE deleted_at IS NULL AND id NOT REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';" 2>/dev/null
echo "-- active encounters now --"
mysql $CONN -N -e "SELECT COUNT(*) FROM diab_his_enc_encounters WHERE deleted_at IS NULL;" 2>/dev/null
echo DONE
