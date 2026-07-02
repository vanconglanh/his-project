#!/usr/bin/env bash
# wait-for-it.sh — chuan util, nguon: https://github.com/vishnubob/wait-for-it
# Kiem tra TCP host:port san sang truoc khi chay lenh tiep theo
# Su dung: ./wait-for-it.sh host:port [-t timeout] [-- command args]
# LF line ending, UTF-8 no BOM

WAITFORIT_cmdname=${0##*/}

echoerr() { if [[ $WAITFORIT_QUIET -ne 1 ]]; then echo "$@" 1>&2; fi }

usage() {
    cat << USAGE >&2
Su dung: $WAITFORIT_cmdname host:port [-s] [-t timeout] [-- command args]
    -h HOST | --host=HOST       Host hoac IP can kiem tra
    -p PORT | --port=PORT       Port TCP can kiem tra
    -s | --strict               Chi chay lenh khi ket noi thanh cong
    -q | --quiet                Im lang, khong in output
    -t TIMEOUT | --timeout=TIMEOUT  Thoi gian cho tinh bang giay (mac dinh 15)
    -- COMMAND ARGS             Lenh chay sau khi ket noi san sang
USAGE
    exit 1
}

wait_for() {
    if [[ $WAITFORIT_TIMEOUT -gt 0 ]]; then
        echoerr "$WAITFORIT_cmdname: dang doi $WAITFORIT_HOST:$WAITFORIT_PORT trong ${WAITFORIT_TIMEOUT}s"
    else
        echoerr "$WAITFORIT_cmdname: dang doi $WAITFORIT_HOST:$WAITFORIT_PORT khong gioi han"
    fi
    WAITFORIT_start_ts=$(date +%s)
    while :; do
        if [[ $WAITFORIT_ISBUSY -eq 1 ]]; then
            nc -z $WAITFORIT_HOST $WAITFORIT_PORT
            WAITFORIT_result=$?
        else
            (echo -n > /dev/tcp/$WAITFORIT_HOST/$WAITFORIT_PORT) >/dev/null 2>&1
            WAITFORIT_result=$?
        fi
        if [[ $WAITFORIT_result -eq 0 ]]; then
            WAITFORIT_end_ts=$(date +%s)
            echoerr "$WAITFORIT_cmdname: $WAITFORIT_HOST:$WAITFORIT_PORT san sang sau $((WAITFORIT_end_ts - WAITFORIT_start_ts))s"
            break
        fi
        sleep 1
        WAITFORIT_now_ts=$(date +%s)
        if [[ $WAITFORIT_TIMEOUT -gt 0 ]] && [[ $((WAITFORIT_now_ts - WAITFORIT_start_ts)) -gt $WAITFORIT_TIMEOUT ]]; then
            echoerr "$WAITFORIT_cmdname: het thoi gian cho $WAITFORIT_HOST:$WAITFORIT_PORT"
            break
        fi
    done
    return $WAITFORIT_result
}

wait_for_wrapper() {
    if [[ $WAITFORIT_QUIET -eq 1 ]]; then
        timeout $WAITFORIT_BUSYTIMEFLAG $WAITFORIT_TIMEOUT bash $0 --quiet --child --host=$WAITFORIT_HOST --port=$WAITFORIT_PORT --timeout=$WAITFORIT_TIMEOUT &
    else
        timeout $WAITFORIT_BUSYTIMEFLAG $WAITFORIT_TIMEOUT bash $0 --child --host=$WAITFORIT_HOST --port=$WAITFORIT_PORT --timeout=$WAITFORIT_TIMEOUT &
    fi
    WAITFORIT_PID=$!
    trap "kill -INT -$WAITFORIT_PID" INT
    wait $WAITFORIT_PID
    WAITFORIT_RESULT=$?
    if [[ $WAITFORIT_RESULT -ne 0 ]]; then
        echoerr "$WAITFORIT_cmdname: het thoi gian hoac loi cho $WAITFORIT_HOST:$WAITFORIT_PORT"
    fi
    return $WAITFORIT_RESULT
}

WAITFORIT_TIMEOUT=15
WAITFORIT_STRICT=0
WAITFORIT_CHILD=0
WAITFORIT_QUIET=0
WAITFORIT_BUSYTIMEFLAG=""
WAITFORIT_ISBUSY=0

while [[ $# -gt 0 ]]; do
    case "$1" in
        *:* )
        WAITFORIT_hostport=(${1//:/ })
        WAITFORIT_HOST=${WAITFORIT_hostport[0]}
        WAITFORIT_PORT=${WAITFORIT_hostport[1]}
        shift 1
        ;;
        --child) WAITFORIT_CHILD=1; shift 1 ;;
        -q | --quiet) WAITFORIT_QUIET=1; shift 1 ;;
        -s | --strict) WAITFORIT_STRICT=1; shift 1 ;;
        -h) WAITFORIT_HOST="$2"; shift 2 ;;
        --host=*) WAITFORIT_HOST="${1#*=}"; shift 1 ;;
        -p) WAITFORIT_PORT="$2"; shift 2 ;;
        --port=*) WAITFORIT_PORT="${1#*=}"; shift 1 ;;
        -t) WAITFORIT_TIMEOUT="$2"; shift 2 ;;
        --timeout=*) WAITFORIT_TIMEOUT="${1#*=}"; shift 1 ;;
        --) shift; break ;;
        --help) usage ;;
        *) echoerr "Tham so khong hop le: $1"; usage ;;
    esac
done

if [[ "$WAITFORIT_HOST" == "" || "$WAITFORIT_PORT" == "" ]]; then
    echoerr "Thieu host hoac port"
    usage
fi

WAITFORIT_RESULT=0
if [[ $WAITFORIT_CHILD -gt 0 ]]; then
    wait_for
    WAITFORIT_RESULT=$?
    exit $WAITFORIT_RESULT
else
    if [[ $WAITFORIT_TIMEOUT -gt 0 ]]; then
        wait_for_wrapper
        WAITFORIT_RESULT=$?
    else
        wait_for
        WAITFORIT_RESULT=$?
    fi
fi

if [[ $# -gt 0 ]]; then
    if [[ $WAITFORIT_RESULT -ne 0 && $WAITFORIT_STRICT -eq 1 ]]; then
        echoerr "$WAITFORIT_cmdname: strict mode: khong chay lenh vi $WAITFORIT_HOST:$WAITFORIT_PORT khong san sang"
        exit $WAITFORIT_RESULT
    fi
    exec "$@"
else
    exit $WAITFORIT_RESULT
fi
