/**
 * helpers/session.ts — Đăng nhập qua form UI thật (mặc định) + tuỳ chọn seed token vào
 * localStorage (thử nghiệm, KHÔNG dùng mặc định) + gắn listener bắt lỗi console/network
 * (copy pattern từ e2e/full-walker.spec.ts, lọc nhiễu next-themes/hydration...).
 */
import type { Page, ConsoleMessage, Request, Response } from "@playwright/test";
import { ROLES, USE_ADMIN, type RoleKey } from "../clinic-config";

/**
 * Đăng nhập qua form UI (#email, #password, nút "Đăng nhập").
 * Nếu USE_ADMIN=1 (env SIM_USE_ADMIN=1) thì LUÔN dùng tài khoản admin bất kể roleKey truyền vào
 * — hữu ích khi phân quyền RBAC theo role chưa đủ để chạy hết luồng mô phỏng.
 */
export async function loginAs(page: Page, roleKey: RoleKey): Promise<void> {
  const effectiveKey: RoleKey = USE_ADMIN ? "admin" : roleKey;
  const role = ROLES[effectiveKey];

  await page.goto("/login", { waitUntil: "domcontentloaded", timeout: 30_000 });

  // Nếu đã đăng nhập sẵn (redirect khỏi /login ngay), coi như xong — tránh fill trên trang sai.
  if (!page.url().includes("/login")) return;

  const emailInput = page.locator("#email");
  const passwordInput = page.locator("#password");
  // Next dev có thể compile route lần đầu khá lâu -> domcontentloaded có thể fire trước khi React
  // hydrate xong. Chờ input thật sự sẵn sàng trước khi fill, tránh giá trị bị hydration ghi đè lại
  // thành rỗng (quan sát thực tế: submit với #email rỗng dù đã fill() trước đó).
  await emailInput.waitFor({ state: "visible", timeout: 30_000 });

  // Thử tối đa 2 lần: fill -> submit -> nếu vẫn còn ở /login VÀ email bị rỗng (hydration race) thì
  // fill lại và thử lại; các lỗi khác (sai mật khẩu, backend chậm...) chờ đủ timeout ở lần cuối rồi
  // để nguyên trạng thái /login cho bước gọi sau tự phát hiện qua URL (không che giấu lỗi thật).
  const maxAttempts = 2;
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    await emailInput.fill(role.email);
    await passwordInput.fill(role.password);
    await page.getByRole("button", { name: /Đăng nhập/i }).click();

    const timeout = attempt < maxAttempts ? 10_000 : 30_000;
    const leftLogin = await page
      .waitForURL((u) => !u.toString().includes("/login"), { timeout })
      .then(() => true)
      .catch(() => false);
    if (leftLogin) break;

    const emailNowEmpty = (await emailInput.inputValue().catch(() => "")) === "";
    if (attempt < maxAttempts && emailNowEmpty) continue; // hydration race — thử lại
    break; // hết lượt thử hoặc lỗi khác (không phải hydration race) -> dừng, để nguyên trạng thái
  }
  await page.waitForLoadState("domcontentloaded", { timeout: 30_000 }).catch(() => {});
}

/**
 * [THỬ NGHIỆM — không dùng mặc định] Ghi thẳng access/refresh token + user vào localStorage
 * theo shape của zustand persist store "auth-store", nhằm bỏ qua form login khi cần chạy nhanh.
 * Yêu cầu: accessToken phải là JWT hợp lệ do backend cấp thật (vd lấy từ 1 lần login qua API),
 * nếu không request tiếp theo sẽ bị 401. Vì rủi ro này, mặc định toàn bộ harness dùng loginAs().
 */
export async function seedAuthToken(
  page: Page,
  data: {
    user: Record<string, unknown>;
    accessToken: string;
    refreshToken: string;
    permissions?: string[];
    roles?: string[];
  }
): Promise<void> {
  const payload = {
    state: {
      user: data.user,
      accessToken: data.accessToken,
      refreshToken: data.refreshToken,
      isAuthenticated: true,
      permissions: data.permissions ?? [],
      roles: data.roles ?? [],
    },
    version: 0,
  };
  await page.addInitScript(
    ([key, value]) => {
      window.localStorage.setItem(key, value);
    },
    ["auth-store", JSON.stringify(payload)] as [string, string]
  );
}

export interface ErrItem {
  url: string;
  type: "console" | "pageerror" | "response";
  message: string;
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

/**
 * Gắn listener bắt console.error/pageerror/HTTP 5xx/requestfailed, trả về mảng lỗi tích luỹ.
 * ĐỒNG THỜI tự động accept mọi native dialog (window.confirm/alert/beforeunload) — quan trọng vì
 * các form dài (PatientEditorLayout...) cảnh báo "beforeunload" khi rời trang lúc còn thay đổi
 * chưa lưu (vd sau khi submit lỗi 403/400). Nếu không accept, Playwright mặc định DISMISS dialog
 * này -> page.goto() điều hướng sang trang khác bị HUỶ NGẦM, khiến bước sau chờ phần tử trên trang
 * đích timeout mơ hồ dù chính bước điều hướng đã "chạy xong" không lỗi.
 */
export function attachErrorListeners(page: Page): ErrItem[] {
  const errors: ErrItem[] = [];

  page.on("dialog", (dialog) => {
    dialog.accept().catch(() => {});
  });

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
        message: `${req.method()} ${url} -> ${resp.status()}`,
      });
    }
  });

  page.on("requestfailed", (req: Request) => {
    const url = req.url();
    if (url.includes("/_next/") || url.endsWith(".map")) return;
    errors.push({
      url: page.url(),
      type: "response",
      message: `FAILED ${req.method()} ${url} :: ${req.failure()?.errorText ?? "unknown"}`,
    });
  });

  return errors;
}
