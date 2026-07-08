/**
 * gen-evidence-report.mjs — Dựng report HTML tổng hợp từ evidence-10-report.json (+ báo cáo sweep
 * nút in nếu có), nhúng ảnh dạng thumbnail click phóng to, tô màu PASS/FAIL. Đồng thời verify sơ bộ
 * mọi ảnh PNG (tồn tại + kích thước > ngưỡng) để phát hiện ảnh trắng/hỏng.
 *
 * Chạy: node e2e/sim/gen-evidence-report.mjs
 * Output: docs/test/evidence-shots/report.html
 */
import fs from "fs";
import path from "path";
import { fileURLToPath } from "url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const SHOTS_DIR = path.resolve(__dirname, "..", "..", "..", "docs", "test", "evidence-shots");
const FLOW_JSON = path.join(SHOTS_DIR, "evidence-10-report.json");
const PRINT_JSON = path.join(SHOTS_DIR, "print-sweep-report.json");
const OUT = path.join(SHOTS_DIR, "report.html");

const MIN_PNG_BYTES = 3 * 1024; // < 3KB => nghi ảnh trắng/hỏng

function loadJson(p) {
  try { return JSON.parse(fs.readFileSync(p, "utf-8")); } catch { return null; }
}

function verifyImg(relFile) {
  const abs = path.join(SHOTS_DIR, relFile);
  if (!fs.existsSync(abs)) return { ok: false, bytes: 0, reason: "missing" };
  const bytes = fs.statSync(abs).size;
  if (bytes < MIN_PNG_BYTES) return { ok: false, bytes, reason: "too-small" };
  return { ok: true, bytes };
}

const flow = loadJson(FLOW_JSON);
const print = loadJson(PRINT_JSON);

const esc = (s) => String(s ?? "").replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));

let imgTotal = 0, imgBad = 0;
const badList = [];

function stepCard(r) {
  const v = verifyImg(r.file);
  imgTotal++;
  if (!v.ok) { imgBad++; badList.push({ file: r.file, ...v }); }
  const badge = r.ok ? '<span class="b ok">OK</span>' : '<span class="b fail">FAIL</span>';
  const imgWarn = v.ok ? "" : `<div class="imgwarn">⚠ ảnh ${v.reason} (${v.bytes}B)</div>`;
  return `<figure class="shot">
    <a href="${esc(r.file)}" target="_blank"><img loading="lazy" src="${esc(r.file)}" alt="${esc(r.label)}"></a>
    <figcaption>${badge} <b>#${r.step}</b> ${esc(r.label)}${imgWarn}
      ${r.error ? `<div class="err">${esc(r.error)}</div>` : ""}</figcaption>
  </figure>`;
}

let flowHtml = "<p>(không có dữ liệu flow)</p>";
if (flow) {
  const byPatient = new Map();
  for (const r of flow.results) {
    if (!byPatient.has(r.index)) byPatient.set(r.index, []);
    byPatient.get(r.index).push(r);
  }
  const sections = [];
  for (const p of flow.patients) {
    const steps = (byPatient.get(p.index) || []).sort((a, b) => a.step - b.step);
    const pass = steps.filter((s) => s.ok).length;
    sections.push(`<section class="patient">
      <h3>BN ${p.index}: ${esc(p.fullName)} <small>${esc(p.icd10)} · ${esc(p.room)} · ${pass}/${steps.length} bước OK</small></h3>
      <div class="grid">${steps.map(stepCard).join("")}</div>
    </section>`);
  }
  flowHtml = sections.join("\n");
}

let printHtml = "";
if (print) {
  const rows = (print.results || []).map((r) => {
    const shots = (r.screenshots || r.files || []).map((f) => {
      const v = verifyImg(f); imgTotal++; if (!v.ok) { imgBad++; badList.push({ file: f, ...v }); }
      return `<figure class="shot"><a href="${esc(f)}" target="_blank"><img loading="lazy" src="${esc(f)}"></a>
        <figcaption>${v.ok ? "" : `<span class="imgwarn">⚠ ${v.reason}</span>`}</figcaption></figure>`;
    }).join("");
    const badge = r.status === "PASS" ? '<span class="b ok">PASS</span>' : r.status === "SKIP" ? '<span class="b skip">SKIP</span>' : '<span class="b fail">FAIL</span>';
    return `<section class="patient"><h3>${badge} ${esc(r.name || r.step)} <small>${esc(r.note || "")}</small></h3>
      ${r.error ? `<div class="err">${esc(r.error)}</div>` : ""}
      <div class="grid">${shots}</div></section>`;
  }).join("\n");
  printHtml = `<h2>Phần B — Sweep màn hình có nút in / xuất file</h2>
    <p class="meta">Tổng ${print.total ?? "?"} · PASS ${print.pass ?? "?"} · FAIL ${print.fail ?? "?"} · SKIP ${print.skip ?? "?"}</p>${rows}`;
}

const html = `<!doctype html><html lang="vi"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Evidence — Flow khám 10 BN + Sweep nút in — Pro-Diab HIS</title>
<style>
  :root{--bg:#f6f8f9;--card:#fff;--ink:#0f172a;--mut:#64748b;--ok:#047857;--fail:#b91c1c;--skip:#b45309;--teal:#01645A}
  *{box-sizing:border-box}body{margin:0;font:14px/1.5 system-ui,Segoe UI,Roboto,sans-serif;background:var(--bg);color:var(--ink)}
  header{background:var(--teal);color:#fff;padding:20px 24px}header h1{margin:0 0 4px;font-size:20px}header .meta{color:#bfe0db}
  main{max-width:1200px;margin:0 auto;padding:20px}
  h2{margin:28px 0 10px;font-size:17px;border-bottom:2px solid #e2e8f0;padding-bottom:6px}
  .patient{background:var(--card);border:1px solid #e2e8f0;border-radius:10px;padding:14px 16px;margin:14px 0}
  .patient h3{margin:0 0 10px;font-size:15px}.patient h3 small{color:var(--mut);font-weight:400}
  .grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(210px,1fr));gap:12px}
  .shot{margin:0}.shot img{width:100%;height:132px;object-fit:cover;object-position:top;border:1px solid #e2e8f0;border-radius:6px;background:#fff}
  figcaption{font-size:12px;color:var(--mut);margin-top:4px}
  .b{display:inline-block;font-size:11px;font-weight:700;padding:1px 6px;border-radius:4px;color:#fff}
  .b.ok{background:var(--ok)}.b.fail{background:var(--fail)}.b.skip{background:var(--skip)}
  .err{color:var(--fail);font-size:11px;margin-top:2px;word-break:break-word}
  .imgwarn{color:var(--fail);font-weight:600}
  .summary{display:flex;gap:16px;flex-wrap:wrap;margin:8px 0}
  .kpi{background:#fff;border:1px solid #e2e8f0;border-radius:8px;padding:10px 16px;min-width:120px}
  .kpi b{display:block;font-size:22px}.kpi span{color:var(--mut);font-size:12px}
</style></head><body>
<header><h1>Evidence — Flow khám 10 bệnh nhân + Sweep nút in</h1>
<div class="meta">Pro-Diab HIS · ${esc(flow?.baseUrl || "")} · tạo lúc ${esc(flow?.generatedAt || "")}</div></header>
<main>
  <div class="summary">
    <div class="kpi"><b>${flow?.patientCount ?? "-"}</b><span>bệnh nhân</span></div>
    <div class="kpi"><b>${flow?.passed ?? "-"}/${flow?.totalSteps ?? "-"}</b><span>bước flow OK</span></div>
    <div class="kpi"><b>${imgTotal}</b><span>ảnh evidence</span></div>
    <div class="kpi"><b style="color:${imgBad ? "var(--fail)" : "var(--ok)"}">${imgBad}</b><span>ảnh nghi hỏng</span></div>
  </div>
  <h2>Phần A — Luồng khám đầy đủ (10 BN, mỗi bước 1 ảnh)</h2>
  ${flowHtml}
  ${printHtml}
</main></body></html>`;

fs.writeFileSync(OUT, html, "utf-8");
console.log(`[report] Da tao: ${OUT}`);
console.log(`[report] Anh: ${imgTotal} tong, ${imgBad} nghi hong.`);
if (badList.length) console.log("[report] Danh sach anh nghi hong:\n" + badList.map((b) => ` - ${b.file} (${b.reason}, ${b.bytes}B)`).join("\n"));
