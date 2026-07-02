/**
 * helpers/ui.ts — Thao tác UI dùng chung cho các agent mô phỏng: mở/chọn Select (base-ui),
 * gõ + chọn item trong ô tìm kiếm dạng dropdown (patient search, DrugAutocomplete...),
 * và chẩn đoán lỗi API (4xx/5xx) song song với hành động UI để báo cáo rõ nguyên nhân thay vì
 * chỉ ghi "timeout" mơ hồ khi backend trả lỗi (vd 403 PERMISSION_DENIED).
 * Không dùng data-testid (UI thật không có) — chỉ dùng role/label/text/id đã xác minh.
 */
import type { Page, Locator, Response as PwResponse } from "@playwright/test";

/** Escape ký tự đặc biệt regex khi build RegExp từ tên bệnh nhân/thuốc động. */
export function escapeRegExp(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** Mở Select (component base-ui, trigger role="combobox") theo locator rồi chọn option theo text/regex. */
export async function pickOption(
  page: Page,
  trigger: Locator,
  optionText: string | RegExp,
  timeout = 5000
): Promise<void> {
  await trigger.click({ timeout });
  const option = page.getByRole("option", { name: optionText }).first();
  await option.waitFor({ state: "visible", timeout });
  await option.click({ timeout });
}

/** Chọn option cho Select có id cố định trên SelectTrigger (vd #gender, #patient_type, #enc-type...). */
export async function pickOptionById(
  page: Page,
  triggerId: string,
  optionText: string | RegExp,
  timeout = 5000
): Promise<void> {
  await pickOption(page, page.locator(`#${triggerId}`), optionText, timeout);
}

/**
 * Chọn option cho Select KHÔNG có id, xác định trigger qua field wrapper chứa đúng Label text
 * (cấu trúc thực tế: <div><Label>{label}</Label><Select>...</Select></div>).
 */
export async function pickOptionByFieldLabel(
  page: Page,
  fieldLabelText: string,
  optionText: string | RegExp,
  timeout = 5000
): Promise<void> {
  const label = page.getByText(fieldLabelText, { exact: true }).first();
  const wrapper = label.locator("xpath=..");
  const trigger = wrapper.getByRole("combobox").first();
  await pickOption(page, trigger, optionText, timeout);
}

export interface DropdownPickOptions {
  /** Role của item trong dropdown: "button" (patient search) hoặc "option" (DrugAutocomplete). */
  role?: "button" | "option";
  /** Thời gian chờ debounce phía client trước khi dropdown render kết quả. */
  debounceMs?: number;
  timeout?: number;
}

/**
 * Gõ query vào 1 ô input rồi chọn item khớp trong dropdown kết quả.
 * Trả về false (không throw) nếu không tìm thấy item — để agent tự quyết định fallback.
 */
export async function typeAndPickFromDropdown(
  page: Page,
  input: Locator,
  query: string,
  optionMatcher: string | RegExp,
  opts?: DropdownPickOptions
): Promise<boolean> {
  const { role = "button", debounceMs = 500, timeout = 6000 } = opts ?? {};
  await input.fill("");
  await input.fill(query);
  await page.waitForTimeout(debounceMs);
  const option = page.getByRole(role, { name: optionMatcher }).first();
  try {
    await option.waitFor({ state: "visible", timeout });
    await option.click({ timeout });
    return true;
  } catch {
    return false;
  }
}

export interface ApiCheckOptions {
  /** Chuỗi con phải xuất hiện trong URL response cần bắt (vd "/reception/check-in"). */
  urlIncludes: string;
  /** Lọc thêm theo HTTP method của request gây ra response (vd "POST"). */
  method?: string;
  timeout?: number;
}

/**
 * Thực hiện 1 hành động (thường là click nút submit / gõ vào ô search) đồng thời lắng nghe
 * response API tương ứng, nhằm phát hiện SỚM lỗi 4xx/5xx (vd 403 PERMISSION_DENIED do backend
 * thiếu quyền/seed) thay vì chỉ dựa vào timeout chờ hiệu ứng UI rồi báo lỗi mơ hồ.
 * KHÔNG throw — trả về response khớp đầu tiên bắt được, hoặc null nếu không khớp trong thời gian chờ
 * (trường hợp này agent tự quyết định fallback như trước, không đổi hành vi cũ).
 */
export async function actionWithResponse(
  page: Page,
  action: () => Promise<void>,
  opts: ApiCheckOptions
): Promise<PwResponse | null> {
  const { urlIncludes, method, timeout = 10_000 } = opts;
  const respPromise = page
    .waitForResponse(
      (r) => r.url().includes(urlIncludes) && (!method || r.request().method() === method),
      { timeout }
    )
    .catch(() => null);
  await action();
  return respPromise;
}

/**
 * Nếu response lỗi (status >= 400), throw Error "SKIP: ..." kèm status + message backend trả về.
 * Backend trả 2 dạng envelope khác nhau tuỳ tầng lỗi:
 * - Business logic (Result.Conflict() trong controller) -> { error: { code, message } }.
 * - Model-binding/validation tự động của [ApiController] (vd JSON không convert được sang kiểu C#
 *   khai báo, model thiếu field bắt buộc) -> ProblemDetails chuẩn RFC 9110: { title, errors: {...} },
 *   KHÔNG có "error.message" -> nếu chỉ đọc error.message sẽ mất hoàn toàn nội dung lỗi thật, khiến
 *   agent chỉ báo "trả lỗi HTTP 400" mơ hồ. Đọc thêm "errors"/"title" để chẩn đoán chính xác.
 * Không làm gì nếu response null (không bắt được) hoặc thành công — agent tiếp tục theo logic cũ.
 */
export async function assertOkResponse(resp: PwResponse | null, context: string): Promise<void> {
  if (!resp || resp.status() < 400) return;
  let backendMsg = "";
  try {
    const body = await resp.json();
    backendMsg = body?.error?.message ?? body?.message ?? "";
    if (!backendMsg && body?.errors && typeof body.errors === "object") {
      const parts = Object.entries(body.errors as Record<string, unknown>).map(
        ([field, msgs]) => `${field}: ${Array.isArray(msgs) ? msgs.join("; ") : String(msgs)}`
      );
      backendMsg = parts.join(" | ");
    }
    if (!backendMsg && body?.title) {
      backendMsg = body.title;
    }
  } catch {
    /* body không phải JSON hoặc rỗng — bỏ qua, vẫn báo status */
  }
  throw new Error(
    `SKIP: ${context} — API ${resp.request().method()} ${new URL(resp.url()).pathname} trả lỗi HTTP ${resp.status()}` +
      `${backendMsg ? ` (${backendMsg})` : ""}`
  );
}
