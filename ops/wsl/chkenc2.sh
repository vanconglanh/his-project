CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
echo "-- all encounter ids --"
mysql $CONN -t -e "SELECT id, encounter_type, status, LENGTH(id) len FROM diab_his_enc_encounters LIMIT 20;" 2>/dev/null
echo "-- non-GUID ids (len != 36 or contains non-hex) --"
mysql $CONN -N -e "SELECT id FROM diab_his_enc_encounters WHERE LENGTH(id)<>36 OR id NOT REGEXP '^[0-9a-fA-F-]{36}\$';" 2>/dev/null
