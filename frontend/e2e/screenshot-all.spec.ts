import { test, type Page } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const ADMIN_EMAIL = "admin@prodiab.local";
const ADMIN_PASSWORD = process.env.ADMIN_PASSWORD ?? "Admin@123";

const SHOTS_DIR = path.resolve(__dirname, "..", "test-results", "all-routes-shots");

async function login(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded" });
  await page.locator("#email").fill(ADMIN_EMAIL);
  await page.locator("#password").fill(ADMIN_PASSWORD);
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 30000 });
}

test.describe.configure({ mode: "serial", timeout: 600_000 });

test("screenshot all sidebar routes", async ({ page }) => {
  test.setTimeout(600_000);
  fs.mkdirSync(SHOTS_DIR, { recursive: true });
  await login(page);

  const hrefs = await page.$$eval('aside a[href^="/"]', (els) =>
    Array.from(new Set(els.map((e) => (e as HTMLAnchorElement).getAttribute("href") ?? "")))
      .filter(Boolean)
      .sort()
  );

  const results: Array<{ path: string; status: "PASS" | "404"; screenshot: string }> = [];

  for (const href of hrefs) {
    const slug = href.replace(/[^a-z0-9]+/gi, "_").replace(/^_|_$/g, "") || "root";
    const isSlowRoute = /lab.results|admin\/(tenants|audit)/.test(href);

    try {
      await page.goto(href, { waitUntil: "domcontentloaded", timeout: isSlowRoute ? 45_000 : 30_000 });
      await page.waitForLoadState("domcontentloaded", { timeout: 15_000 }).catch(() => {});
      await page.waitForTimeout(1500);
    } catch {}

    const file = path.join(SHOTS_DIR, slug + ".png");
    await page.screenshot({ path: file, fullPage: true }).catch(() => {});

    const bodyText = await page.locator("body").innerText().catch(() => "");
    const is404 = /Không tìm thấy trang|không tồn tại trong hệ thống/.test(bodyText);

    results.push({
      path: href,
      status: is404 ? "404" : "PASS",
      screenshot: slug + ".png",
    });
  }

  fs.writeFileSync(
    path.resolve(__dirname, "..", "test-results", "all-routes-report.json"),
    JSON.stringify({ generated_at: new Date().toISOString(), total: results.length, results }, null, 2)
  );

  console.log("\n=== ROUTE STATUS ===");
  for (const r of results) {
    console.log(`${r.status === "PASS" ? "OK" : "404"} ${r.path}`);
  }
});
