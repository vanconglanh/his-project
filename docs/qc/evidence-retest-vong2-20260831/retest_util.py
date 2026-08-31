# -*- coding: utf-8 -*-
"""Tien ich chung cho retest vong 2: ghi ket qua case, truy van DB, doc trang FE."""
import os, json, subprocess, urllib.request, urllib.error

D = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(D, "retest2-results.jsonl")
R = []


def rec(cid, status, note):
    R.append({"id": cid, "status": status, "note": note})
    icon = {"PASS": "OK  ", "FAIL": "FAIL", "SKIP": "SKIP"}[status]
    print(f"  [{icon}] {cid}: {note}")


def dump(part="part1"):
    """Ghi ket qua cua 1 phan ra file jsonl rieng."""
    with open(os.path.join(D, f"retest2-{part}.jsonl"), "w", encoding="utf-8") as f:
        for r in R:
            f.write(json.dumps(r, ensure_ascii=False) + "\n")
    print(f">>> da ghi {len(R)} case vao {OUT}")


def sql(q):
    p = subprocess.run(["docker", "exec", "prodiab-mysql", "sh", "-c",
                        'mysql -uroot -p"$MYSQL_ROOT_PASSWORD" --default-character-set=utf8mb4 '
                        '--skip-pager -N -e ' + json.dumps(q)],
                       capture_output=True, text=True, encoding="utf-8", errors="replace")
    return [l for l in (p.stdout or "").splitlines()
            if l and not l.startswith("mysql:") and not l.startswith("PAGER")]


def err(d):
    return (d or {}).get("error", {}).get("code") if isinstance(d, dict) else None


def lst(d):
    raw = (d or {}).get("data") if isinstance(d, dict) else d
    if isinstance(raw, dict):
        return raw.get("items") or raw.get("results") or []
    return raw or []


def page(url):
    try:
        with urllib.request.urlopen(url, timeout=30) as r:
            return r.status, r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, ""
    except Exception as ex:
        return 0, str(ex)


def hd(t):
    print("\n" + "=" * 78 + f"\n{t}\n" + "=" * 78)
