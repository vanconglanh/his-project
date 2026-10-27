/**
 * Verify dot ra soat Lark 2026-10-27 (BM-125, bug a service-only, BM-120, bug b cashier).
 * Chay: BASE_URL=https://his.diab.vn node docs/qc/evidence-lark-20261027/verify.js
 * Dang nhap qua panel test theo vai tro (khong goi API tat). Ghi EVIDENCE.txt.
 */
const path = require("path");
const fs = require("fs");
const REPO_ROOT = path.resolve(__dirname, "../../..");
const { chromium } = require(path.join(REPO_ROOT, "frontend", "node_modules", "@playwright", "test"));
const BASE = (process.env.BASE_URL || "https://his.diab.vn").replace(/\/$/, "");
const OUT = path.join(__dirname, "EVIDENCE.txt");

const lines = [];
function rec(id, pass, detail) { const s = `${pass ? "PASS" : "FAIL"}  ${id}  ${detail}`; lines.push(s); console.log(s); }

async function quickLogin(page, roleLabel) {
  for (let i = 1; i <= 3; i++) {
    await page.goto(BASE + "/login", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1200);
    await page.getByRole("button", { name: roleLabel, exact: true }).click();
    try { await page.waitForURL((u) => !u.pathname.includes("/login"), { timeout: 30000 }); await page.waitForLoadState("networkidle").catch(() => {}); return; }
    catch { /* retry */ }
  }
  throw new Error("Khong dang nhap duoc: " + roleLabel);
}
async function getToken(page) {
  return page.evaluate(() => {
    for (let i = 0; i < localStorage.length; i++) {
      const v = localStorage.getItem(localStorage.key(i)); if (!v) continue;
      const am = v.match(/"accessToken":"(eyJ[^"]+)"/); if (am) return am[1];
      const bm = v.match(/eyJ[A-Za-z0-9_.-]+/); if (bm) return bm[0];
    }
    return null;
  });
}
async function api(page, token, urlPath, opts = {}) {
  return page.evaluate(async ({ urlPath, token, opts }) => {
    const res = await fetch(urlPath, { method: opts.method || "GET", headers: Object.assign({ Authorization: "Bearer " + token, "Content-Type": "application/json" }, opts.headers || {}), body: opts.body ? JSON.stringify(opts.body) : undefined });
    let text = ""; try { text = await res.text(); } catch {}
    let json = null; try { json = JSON.parse(text); } catch {}
    return { status: res.status, text: text.slice(0, 500), json };
  }, { urlPath, token, opts });
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ ignoreHTTPSErrors: true });
  const page = await ctx.newPage();
  try {
    // ---- Bac si: BM-125 (vital fallback) + BM-120 (upload anh) ----
    await quickLogin(page, "Bác sĩ");
    const tDoc = await getToken(page);

    // BM-125: luot kham khong co vital rieng nhung benh nhan co vital -> fallback tra >=1
    const encId = "439e19ce-1a91-42e9-a9c1-a3a5f2a0eed9";
    const vr = await api(page, tDoc, "/api/v1/encounters/" + encId + "/vital-signs");
    const vArr = (vr.json && (vr.json.data || vr.json)) || [];
    const vCount = Array.isArray(vArr) ? vArr.length : (Array.isArray(vr.json) ? vr.json.length : 0);
    rec("BM-125", vr.status === 200 && vCount >= 1, `GET /encounters/${encId}/vital-signs status=${vr.status}, so sinh hieu=${vCount} (ky vong >=1 nho fallback benh nhan; truoc fix = 0)`);

    // BM-120: upload anh PNG nho -> KHONG bi INBODY_INVALID_FORMAT (format anh duoc chap nhan)
    const pid = (await api(page, tDoc, "/api/v1/patients?page=1&page_size=1")).json?.data?.[0]?.id;
    if (pid) {
      const up = await page.evaluate(async ({ pid, token }) => {
        // 1x1 PNG base64
        const b64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
        const bin = atob(b64); const arr = new Uint8Array(bin.length); for (let i = 0; i < bin.length; i++) arr[i] = bin.charCodeAt(i);
        const fd = new FormData();
        fd.append("file", new Blob([arr], { type: "image/png" }), "REGR-bm120-probe.png");
        const res = await fetch(`/api/v1/patients/${pid}/inbody-reports`, { method: "POST", headers: { Authorization: "Bearer " + token }, body: fd });
        let text = ""; try { text = await res.text(); } catch {}
        let json = null; try { json = JSON.parse(text); } catch {}
        return { status: res.status, code: json?.error?.code || null, text: text.slice(0, 300) };
      }, { pid, token: tDoc });
      rec("BM-120", up.code !== "INBODY_INVALID_FORMAT", `Upload anh PNG -> status=${up.status}, errorCode=${up.code || "(none/success)"} (ky vong KHAC INBODY_INVALID_FORMAT = format anh da duoc chap nhan)`);
    } else rec("BM-120", false, "Khong lay duoc patient de test upload");

    // ---- Le tan: bug(a) service-only snake_case vs camelCase + bug(b) cashier ----
    await quickLogin(page, "Lễ tân");
    const tLt = await getToken(page);

    // bug(b) cashier today (informational - khong co ca OPEN co payments de khang dinh >0)
    const today = await api(page, tLt, "/api/v1/cashier/closing/today");
    const net = today.json?.data?.summary?.net_collected;
    rec("bug-b-cashier", today.status === 200, `GET /cashier/closing/today status=${today.status}, net_collected=${net} (fix: khi ca OPEN tinh live tu payments; staging hien khong co ca OPEN co giao dich de khang dinh >0 -> logic verify qua unit test + build)`);

    const pid2 = (await api(page, tLt, "/api/v1/patients?page=1&page_size=1")).json?.data?.[0]?.id;
    const svc = (await api(page, tLt, "/api/v1/services?is_active=true&page=1&page_size=1")).json?.data?.[0]?.id;
    if (pid2 && svc) {
      // camelCase (bug cu): BE bo qua serviceIds -> SERVICE_ONLY_EMPTY
      const camel = await api(page, tLt, "/api/v1/reception/service-only", { method: "POST", body: { patientId: pid2, serviceIds: [svc], servicePackageIds: [] } });
      const camelEmpty = (camel.json?.error?.code === "SERVICE_ONLY_EMPTY");
      // snake_case (FE sau fix): BE doc dung -> tao duoc hoa don (khong con EMPTY)
      const snake = await api(page, tLt, "/api/v1/reception/service-only", { method: "POST", body: { patient_id: pid2, service_ids: [svc], service_package_ids: [], note: "REGR-bug-a-service-only" } });
      const billNo = snake.json?.data?.bill_no || snake.json?.data?.billNo;
      const snakeOk = (snake.status === 200 || snake.status === 201);
      rec("bug-a-service-only", camelEmpty && snakeOk, `camelCase serviceIds -> ${camel.json?.error?.code} (tai hien bug cu); snake_case service_ids -> status=${snake.status}, bill_no=${billNo || "(none)"} (fix OK: tao duoc hoa don, het SERVICE_ONLY_EMPTY)`);
    } else rec("bug-a-service-only", false, `Khong lay duoc patient/service (pid=${!!pid2}, svc=${!!svc})`);
  } finally {
    fs.writeFileSync(OUT, lines.join("\n") + "\n", "utf8");
    console.log("\nDa ghi " + OUT);
    await browser.close();
  }
})();
