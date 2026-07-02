import { test, expect, type Page, type ConsoleMessage, type Request, type Response } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const ADMIN_EMAIL = "admin@prodiab.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "Admin@123";

interface ErrItem {
  url: string;
  type: "console" | "pageerror" | "response";
  message: string;
}
interface WalkResult {
  path: string;
  label?: string;
  ok: boolean;
  errorsAdded: ErrItem[];
  screenshot?: string;
}

const REPORT_DIR = path.resolve(__dirname, "..", "test-results");
const SHOTS_DIR = path.join(REPORT_DIR, "screenshots");

function slug(p: string) {
  return p.replace(/[^a-z0-9]+/gi, "_").replace(/^_|_$/g, "") || "root";
}

function isIgnorableConsole(text: string): boolean {
  const ignore = [
    "next-themes",
    "[next-intl]",
    "Download the React DevTools",
    "Fast Refresh",
    "Hydration",
    "hydration",
    "Warning: Extra attributes from the server",
    "[Fast Refresh]",
  ];
  return ignore.some((k) => text.includes(k));
}

function attachErrorListeners(page: Page, errors: ErrItem[]) {
  page.on("console", (msg: ConsoleMessage) => {
    if (msg.type() !== "error") return;
    const text = msg.text();
    if (isIgnorableConsole(text)) return;
    errors.push({ url: page.url(), type: "console", message: text });
  });
  page.on("pageerror", (err: Error) => {
    errors.push({ url: page.url(), type: "pageerror", message: err.message });
  });
  page.on("response", (resp: Response) => {
    const req: Request = resp.request();
    const url = resp.url();
    if (url.includes("/_next/") || url.includes("/__next") || url.endsWith(".map")) return;
    if (resp.status() >= 500) {
      errors.push({
        url: page.url(),
        type: "response",
        message: req.method() + " " + url + " -> " + resp.status(),
      });
    }
  });
  page.on("requestfailed", (req: Request) => {
    const url = req.url();
    if (url.includes("/_next/") || url.endsWith(".map")) return;
    errors.push({
      url: page.url(),
      type: "response",
      message: "FAILED " + req.method() + " " + url + " :: " + (req.failure()?.errorText ?? "unknown"),
    });
  });
}

async function login(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.locator("#email").fill(ADMIN_EMAIL);
  await page.locator("#password").fill(ADMIN_PASSWORD);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30000 });
  await page.waitForLoadState("domcontentloaded", { timeout: 30000 }).catch(() => {});
}

test.describe("full walker", () => {
  test.describe.configure({ mode: "serial", timeout: 600_000 });

  test("walker toàn hệ thống", async ({ page }) => {
    fs.mkdirSync(SHOTS_DIR, { recursive: true });
    const allErrors: ErrItem[] = [];
    attachErrorListeners(page, allErrors);

    const results: WalkResult[] = [];

    await login(page);
    expect(page.url()).not.toContain("/login");

    const hrefs: string[] = await page.$$eval('aside a[href^="/"]', (els) =>
      Array.from(new Set(els.map((e) => (e as HTMLAnchorElement).getAttribute("href") ?? "")))
        .filter(Boolean)
    );
    console.log("[walker] Tìm thấy " + hrefs.length + " link sidebar: " + hrefs.join(", "));

    for (const href of hrefs) {
      const before = allErrors.length;
      let ok = true;
      try {
        const isSlowRoute = /lab.results|admin\/(tenants|audit)/.test(href);
        await page.goto(href, { waitUntil: "domcontentloaded", timeout: isSlowRoute ? 45_000 : 30_000 });
        await page.waitForLoadState("domcontentloaded", { timeout: isSlowRoute ? 45_000 : 15_000 }).catch(() => {});
        await page.waitForTimeout(1000);
        // Bắt 404-page-as-200: Next.js render not-found page với HTTP 200 nhưng nội dung "Không tìm thấy trang"
        const bodyText = await page.locator("body").innerText().catch(() => "");
        if (/Không tìm thấy trang|không tồn tại trong hệ thống/.test(bodyText)) {
          ok = false;
          allErrors.push({
            url: href,
            type: "pageerror",
            message: "404 NOT-FOUND PAGE rendered (route không có file page.tsx)",
          });
        }
      } catch (e: unknown) {
        ok = false;
        allErrors.push({
          url: href,
          type: "pageerror",
          message: "Navigation error: " + (e as Error).message,
        });
      }
      const added = allErrors.slice(before);
      const fail = !ok || added.length > 0;
      let screenshot: string | undefined;
      if (fail) {
        const file = path.join(SHOTS_DIR, slug(href) + ".png");
        await page.screenshot({ path: file, fullPage: true }).catch(() => {});
        screenshot = file;
      }
      results.push({ path: href, ok: !fail, errorsAdded: added, screenshot });
    }

    const tabs: Array<{ value: string; label: string; type: string }> = [
      { value: "financial", label: "Tài chính", type: "financial" },
      { value: "clinical", label: "Lâm sàng", type: "clinical" },
      { value: "pharmacy", label: "Kho dược", type: "pharmacy" },
    ];

    try {
      await page.goto("/reports", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.waitForLoadState("domcontentloaded", { timeout: 15_000 }).catch(() => {});
    } catch (e: unknown) {
      results.push({
        path: "/reports (setup)",
        ok: false,
        errorsAdded: [{ url: "/reports", type: "pageerror", message: (e as Error).message }],
      });
    }

    for (const tab of tabs) {
      const before = allErrors.length;
      let ok = true;
      try {
        await page.getByRole("tab", { name: new RegExp(tab.label === "Kho dược" ? "Kho dược|Dược|Tồn kho" : tab.label, "i") }).first().click({ timeout: 15_000 });
        await page.waitForTimeout(800);

        const popupPromise = page.context().waitForEvent("page", { timeout: 15_000 });
        await page.getByRole("button", { name: /In báo cáo/i }).first().click();
        const popup = await popupPromise;
        const popupErrors: ErrItem[] = [];
        attachErrorListeners(popup, popupErrors);
        await popup.waitForLoadState("domcontentloaded", { timeout: 30_000 });
        await popup.waitForLoadState("networkidle", { timeout: 15_000 }).catch(() => {});
        expect(popup.url()).toContain("/reports/print/" + tab.type);

        try {
          const dl = popup.waitForEvent("download", { timeout: 15_000 });
          await popup.getByRole("button", { name: /Tải PDF/i }).first().click();
          const download = await dl;
          const dlPath = await download.path();
          if (dlPath) {
            const size = fs.statSync(dlPath).size;
            if (size <= 1024) {
              popupErrors.push({
                url: popup.url(),
                type: "response",
                message: "PDF download quá nhỏ (" + size + " bytes)",
              });
            }
          }
        } catch (e: unknown) {
          popupErrors.push({
            url: popup.url(),
            type: "response",
            message: "PDF download lỗi: " + (e as Error).message,
          });
        }
        allErrors.push(...popupErrors);
        await popup.close();
      } catch (e: unknown) {
        ok = false;
        allErrors.push({
          url: "/reports tab " + tab.value,
          type: "pageerror",
          message: (e as Error).message,
        });
      }
      const added = allErrors.slice(before);
      const fail = !ok || added.length > 0;
      let screenshot: string | undefined;
      if (fail) {
        const file = path.join(SHOTS_DIR, "reports_" + tab.value + ".png");
        await page.screenshot({ path: file, fullPage: true }).catch(() => {});
        screenshot = file;
      }
      results.push({
        path: "/reports#" + tab.value,
        label: tab.label,
        ok: !fail,
        errorsAdded: added,
        screenshot,
      });
    }

    const profileItems = [
      { name: /Hồ sơ cá nhân/i, expectUrl: "/account/profile" },
      { name: /Bảo mật/i, expectUrl: "/account/security" },
      { name: /Thông báo/i, expectUrl: "/account/notifications" },
    ];
    for (const item of profileItems) {
      const before = allErrors.length;
      let ok = true;
      try {
        await page.getByRole("button", { name: /Menu tài khoản/i }).click({ timeout: 10_000 });
        await page.waitForSelector('[role="menuitem"]', { timeout: 5_000 }).catch(() => {});
        await page.getByRole("menuitem", { name: item.name }).click({ timeout: 10_000 }).catch(async () => {
          // menu có thể đã đóng — thử mở lại
          await page.getByRole("button", { name: /Menu tài khoản/i }).click({ timeout: 5_000 });
          await page.waitForSelector('[role="menuitem"]', { timeout: 5_000 });
          await page.getByRole("menuitem", { name: item.name }).click({ timeout: 10_000 });
        });
        await page.waitForLoadState("domcontentloaded", { timeout: 15_000 }).catch(() => {});
        await page.waitForTimeout(800);
        if (!page.url().includes(item.expectUrl)) {
          ok = false;
          allErrors.push({
            url: page.url(),
            type: "pageerror",
            message: "URL không khớp expect=" + item.expectUrl + ", actual=" + page.url(),
          });
        }
      } catch (e: unknown) {
        ok = false;
        allErrors.push({
          url: item.expectUrl,
          type: "pageerror",
          message: (e as Error).message,
        });
      }
      const added = allErrors.slice(before);
      const fail = !ok || added.length > 0;
      let screenshot: string | undefined;
      if (fail) {
        const file = path.join(SHOTS_DIR, "profile_" + slug(item.expectUrl) + ".png");
        await page.screenshot({ path: file, fullPage: true }).catch(() => {});
        screenshot = file;
      }
      results.push({
        path: item.expectUrl,
        label: String(item.name),
        ok: !fail,
        errorsAdded: added,
        screenshot,
      });
    }

    const summary = {
      total: results.length,
      pass: results.filter((r) => r.ok).length,
      fail: results.filter((r) => !r.ok).length,
      totalErrors: allErrors.length,
      generatedAt: new Date().toISOString(),
      results,
    };
    fs.writeFileSync(path.join(REPORT_DIR, "walker-report.json"), JSON.stringify(summary, null, 2), "utf-8");

    console.log("\n========= WALKER SUMMARY =========");
    console.log("Total: " + summary.total + " | PASS: " + summary.pass + " | FAIL: " + summary.fail);
    for (const r of results) {
      console.log("  " + (r.ok ? "PASS" : "FAIL") + "  " + r.path + (r.errorsAdded.length ? "  (" + r.errorsAdded.length + " err)" : ""));
      for (const e of r.errorsAdded.slice(0, 3)) {
        console.log("         [" + e.type + "] " + e.message.substring(0, 200));
      }
    }
    console.log("===================================`n");
    console.log("Report JSON: " + path.join(REPORT_DIR, "walker-report.json"));
  });
});

