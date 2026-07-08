#!/usr/bin/env bash
CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
echo "-- sample encounter ids --"
mysql $CONN -t -e "SELECT id, LENGTH(id) AS len, encounter_type, status FROM diab_his_enc_encounters LIMIT 15;" 2>/dev/null
echo "-- total encounters --"
mysql $CONN -N -e "SELECT COUNT(*) FROM diab_his_enc_encounters;" 2>/dev/null
echo "-- ids NOT matching GUID hex pattern --"
mysql $CONN -N -e "SELECT id FROM diab_his_enc_encounters WHERE id NOT REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';" 2>/dev/null
