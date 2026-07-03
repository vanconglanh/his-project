CONN="-h 127.0.0.1 -uprodiab -pprodiab_dev_2026 prodiab_his"
mysql $CONN -N -e "SELECT GROUP_CONCAT(column_name ORDER BY ordinal_position) FROM information_schema.columns WHERE table_schema=DATABASE() AND table_name='diab_his_enc_encounters';" 2>/dev/null
