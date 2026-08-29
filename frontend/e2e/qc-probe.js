// QC probe: dang nhap theo role roi dump cau truc form/UI cua 1 route.
// Chay: node e2e/qc-probe.js <roleLabel> <path> [waitMs]
const { chromium } = require("@playwright/test");

const BASE = process.env.BASE_URL || "http://localhost:3000";

async function quickLogin(page, roleLabel) {
  await page.goto(BASE + "/login", { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: roleLabel, exact: true }).click();
  await page.waitForURL((u) => !u.pathname.includes("/login"), { timeout: 30000 });
  await page.waitForLoadState("networkidle").catch(() => {});
}

async function dump(page) {
  const info = await page.evaluate(() => {
    const out = { url: location.href, controls: [], buttons: [], headings: [], tables: [] };
    const lbl = (el) => {
      const id = el.id;
      let t = "";
      if (id) {
        const l = document.querySelector(`label[for="${CSS.escape(id)}"]`);
        if (l) t = l.innerText.trim();
      }
      if (!t) {
        const l = el.closest("label");
        if (l) t = l.innerText.trim();
      }
      return t;
    };
    document.querySelectorAll("input,select,textarea,[contenteditable=true],[role=combobox]").forEach((el) => {
      out.controls.push({
        tag: el.tagName.toLowerCase(),
        type: el.getAttribute("type") || el.getAttribute("role") || "",
        name: el.getAttribute("name") || "",
        id: el.id || "",
        placeholder: el.getAttribute("placeholder") || "",
        label: lbl(el),
        value: (el.value || "").slice(0, 40),
        required: el.required || el.getAttribute("aria-required") === "true",
        visible: !!(el.offsetParent || el.getClientRects().length),
      });
    });
    document.querySelectorAll("button,[role=button],a[href]").forEach((el) => {
      const t = (el.innerText || "").trim().replace(/\s+/g, " ");
      if (t) out.buttons.push({ t: t.slice(0, 60), href: el.getAttribute("href") || "", dis: el.disabled === true });
    });
    document.querySelectorAll("h1,h2,h3").forEach((el) => out.headings.push((el.innerText || "").trim().slice(0, 80)));
    document.querySelectorAll("table").forEach((tb) => {
      const head = [...tb.querySelectorAll("thead th")].map((th) => th.innerText.trim());
      const rows = [...tb.querySelectorAll("tbody tr")].slice(0, 5).map((tr) =>
        [...tr.querySelectorAll("td")].map((td) => td.innerText.trim().replace(/\s+/g, " ").slice(0, 30))
      );
      out.tables.push({ head, rows, total: tb.querySelectorAll("tbody tr").length });
    });
    return out;
  });
  return info;
}

(async () => {
  const [role, path_, waitMs] = process.argv.slice(2);
  const browser = await chromium.launch();
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 }, locale: "vi-VN" });
  const page = await ctx.newPage();
  const errs = [];
  page.on("console", (m) => m.type() === "error" && errs.push(m.text().slice(0, 200)));
  page.on("response", (r) => {
    if (r.status() >= 400 && r.url().includes("/api/")) errs.push(`HTTP ${r.status()} ${r.url()}`);
  });
  try {
    await quickLogin(page, role);
    if (path_) {
      await page.goto(BASE + path_, { waitUntil: "domcontentloaded" });
      await page.waitForLoadState("networkidle").catch(() => {});
    }
    await page.waitForTimeout(Number(waitMs || 1500));
    const info = await dump(page);
    console.log(JSON.stringify({ ...info, errs }, null, 1));
  } catch (e) {
    console.log("PROBE_ERROR: " + e.message);
  }
  await browser.close();
})();
