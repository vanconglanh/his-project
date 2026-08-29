// Thu vien dung chung cho bo test full-flow qua UI that (Playwright).
// - quickLogin: dung panel "Dang nhap nhanh theo vai tro" tren /login (khong goi API tat)
// - shot: chup screenshot + khoanh vung INPUT (xanh duong) / ACTION (vang) / RESULT (xanh la)
const { chromium } = require("@playwright/test");
const path = require("path");
const fs = require("fs");

const BASE = process.env.BASE_URL || "http://localhost:3000";
const EVID = process.env.EVID_DIR ||
  path.resolve(__dirname, "../../docs/qc/evidence-fullflow-20260829");

fs.mkdirSync(EVID, { recursive: true });

const OVERLAY_CSS = `
#qc-ovl{position:fixed;inset:0;z-index:2147483647;pointer-events:none}
#qc-ovl .qb{position:absolute;box-sizing:border-box;border-width:3px;border-style:solid;border-radius:4px}
#qc-ovl .qt{position:absolute;font:bold 12px/1.4 system-ui,sans-serif;color:#fff;padding:2px 7px;border-radius:3px;white-space:nowrap}
`;

async function annotate(page, marks) {
  // marks: [{sel|box, kind:'input'|'action'|'result', label}]
  await page.evaluate(
    ({ marks, css }) => {
      document.getElementById("qc-ovl")?.remove();
      const st = document.getElementById("qc-ovl-css") || document.createElement("style");
      st.id = "qc-ovl-css";
      st.textContent = css;
      document.head.appendChild(st);
      const ovl = document.createElement("div");
      ovl.id = "qc-ovl";
      const COLORS = { input: "#2563eb", action: "#d97706", result: "#16a34a" };
      const PREFIX = { input: "INPUT", action: "ACTION", result: "RESULT" };
      marks.forEach((m) => {
        let r = m.box;
        if (!r && m.sel) {
          let el = null;
          if (m.sel.startsWith("btn:")) {
            const want = m.sel.slice(4);
            el = [...document.querySelectorAll("button,a")].reverse()
              .find((b) => (b.innerText || "").trim() === want && b.getClientRects().length);
          } else if (m.sel.startsWith("txt:")) {
            const want = m.sel.slice(4);
            el = [...document.querySelectorAll("body *")]
              .find((b) => (b.innerText || "").trim().includes(want) && b.children.length === 0 && b.getClientRects().length);
          } else {
            el = document.querySelector(m.sel);
          }
          if (!el) return;
          const b = el.getBoundingClientRect();
          r = { x: b.x, y: b.y, width: b.width, height: b.height };
        }
        if (!r || r.width === 0) return;
        const c = COLORS[m.kind] || "#e11d48";
        const pad = 4;
        const box = document.createElement("div");
        box.className = "qb";
        box.style.cssText += `left:${r.x - pad}px;top:${r.y - pad}px;width:${r.width + pad * 2}px;height:${r.height + pad * 2}px;border-color:${c};box-shadow:0 0 0 2px rgba(255,255,255,.85)`;
        ovl.appendChild(box);
        const tag = document.createElement("div");
        tag.className = "qt";
        tag.textContent = `${PREFIX[m.kind] || "NOTE"}${m.label ? ": " + m.label : ""}`;
        tag.style.cssText += `background:${c};left:${Math.max(2, r.x - pad)}px;top:${Math.max(2, r.y - pad - 20)}px`;
        ovl.appendChild(tag);
      });
      document.body.appendChild(ovl);
    },
    { marks, css: OVERLAY_CSS }
  );
}

async function clearAnnotate(page) {
  await page.evaluate(() => document.getElementById("qc-ovl")?.remove());
}

async function shot(page, name, marks = []) {
  if (marks.length) await annotate(page, marks);
  const file = path.join(EVID, name + ".png");
  await page.screenshot({ path: file });
  await clearAnnotate(page);
  console.log("  SHOT " + name + ".png");
  return file;
}

async function quickLogin(page, roleLabel) {
  for (let i = 1; i <= 3; i++) {
    await page.goto(BASE + "/login", { waitUntil: "domcontentloaded" });
    await page.waitForLoadState("networkidle").catch(() => {});
    await page.waitForTimeout(1200); // cho React hydrate xong moi bam
    await page.getByRole("button", { name: roleLabel, exact: true }).click();
    try {
      await page.waitForURL((u) => !u.pathname.includes("/login"), { timeout: 30000 });
      await page.waitForLoadState("networkidle").catch(() => {});
      return;
    } catch {
      console.log("  [retry login " + i + "] van o trang login");
    }
  }
  throw new Error("Khong dang nhap duoc sau 3 lan thu voi role " + roleLabel);
}

async function newSession() {
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 950 }, locale: "vi-VN" });
  const page = await ctx.newPage();
  const net = [];
  const consoleErrs = [];
  page.on("console", (m) => m.type() === "error" && consoleErrs.push(m.text().slice(0, 240)));
  page.on("response", async (r) => {
    const u = r.url();
    if (!u.includes("/api/")) return;
    const rec = { status: r.status(), method: r.request().method(), url: u.replace("http://localhost:5000", "") };
    if (r.status() >= 400) {
      try { rec.body = (await r.text()).slice(0, 400); } catch {}
    }
    net.push(rec);
  });
  return { browser, ctx, page, net, consoleErrs };
}

function saveJson(name, data) {
  fs.writeFileSync(path.join(EVID, name + ".json"), JSON.stringify(data, null, 1), "utf8");
}

module.exports = { BASE, EVID, shot, annotate, clearAnnotate, quickLogin, newSession, saveJson };
