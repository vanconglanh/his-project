#!/bin/bash
# Harness: dung MySQL 8 sach, nap base dump (strip GTID), apply migrations, log loi tung file.
set -u
REPO="/d/_Project/08.ATDS/02.Onetech/202501_CV10/_project/git/screen/_git/atds/his-project/.claude/worktrees/agent-adb53e97bf2408bb3"
DB="/d/_Project/08.ATDS/02.Onetech/202501_CV10/_project/git/screen/_git/atds/his-project/.claude/worktrees/agent-adb53e97bf2408bb3/db"
CT=prodiab_mig_test
PASS=root_dev
DBNAME=prodiab_his
LOGDIR="$1"   # evidence output dir
mkdir -p "$LOGDIR"
SUMMARY="$LOGDIR/summary.log"
: > "$SUMMARY"

mysqlx() { docker exec -i $CT mysql -uroot -p$PASS --default-character-set=utf8mb4 "$@" 2>&1; }

echo "=== [1] Nap base dump (strip GTID_PURGED) ===" | tee -a "$SUMMARY"
mysqlx -e "DROP DATABASE IF EXISTS $DBNAME; CREATE DATABASE $DBNAME CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci;" >>"$SUMMARY" 2>&1
BASE_ERR=0
for f in $(ls "$DB"/diab_his_*.sql | sort); do
  bn=$(basename "$f")
  out=$(grep -v 'SET @@GLOBAL.GTID_PURGED' "$f" | docker exec -i $CT mysql -uroot -p$PASS --default-character-set=utf8mb4 "$DBNAME" 2>&1)
  if echo "$out" | grep -qi 'ERROR'; then
    echo "BASE-FAIL $bn: $out" | tee -a "$SUMMARY"
    BASE_ERR=$((BASE_ERR+1))
  fi
done
echo "Base dump loaded, errors=$BASE_ERR" | tee -a "$SUMMARY"

echo "=== [2] Apply migrations ===" | tee -a "$SUMMARY"
FAIL=0; OK=0
for f in $(ls "$DB"/migrations/*.sql | sort); do
  bn=$(basename "$f")
  out=$(docker exec -i $CT mysql -uroot -p$PASS --default-character-set=utf8mb4 "$DBNAME" < "$f" 2>&1)
  if echo "$out" | grep -qi 'ERROR'; then
    echo "FAIL $bn" | tee -a "$SUMMARY"
    echo "$out" | grep -i 'ERROR' | head -5 | sed 's/^/    /' | tee -a "$SUMMARY"
    FAIL=$((FAIL+1))
  else
    OK=$((OK+1))
  fi
done
echo "=== RESULT: OK=$OK FAIL=$FAIL (base_err=$BASE_ERR) ===" | tee -a "$SUMMARY"
