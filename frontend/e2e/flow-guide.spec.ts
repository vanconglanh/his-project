/**
 * flow-guide.spec.ts — Chup anh cho TAI LIEU VAN HANH end-to-end.
 * Moi anh: thanh tieu de xanh (buoc) + KHOANH DO vung thao tac chinh.
 * BASE_URL=https://his.diab.com.vn npx playwright test --config=e2e/flow-guide.config.ts
 */
import { test, type Page, type Locator } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";

const EMAIL = process.env.EV_EMAIL || "admin@prodiab.local";
const PASSWORD = process.env.EV_PASSWORD || "admin123";
const DIR = path.resolve(__dirname, "..", "..", "docs", "test", "flow-guide-shots");
fs.mkdirSync(DIR, { recursive: true });

/** Mo trang login + dien san (chua submit) — de chup anh buoc 1. */
async function gotoLoginForm(page: Page) {
  await page.goto("/login", { waitUntil: "domcontentloaded", timeout: 40_000 });
  await page.locator("#email").waitFor({ state: "visible", timeout: 30_000 });
  await page.locator("#email").fill(EMAIL);
  await page.locator("#password").fill(PASSWORD);
}

/** Dang nhap ROBUST: submit + retry toi 4 lan (chiu rate-limit tam thoi), xac nhan da roi /login. */
async function doLogin(page: Page): Promise<boolean> {
  for (let a = 1; a <= 4; a++) {
    if (!page.url().includes("/login")) return true;
    await page.locator("#email").fill(EMAIL).catch(() => {});
    await page.locator("#password").fill(PASSWORD).catch(() => {});
    await page.getByRole("button", { name: /Đăng nhập/i }).click().catch(() => {});
    const ok = await page.waitForURL((u) => !u.toString().includes("/login"), { timeout: 15_000 }).then(() => true).catch(() => false);
    if (ok) { await page.waitForTimeout(1200); return true; }
    await page.waitForTimeout(8_000); // cho rate-limit dju bot roi thu lai
    await page.goto("/login", { waitUntil: "domcontentloaded" }).catch(() => {});
  }
  return false;
}

/** Xoa het danh dau cu. */
async function clearMarks(page: Page) {
  await page.evaluate(() => document.querySelectorAll("[data-fgn]").forEach((e) => e.removeAttribute("data-fgn"))).catch(() => {});
}

/** Danh dau 1 element voi so thu tu n (de shot() ve khung do + huy hieu so). */
async function mark(page: Page, loc: Locator, n: number): Promise<boolean> {
  const l = loc.first();
  if (!(await l.count().catch(() => 0))) return false;
  if (!(await l.isVisible({ timeout: 3000 }).catch(() => false))) return false;
  await l.scrollIntoViewIfNeeded().catch(() => {});
  await l.evaluate((e, n) => e.setAttribute("data-fgn", String(n)), n).catch(() => {});
  return true;
}

/** Danh dau 1 vung duy nhat (tuong duong so 1). */
async function focusEl(page: Page, loc: Locator): Promise<void> {
  await clearMarks(page);
  await mark(page, loc, 1);
}

/** Khoanh vung theo THU TU UU TIEN: element dau tien tim thay se duoc khoanh.
 *  Luon co fallback (tab dang chon / muc menu active / tieu de) de MOI anh deu co khung do. */
async function markFirst(page: Page, cands: Locator[]): Promise<void> {
  await clearMarks(page);
  for (const c of cands) { if (await mark(page, c, 1)) return; }
  // fallback: tab dang active -> muc menu trai active -> tieu de trang
  const fb = [
    page.locator('[role="tab"][data-state="active"], [role="tab"][aria-selected="true"]').first(),
    page.locator('aside a[aria-current="page"], nav a[aria-current="page"]').first(),
    page.locator("main h1, h1").first(),
  ];
  for (const f of fb) { if (await mark(page, f, 1)) return; }
}

/** Chup anh: caption bar xanh + cac khung do danh so quanh moi [data-fgn]. fullPage.
 *  (_focus: cho phep goi shot(..., await focusEl(...)) — focusEl da danh dau truoc do.) */
async function shot(page: Page, file: string, step: string, caption: string, _focus?: unknown) {
  if (page.isClosed()) return;
  await page.evaluate(({ step, caption }) => {
    document.querySelectorAll(".__fg").forEach((e) => e.remove());
    const cap = document.createElement("div"); cap.className = "__fg";
    cap.innerHTML = `<b>${step}</b>&nbsp;&nbsp;${caption}`;
    Object.assign(cap.style, { position: "absolute", top: "0", left: "0", right: "0", zIndex: "2147483647", background: "#01645A", color: "#fff", font: "600 16px system-ui,Segoe UI,sans-serif", padding: "12px 18px", textAlign: "center", boxShadow: "0 2px 8px rgba(0,0,0,.2)" });
    document.body.appendChild(cap);
    const single = document.querySelectorAll("[data-fgn]").length <= 1;
    document.querySelectorAll<HTMLElement>("[data-fgn]").forEach((el) => {
      const n = el.getAttribute("data-fgn") || "";
      const r = el.getBoundingClientRect();
      const x = r.left + window.scrollX, y = r.top + window.scrollY;
      const box = document.createElement("div"); box.className = "__fg";
      Object.assign(box.style, { position: "absolute", left: x - 5 + "px", top: y - 5 + "px", width: r.width + 10 + "px", height: r.height + 10 + "px", border: "3px solid #ef4444", borderRadius: "10px", zIndex: "2147483646", pointerEvents: "none", boxShadow: "0 0 0 3px rgba(239,68,68,.22)" });
      document.body.appendChild(box);
      if (!single) {
        const badge = document.createElement("div"); badge.className = "__fg"; badge.textContent = n;
        Object.assign(badge.style, { position: "absolute", left: x - 18 + "px", top: y - 18 + "px", width: "26px", height: "26px", background: "#ef4444", color: "#fff", borderRadius: "50%", font: "700 15px system-ui,Segoe UI,sans-serif", display: "flex", alignItems: "center", justifyContent: "center", zIndex: "2147483647", boxShadow: "0 1px 4px rgba(0,0,0,.35)" });
        document.body.appendChild(badge);
      }
    });
  }, { step, caption });
  await page.waitForTimeout(300);
  try { await page.screenshot({ path: path.join(DIR, file), fullPage: true, timeout: 15_000 }); }
  catch { await page.screenshot({ path: path.join(DIR, file), fullPage: false }).catch(() => {}); }
  await page.evaluate(() => document.querySelectorAll(".__fg").forEach((e) => e.remove())).catch(() => {});
  console.log(`[shot] ${file}`);
}

async function goto(page: Page, url: string, wait = 1800) {
  await page.goto(url, { waitUntil: "domcontentloaded", timeout: 40_000 }).catch(() => {});
  await page.waitForTimeout(wait);
}

test("flow-guide — chup anh tai lieu van hanh", async ({ page }) => {
  test.setTimeout(15 * 60_000);

  // 01 — Dang nhap (chup form truoc khi submit)
  await gotoLoginForm(page);
  await shot(page, "01-dang-nhap.png", "Bước 1 · Đăng nhập",
    "Nhập email + mật khẩu rồi bấm nút xanh “Đăng nhập”.",
    await focusEl(page, page.getByRole("button", { name: /Đăng nhập/i })));
  const authed = await doLogin(page);
  if (!authed) throw new Error("Đăng nhập THẤT BẠI (rate-limit?) — dừng để không chụp nhầm trang login.");

  // 02 — Dashboard
  await markFirst(page, [
    page.locator('aside a[aria-current="page"], nav a[aria-current="page"]').first(),
    page.getByRole("button", { name: /Làm mới/i }),
  ]);
  await shot(page, "02-dashboard.png", "Màn hình chính · Tổng quan",
    "Sau khi đăng nhập là màn Tổng quan. Menu chức năng ở cột trái (vùng khoanh đỏ) — bấm để mở từng màn.");

  // 03 — Tiep don: khoanh 4 vung danh so
  await goto(page, "/reception");
  await clearMarks(page);
  await mark(page, page.getByPlaceholder(/Tìm tên/i), 1);
  await mark(page, page.getByText(/Phòng khám số 1/i).first(), 2);
  await mark(page, page.getByPlaceholder(/Đau đầu, sốt/i), 3);
  await mark(page, page.getByRole("button", { name: /Tiếp đón/i }), 4);
  await shot(page, "03-tiep-don.png", "Bước 2 · Tiếp đón bệnh nhân",
    "① Tìm/chọn bệnh nhân · ② Chọn phòng khám · ③ Nhập lý do khám · ④ Bấm “Tiếp đón (F4)”.");

  // 04 — Dua vao kham (nut moi tren the ve)
  await markFirst(page, [
    page.getByRole("button", { name: /Đưa vào khám/i }),
    page.getByRole("button", { name: /Gọi vào/i }),
  ]);
  await shot(page, "04-dua-vao-kham.png", "Bước 2b · Đưa bệnh nhân vào khám",
    "Trên thẻ bệnh nhân ở hàng đợi, bấm nút xanh “Đưa vào khám” để mở lượt khám cho bác sĩ.");

  // 05 — Benh nhan (nut Them benh nhan)
  await goto(page, "/patients");
  await markFirst(page, [
    page.getByRole("button", { name: /Thêm bệnh nhân|Tạo bệnh nhân/i }),
    page.getByPlaceholder(/Tìm/i),
  ]);
  await shot(page, "05-benh-nhan.png", "Danh mục · Bệnh nhân",
    "Tra cứu hồ sơ bệnh nhân. Bấm “Thêm bệnh nhân (F2)” (vùng khoanh đỏ) để tạo hồ sơ mới.");

  // 06 — Kham benh list (nut Tao luot kham)
  await goto(page, "/encounters");
  await markFirst(page, [
    page.getByRole("button", { name: /Tạo lượt khám/i }),
    page.getByRole("button", { name: /Đang khám hôm nay|Chờ khám/i }),
  ]);
  await shot(page, "06-kham-benh-ds.png", "Bước 4 · Danh sách khám bệnh",
    "Bấm “Đang khám hôm nay” / “Chờ khám” để lọc hôm nay. Bấm “Tạo lượt khám” (vùng khoanh đỏ) để mở ca khám mới.");

  // 07 — Tao luot kham (form): khoanh cac o + nut
  await goto(page, "/encounters/new");
  await clearMarks(page);
  await mark(page, page.locator("#enc-patient-search"), 1);
  await mark(page, page.locator("#enc-doctor-select"), 2);
  await mark(page, page.locator("#enc-type"), 3);
  await mark(page, page.locator("#enc-reason"), 4);
  await mark(page, page.getByRole("button", { name: /^Tạo lượt khám$/i }), 5);
  await shot(page, "07-tao-luot-kham.png", "Bước 4b · Tạo lượt khám",
    "① Chọn bệnh nhân · ② Chọn bác sĩ · ③ Loại khám · ④ Lý do khám · ⑤ Bấm “Tạo lượt khám”.");

  // 08 — Chi tiet kham: mo 1 luot kham co san (read-only, tranh crash)
  await goto(page, "/encounters", 2200);
  await page.getByRole("row").nth(1).click({ timeout: 5000 }).catch(() => {});
  await page.waitForTimeout(2500);
  // Uu tien "Bat dau kham"; neu dang kham -> "Dong luot kham"; fallback tab "Kham benh".
  await markFirst(page, [
    page.getByRole("button", { name: /Bắt đầu khám/i }),
    page.getByRole("button", { name: /Đóng lượt khám|Ký số bệnh án/i }),
    page.getByRole("tab", { name: /Khám bệnh/i }),
  ]);
  await shot(page, "08-chi-tiet-kham.png", "Bước 5 · Màn khám bệnh",
    "Bấm “Bắt đầu khám” để vào khám (vé ở Tiếp đón tự chuyển “Đang khám”). Nhập theo các tab ở giữa: Khám bệnh, Sinh hiệu, Chẩn đoán, CLS, Đơn thuốc.");

  // 09 — Dieu duong (co the empty -> khoanh tab "Danh sach cho")
  await goto(page, "/nurse");
  await markFirst(page, [
    page.getByRole("button", { name: /Nhập sinh hiệu/i }),
    page.getByRole("tab", { name: /Danh sách chờ/i }),
  ]);
  await shot(page, "09-dieu-duong.png", "Bước 3 · Điều dưỡng — Sinh hiệu",
    "Ở tab “Danh sách chờ” (vùng khoanh đỏ), chọn bệnh nhân → “Nhập sinh hiệu” (mạch, huyết áp, nhiệt độ, SpO2, cân nặng).");

  // 10 — CLS
  await goto(page, "/labrad");
  await markFirst(page, [
    page.getByRole("button", { name: /Nhập kết quả/i }),
    page.getByRole("tab", { name: /Kết quả xét nghiệm/i }),
  ]);
  await shot(page, "10-cls.png", "Bước 6 · Cận lâm sàng (CLS)",
    "Nhập & xác thực kết quả xét nghiệm/CĐHA. Bấm “+ Nhập kết quả” (vùng khoanh đỏ); sau đó “Xác thực”.");

  // 11 — Ke don
  await goto(page, "/prescriptions");
  await markFirst(page, [
    page.getByRole("button", { name: /Tạo đơn mới|Tạo đơn/i }),
    page.getByPlaceholder(/Tìm/i),
  ]);
  await shot(page, "11-ke-don.png", "Bước 7 · Kê đơn thuốc",
    "Bấm “Tạo đơn mới” (vùng khoanh đỏ), thêm thuốc (có cảnh báo tương tác CDSS) → “Ký số & gửi ĐTQG”.");

  // 12 — Thu ngan: khoanh nut Mo ca + tab
  await goto(page, "/cashier");
  await clearMarks(page);
  await mark(page, page.getByRole("button", { name: /Mở ca|Thu tiền|Đóng ca/i }), 1);
  await mark(page, page.getByRole("tab", { name: /Hoá đơn chờ thu|Chờ thu/i }), 2);
  await shot(page, "12-thu-ngan.png", "Bước 8 · Thu ngân",
    "① Mở ca đầu giờ · ② Tab “Hoá đơn chờ thu” → chọn hoá đơn → “Thu tiền” → in phiếu thu.");

  // 13 — Cap phat thuoc
  await goto(page, "/pharmacy/dispense");
  await markFirst(page, [
    page.getByRole("button", { name: /Phát thuốc/i }),
    page.getByRole("tab", { name: /Hàng chờ/i }),
  ]);
  await shot(page, "13-cap-phat.png", "Bước 9 · Cấp phát thuốc",
    "Ở tab “Hàng chờ” (vùng khoanh đỏ), chọn đơn của bệnh nhân → “Phát thuốc” (chọn lô theo hạn dùng). Bệnh nhân nhận thuốc và ra về.");

  // 14 — Lich hen (tai kham)
  await goto(page, "/appointments");
  await markFirst(page, [
    page.getByRole("button", { name: /Tạo lịch hẹn/i }),
  ]);
  await shot(page, "14-lich-hen.png", "Bước 10 · Lịch hẹn / Tái khám",
    "Bấm “Tạo lịch hẹn” (vùng khoanh đỏ) để đặt lịch tái khám; xác nhận / check-in khi bệnh nhân đến.");

  // 15 — Nhac tai kham (co the empty -> khoanh bo loc trang thai)
  await goto(page, "/recall");
  await markFirst(page, [
    page.getByRole("button", { name: /Đã gọi/i }),
    page.getByRole("button", { name: /Gửi SMS/i }),
    page.getByRole("combobox").first(),
  ]);
  await shot(page, "15-nhac-tai-kham.png", "Bước 10b · Nhắc tái khám",
    "Mở “Nhắc tái khám” ở menu (vùng khoanh đỏ). Khi có bệnh nhân quá hạn, mỗi dòng có nút “Đã gọi” / “Gửi SMS”.");

  // 16 — Nguy co DTD (khoanh bo loc muc nguy co / dong dau)
  await goto(page, "/diabetes/risk-list");
  await markFirst(page, [
    page.getByRole("combobox").first(),
    page.getByRole("row").nth(1),
  ]);
  await shot(page, "16-nguy-co-dtd.png", "Bước 10c · Danh sách nguy cơ ĐTĐ",
    "Mở “Danh sách nguy cơ ĐTĐ” ở menu (vùng khoanh đỏ). Bệnh nhân xếp theo mức nguy cơ; bấm một dòng để xem biểu đồ xu hướng.");

  // 17 — BHYT
  await goto(page, "/bhyt");
  await markFirst(page, [
    page.getByRole("button", { name: /Tạo kỳ mới/i }),
    page.getByRole("tab", { name: /Kỳ xuất/i }),
  ]);
  await shot(page, "17-bhyt.png", "Bước 11 · BHYT",
    "Tab “Kỳ xuất”: bấm “Tạo kỳ mới” (vùng khoanh đỏ) để sinh XML theo tháng. Tab “Đối soát” để nạp kết quả giám định.");

  // 18 — Bao cao
  await goto(page, "/reports");
  await markFirst(page, [
    page.getByRole("button", { name: /In báo cáo|Xuất/i }),
    page.getByRole("tab").first(),
  ]);
  await shot(page, "18-bao-cao.png", "Bước 12 · Báo cáo & Thống kê",
    "Chọn nhóm/loại báo cáo + khoảng ngày (tối đa 366 ngày) → xem số liệu → “In báo cáo” (vùng khoanh đỏ) để xuất CSV/Excel/PDF.");

  console.log("[flow-guide] DONE -> " + DIR);
});
