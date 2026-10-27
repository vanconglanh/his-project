/**
 * Bo test HOI QUY (regression) cho Pro-Diab HIS tren VNG.
 * Muc dich: dam bao cac bug da fix KHONG MO LAI (BM-04,07,33,36,37,40,47,48,49,50,51,52,
 *           56,59,60,61,63,64,65,67,70,71,74,81,82,86).
 *
 * Cach chay (tu thu muc goc repo hoac bat ky dau):
 *   BASE_URL=https://his.diab.vn node docs/qc/regression/regression.js
 *   (mac dinh BASE_URL = https://his.diab.vn)
 *
 * Nguyen tac:
 *  - Dang nhap bang panel "Dang nhap nhanh theo vai tro" (khong goi API tat).
 *  - Da so assertion goi API that bang access token lay tu localStorage (robust, it rac).
 *  - KHONG tao du lieu rac lon; neu phai tao thi dung tien to "REGR-".
 *  - Ket qua ghi ra docs/qc/regression/RESULT.txt. Test DO (FAIL) = co the hoi quy.
 *
 * Khong dung @playwright/test runner (de khong anh huong lint/build frontend);
 * dung truc tiep chromium tu frontend/node_modules.
 */
const path = require("path");
const fs = require("fs");

const REPO_ROOT = path.resolve(__dirname, "../../..");
const { chromium } = require(path.join(REPO_ROOT, "frontend", "node_modules", "@playwright", "test"));

const BASE = (process.env.BASE_URL || "https://his.diab.vn").replace(/\/$/, "");
const OUT = path.join(__dirname, "RESULT.txt");

const results = [];
function rec(id, pass, detail) {
  results.push({ id, pass, detail });
  console.log(`${pass ? "PASS" : "FAIL"}  ${id}  ${detail}`);
}

async function quickLogin(page, roleLabel) {
  for (let i = 1; i <= 3; i++) {
    await page.goto(BASE + "/login", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1200);
    await page.getByRole("button", { name: roleLabel, exact: true }).click();
    try {
      await page.waitForURL((u) => !u.pathname.includes("/login"), { timeout: 30000 });
      await page.waitForLoadState("networkidle").catch(() => {});
      return;
    } catch { /* retry */ }
  }
  throw new Error("Khong dang nhap duoc voi role " + roleLabel);
}

// Lay access/refresh token tu localStorage (zustand persist hoac tuong tu)
async function getTokens(page) {
  return page.evaluate(() => {
    let access = null, refresh = null;
    for (let i = 0; i < localStorage.length; i++) {
      const v = localStorage.getItem(localStorage.key(i));
      if (!v) continue;
      try {
        const o = JSON.parse(v);
        const s = JSON.stringify(o);
        const am = s.match(/"accessToken":"(eyJ[^"]+)"/); if (am) access = am[1];
        const rm = s.match(/"refreshToken":"([^"]+)"/); if (rm) refresh = rm[1];
      } catch {
        const am = v.match(/eyJ[A-Za-z0-9_.-]+/); if (am && !access) access = am[0];
      }
    }
    return { access, refresh };
  });
}

// Goi API trong context trang (co cookie + header) voi Bearer token
async function api(page, token, urlPath, opts = {}) {
  return page.evaluate(async ({ urlPath, token, opts }) => {
    const res = await fetch(urlPath, {
      method: opts.method || "GET",
      headers: Object.assign({ Authorization: "Bearer " + token, "Content-Type": "application/json" }, opts.headers || {}),
      body: opts.body ? JSON.stringify(opts.body) : undefined,
    });
    let text = ""; try { text = await res.text(); } catch {}
    let json = null; try { json = JSON.parse(text); } catch {}
    return { status: res.status, text: text.slice(0, 2000), json };
  }, { urlPath, token, opts });
}

function jwtPayload(token) {
  try { return JSON.parse(Buffer.from(token.split(".")[1], "base64").toString("utf8")); }
  catch { return {}; }
}
function perms(token) {
  const p = jwtPayload(token);
  const raw = p.permissions || p.perms || p.scope || p.scopes || [];
  return Array.isArray(raw) ? raw : String(raw).split(/[ ,]+/);
}

(async () => {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 950 }, locale: "vi-VN" });
  const page = await ctx.newPage();

  try {
    // ====== VAI LE TAN ======
    await quickLogin(page, "Lễ tân");
    let t = (await getTokens(page)).access;

    // BM-49: timestamp API co hau to Z (UTC) -> FE doi sang gio VN
    {
      const r = await api(page, t, "/api/v1/encounters?page=1&page_size=1");
      const hasZ = /"(started_at|created_at)":"[0-9-]+T[0-9:.]+Z"/.test(r.text);
      rec("BM-49", r.status === 200 && hasZ, `GET /encounters status=${r.status}, co Z suffix=${hasZ}`);
    }

    // BM-37 + BM-63: Le tan KHONG co quyen billing.finalize/payment.collect/billing.void
    {
      const pr = perms(t);
      const noFinalize = !pr.includes("billing.finalize");
      const noVoid = !pr.includes("billing.void");
      rec("BM-37/63", noFinalize && noVoid,
        `JWT Le tan perms: billing.finalize=${pr.includes("billing.finalize")}, billing.void=${pr.includes("billing.void")} (ky vong: deu khong co)`);
    }

    // BM-61: Le tan GET /encounters/{id} -> du lieu lam sang bi strip (diagnoses rong)
    {
      const list = await api(page, t, "/api/v1/encounters?page=1&page_size=5");
      const items = (list.json && (list.json.data || list.json.items || list.json)) || [];
      const first = Array.isArray(items) ? items.find((x) => x && (x.id)) : null;
      if (first) {
        const d = await api(page, t, "/api/v1/encounters/" + first.id);
        const body = d.json && (d.json.data || d.json) || {};
        const diags = body.diagnoses;
        const stripped = Array.isArray(diags) ? diags.length === 0 : (diags == null);
        rec("BM-61", d.status === 200 && stripped,
          `Le tan GET /encounters/${first.id}: diagnoses=${JSON.stringify(diags)} (ky vong rong/null)`);
      } else {
        rec("BM-61", false, "Khong tim duoc encounter de kiem");
      }
    }

    // BM-04: tim benh nhan theo SO DIEN THOAI THUC -> ra dung benh nhan do
    // (siet: khong dung tu khoa "test" chung chung; lay 1 benh nhan that roi tim lai theo SDT cua chinh ho)
    {
      const list = await api(page, t, "/api/v1/patients?page=1&page_size=20");
      const items = (list.json && (list.json.data || list.json.items)) || [];
      const arr = Array.isArray(items) ? items : [];
      // tim benh nhan co so dien thoai (phone/phone_number/mobile)
      const withPhone = arr.find((p) => p && (p.phone || p.phone_number || p.mobile));
      const phone = withPhone && (withPhone.phone || withPhone.phone_number || withPhone.mobile);
      if (phone) {
        const r = await api(page, t, "/api/v1/patients?page=1&page_size=10&search=" + encodeURIComponent(phone));
        const found = (r.json && (r.json.data || r.json.items)) || [];
        const foundArr = Array.isArray(found) ? found : [];
        const hit = foundArr.some((p) => p && p.id === withPhone.id);
        rec("BM-04", r.status === 200 && hit,
          `GET /patients?search=<SDT ${phone}> status=${r.status}, tim thay dung BN id=${withPhone.id}=${hit} (so ket qua=${foundArr.length})`);
      } else {
        rec("BM-04", false, `Khong co benh nhan nao co SDT trong ${arr.length} ban ghi dau -> khong kiem duoc tim theo SDT`);
      }
    }

    // BM-81: tao benh nhan thieu ngay sinh -> bi chan (4xx), khong tao record rac
    {
      const r = await api(page, t, "/api/v1/patients", { method: "POST", body: { full_name: "REGR-NoDOB " + Date.now() } });
      rec("BM-81", r.status >= 400 && r.status < 500,
        `POST /patients (thieu ngay sinh) status=${r.status} (ky vong 4xx chan)`);
    }

    // BM-86: refresh token bang field snake_case -> 200 (khong bi dang xuat)
    {
      const tok = await getTokens(page);
      if (tok.refresh) {
        const r = await api(page, t, "/api/v1/auth/refresh", { method: "POST", body: { refresh_token: tok.refresh } });
        rec("BM-86", r.status === 200, `POST /auth/refresh {refresh_token} status=${r.status} (ky vong 200)`);
      } else {
        rec("BM-86", false, "Khong lay duoc refresh token tu localStorage");
      }
    }

    // ====== VAI KY THUAT VIEN (KTV) ======
    await quickLogin(page, "Kỹ thuật viên");
    t = (await getTokens(page)).access;

    // BM-51: KTV goi API so lieu doanh thu bao cao -> 403 (khong tra so gia)
    {
      const r = await api(page, t, "/api/v1/reports/revenue-daily/data?from=2026-10-01&to=2026-10-31&page=1&page_size=10");
      rec("BM-51", r.status === 403, `KTV GET /reports/revenue-daily/data status=${r.status} (ky vong 403, khong tra so mau)`);
    }

    // BM-56: KTV Tong quan KHONG nhan doanh thu; charts doanh thu -> 403
    {
      const trend = await api(page, t, "/api/v1/dashboard/charts/revenue-trend?days=30");
      const top = await api(page, t, "/api/v1/dashboard/charts/top-doctors?days=30");
      rec("BM-56", trend.status === 403 && top.status === 403,
        `KTV charts revenue-trend=${trend.status}, top-doctors=${top.status} (ky vong 403/403)`);
    }

    // ====== VAI KE TOAN (co report.read) ======
    await quickLogin(page, "Kế toán");
    t = (await getTokens(page)).access;

    // BM-71: bao cao tra cot snake_case (khong camelCase) + Ke toan xem duoc (200)
    // (siet: TRUOC day doc key cua rows[0] -> khi rows rong (sample=[]) thi hasCamel=false -> PASS GIA.
    //  Nay kiem tren data.columns[].key (API LUON tra metadata cot ke ca khi 0 dong), BAT BUOC co >=1 cot,
    //  va khong co key camelCase. Neu co rows thi kiem them key cua dong. -> dung du lieu doanh thu rong/khong.)
    {
      const r = await api(page, t, "/api/v1/reports/revenue-daily/data?from=2026-01-01&to=2026-12-31&page=1&page_size=10");
      let colsOk = false;
      let note = `status=${r.status}`;
      if (r.status === 200 && r.json && r.json.data) {
        const cols = Array.isArray(r.json.data.columns) ? r.json.data.columns : [];
        const colKeys = cols.map((c) => c && c.key).filter(Boolean);
        const colsCamel = colKeys.some((k) => /[a-z][A-Z]/.test(k));
        const rows = Array.isArray(r.json.data.rows) ? r.json.data.rows : [];
        const rowKeys = rows[0] ? Object.keys(rows[0]) : [];
        const rowsCamel = rowKeys.some((k) => /[a-z][A-Z]/.test(k));
        // PHAI co metadata cot (>=1) va KHONG camelCase (ca o cot lan o dong neu co dong)
        colsOk = colKeys.length > 0 && !colsCamel && !rowsCamel;
        note += `, so cot=${colKeys.length}, cot mau=${colKeys.slice(0, 6).join(",")}, camelCase(cot)=${colsCamel}, so dong=${rows.length}, camelCase(dong)=${rowsCamel}`;
        if (colKeys.length === 0) note += " (FAIL: khong co metadata cot)";
      }
      rec("BM-71", colsOk, `Ke toan report columns ${note}`);
    }

    // ====== VAI BAC SI ======
    await quickLogin(page, "Bác sĩ");
    t = (await getTokens(page)).access;

    // BM-70 + BM-64: template he thong DIABETES co min/max (chan gia tri vo ly) + co co required(*)
    {
      const r = await api(page, t, "/api/v1/emr-templates");
      const list = (r.json && (r.json.data || r.json.items || r.json)) || [];
      const arr = Array.isArray(list) ? list : [];
      // tim template dai thao duong he thong co structured_json chua field hba1c
      let schema = null;
      for (const tpl of arr) {
        const sj = tpl.structured_json || tpl.structuredJson;
        const parsed = typeof sj === "string" ? safeJson(sj) : sj;
        if (Array.isArray(parsed) && parsed.some((f) => f.key === "hba1c")) { schema = parsed; break; }
      }
      if (schema) {
        const hba1c = schema.find((f) => f.key === "hba1c");
        const hasRange = hba1c && (hba1c.min !== undefined || hba1c.max !== undefined);
        rec("BM-70", !!hasRange, `Template DIABETES: hba1c min=${hba1c && hba1c.min} max=${hba1c && hba1c.max} (ky vong co min/max)`);
        const lydo = schema.find((f) => f.key === "ly_do_kham");
        const reqOk = lydo && lydo.required === true && schema.some((f) => f.required !== true);
        rec("BM-64", !!reqOk, `Template DIABETES: ly_do_kham required=${lydo && lydo.required}, co o khong-required=${schema.some((f) => f.required !== true)}`);
      } else {
        rec("BM-70", false, "Khong tim thay template DIABETES co field hba1c");
        rec("BM-64", false, "Khong tim thay template DIABETES");
      }
    }

    // BM-47 + BM-65: luu EMR (draft) tra 200, khong 500 / khong bao that bai gia
    {
      const me = jwtPayload(t);
      const myId = me.sub || me.user_id || me.nameid || me.uid || null;
      const list = await api(page, t, "/api/v1/encounters?page=1&page_size=50");
      const items = (list.json && (list.json.data || list.json.items)) || [];
      // Chon luot kham CON SUA DUOC: IN_PROGRESS + thuoc CHINH bac si dang nhap (BM-57 chan sua ca
      // bac si khac -> FORBIDDEN; luot DONE/da ky so -> 400 EMR_ALREADY_SIGNED). Thu lan luot cac
      // ung vien, PASS ngay khi 1 luot save tra 200; chi FAIL that su khi gap 500 hoac het ung vien.
      const cands = (Array.isArray(items) ? items : [])
        .filter((x) => x && x.id && x.status === "IN_PROGRESS" && (!myId || x.doctor_id === myId));
      const pool = cands.length ? cands : (Array.isArray(items) ? items.filter((x) => x && x.id) : []);
      let decided = false, lastStatus = null, lastId = null;
      for (const enc of pool.slice(0, 10)) {
        const r = await api(page, t, "/api/v1/encounters/" + enc.id + "/emr", {
          method: "PUT",
          body: { content_json: { type: "doc", content: [] }, structured_values: { ly_do_kham: "REGR check", chan_doan_bien_chung: "REGR" } },
        });
        lastStatus = r.status; lastId = enc.id;
        if (r.status === 500) { rec("BM-47/65", false, `Bac si PUT /encounters/${enc.id}/emr status=500 (loi server)`); decided = true; break; }
        if (r.status === 200) { rec("BM-47/65", true, `Bac si PUT /encounters/${enc.id}/emr status=200 (ky vong 200, khong 500)`); decided = true; break; }
      }
      if (!decided) rec("BM-47/65", false, pool.length ? `Khong luot kham IN_PROGRESS nao save duoc (status cuoi=${lastStatus}, enc=${lastId})` : "Khong tim duoc encounter de luu EMR");
    }

    // ====== Ghi ket qua ======
    const passN = results.filter((r) => r.pass).length;
    const failN = results.length - passN;
    const lines = [];
    lines.push("=== BO TEST HOI QUY Pro-Diab HIS ===");
    lines.push("BASE_URL: " + BASE);
    lines.push("Thoi diem: " + new Date().toISOString());
    lines.push(`Tong: ${results.length} | PASS: ${passN} | FAIL(do=co the hoi quy): ${failN}`);
    lines.push("");
    for (const r of results) lines.push(`[${r.pass ? "PASS" : "FAIL"}] ${r.id} - ${r.detail}`);
    lines.push("");
    lines.push("Ghi chu: cac ticket UI thuan (BM-07,33,36,40,48,50,59,60,67,74,82) kiem thu cong/khac;");
    lines.push("bo nay tap trung assertion API/JWT (robust, it rac) cho hanh vi da fix.");
    fs.writeFileSync(OUT, lines.join("\n"), "utf8");
    console.log("\nDa ghi " + OUT + ` (PASS=${passN}, FAIL=${failN})`);
    process.exitCode = failN > 0 ? 1 : 0;
  } catch (e) {
    console.error("LOI CHAY BO TEST:", e && e.message);
    fs.writeFileSync(OUT, "LOI CHAY BO TEST: " + (e && e.stack || e) + "\nKet qua tung phan:\n" +
      results.map((r) => `[${r.pass ? "PASS" : "FAIL"}] ${r.id} - ${r.detail}`).join("\n"), "utf8");
    process.exitCode = 2;
  } finally {
    await browser.close();
  }
})();

function safeJson(s) { try { return JSON.parse(s); } catch { return null; } }
