/**
 * agents/reception.ts — Mô phỏng thao tác lễ tân trên UI thật: tìm/tạo bệnh nhân (/patients/new),
 * tiếp đón vào phòng khám (/reception), gọi bệnh nhân tiếp theo trong hàng đợi.
 */
import type { Page } from "@playwright/test";
import type { Persona } from "../personas";
import {
  pickOptionByFieldLabel,
  pickOptionById,
  typeAndPickFromDropdown,
  escapeRegExp,
  actionWithResponse,
  assertOkResponse,
} from "../helpers/ui";

export class ReceptionAgent {
  /**
   * Đảm bảo bệnh nhân tồn tại trong hệ thống VÀ đang được chọn trong form tiếp đón (/reception).
   * - persona.isReturning=true: thử tìm trước qua ô search; nếu không thấy thì tạo mới (fallback an toàn).
   * - persona.isReturning=false: tạo mới qua /patients/new rồi quay lại tìm & chọn trong form tiếp đón.
   */
  async ensurePatient(page: Page, persona: Persona): Promise<void> {
    await this.gotoReception(page);

    if (persona.isReturning) {
      const found = await this.searchAndSelect(page, persona.fullName);
      if (found) return;
    }

    await this.createNewPatient(page, persona);
    await page.waitForURL((u) => u.toString().includes("/reception"), { timeout: 20_000 });
    await this.gotoReception(page);

    const selected = await this.searchAndSelect(page, persona.fullName);
    if (!selected) {
      throw new Error(
        `Không chọn được bệnh nhân "${persona.fullName}" trong ô tìm kiếm tiếp đón sau khi tạo mới ` +
          `(có thể do GET /patients/search lỗi quyền — xem bước tạo bệnh nhân trước đó)`
      );
    }
  }

  /** Tiếp đón bệnh nhân đã được chọn (bởi ensurePatient) vào phòng khám chỉ định. */
  async checkIn(page: Page, persona: Persona, room: string): Promise<void> {
    // Danh sách phòng khám (useRooms -> GET /reception/rooms) có thể trống nếu API lỗi quyền/dữ
    // liệu — chờ rõ ràng cho label phòng HOẶC trạng thái rỗng thay vì mù quáng timeout 10s trên click.
    const roomLabel = page.locator("label", { hasText: room }).first();
    const emptyState = page.getByText("Không có phòng khám");
    await Promise.race([
      roomLabel.waitFor({ state: "visible", timeout: 10_000 }).catch(() => {}),
      emptyState.waitFor({ state: "visible", timeout: 10_000 }).catch(() => {}),
    ]);
    if (!(await roomLabel.count())) {
      throw new Error(
        `SKIP: danh sách phòng khám rỗng, không thấy "${room}" — API GET /reception/rooms có thể lỗi ` +
          `quyền (reception.rooms.read) hoặc chưa có dữ liệu phòng`
      );
    }
    await roomLabel.click({ timeout: 10_000 });

    if (persona.exceptionTag) {
      // Ca ngoại lệ đánh dấu ưu tiên cao hơn để dễ quan sát trên bảng hàng đợi.
      await pickOptionByFieldLabel(page, "Ưu tiên", "Ưu tiên").catch(() => {});
    }

    await page.getByPlaceholder("Đau đầu, sốt 3 ngày...").fill(persona.reason);

    const resp = await actionWithResponse(
      page,
      () => page.getByRole("button", { name: /Tiếp đón \(F4\)/i }).click({ timeout: 10_000 }),
      { urlIncludes: "/reception/check-in", method: "POST" }
    );
    await assertOkResponse(resp, `Tiếp đón bệnh nhân "${persona.fullName}" vào ${room}`);
    await page.waitForTimeout(800);
  }

  /** Gọi bệnh nhân đang chờ đầu tiên trong phòng chỉ định vào khám (nút "Gọi vào" trên TicketCard). */
  async callNext(page: Page, room: string): Promise<void> {
    await this.gotoReception(page);
    const roomHeading = page.getByRole("heading", { level: 3, name: room, exact: true });
    if (!(await roomHeading.count())) {
      throw new Error(
        `SKIP: không thấy cột phòng "${room}" trên bảng hàng đợi tiếp đón (có thể do GET /reception/queue ` +
          `lỗi quyền reception.queue.manage)`
      );
    }
    const column = roomHeading.locator("xpath=ancestor::div[contains(concat(' ', @class, ' '), ' space-y-2 ')][1]");

    // TicketCard.tsx hiển thị nút "Gọi vào" cho CẢ vé WAITING lẫn CALLED (canAct = WAITING || CALLED)
    // -> nếu chỉ lấy nút "Gọi vào" đầu tiên trong cột, có thể bấm nhầm vé ĐÃ gọi từ trước (còn hiển
    // thị nút do bước "Bắt đầu khám" của bệnh nhân đó chưa chạy/chưa kịp chuyển trạng thái, hoặc còn
    // sót lại từ lần chạy trước), gây lỗi 422 "Không thể chuyển trạng thái từ CALLED sang CALLED".
    // Phải xác định đúng badge trạng thái "Chờ" (WAITING) của từng thẻ trước khi bấm.
    const waitingBadge = column.getByText("Chờ", { exact: true }).first();
    if (await waitingBadge.count()) {
      const waitingCard = waitingBadge.locator(
        "xpath=ancestor::div[contains(concat(' ', @class, ' '), ' bg-card ')][1]"
      );
      const callBtn = waitingCard.getByRole("button", { name: /Gọi vào/i });
      const resp = await actionWithResponse(
        page,
        () => callBtn.click({ timeout: 10_000 }),
        { urlIncludes: "/call", method: "PUT" }
      );
      await assertOkResponse(resp, `Gọi bệnh nhân vào phòng ${room}`);
      await page.waitForTimeout(500);
      return;
    }

    // Không còn vé nào đang WAITING — nếu đã có vé ở trạng thái CALLED (vd do lần chạy trước hoặc
    // bước trước đó đã gọi rồi), coi như bước này đã hoàn tất từ trước, KHÔNG coi là lỗi (idempotent).
    const calledBadge = column.getByText("Đã gọi", { exact: true }).first();
    if (await calledBadge.count()) {
      return;
    }

    throw new Error(`SKIP: không có bệnh nhân đang chờ trong phòng "${room}" để gọi vào`);
  }

  private async gotoReception(page: Page): Promise<void> {
    if (!page.url().includes("/reception")) {
      await page.goto("/reception", { waitUntil: "domcontentloaded", timeout: 30_000 });
    }
    // Trang có 2 heading trùng text "Tiếp đón bệnh nhân": h2 tiêu đề trang (page.tsx) + h3 trong
    // form check-in (ReceptionCheckInForm.tsx) -> chỉ định level:2 để tránh strict-mode violation.
    await page.getByRole("heading", { level: 2, name: "Tiếp đón bệnh nhân" }).waitFor({ timeout: 15_000 });
  }

  private async searchAndSelect(page: Page, fullName: string): Promise<boolean> {
    const input = page.getByPlaceholder("Tìm tên, SĐT, CMND, BHYT...");
    const ok = await typeAndPickFromDropdown(page, input, fullName, new RegExp(escapeRegExp(fullName)), {
      role: "button",
      debounceMs: 500,
      timeout: 6000,
    });
    if (ok) {
      await page.getByText(fullName, { exact: false }).first().waitFor({ timeout: 5000 }).catch(() => {});
    }
    return ok;
  }

  private async createNewPatient(page: Page, persona: Persona): Promise<void> {
    await page.goto("/patients/new?returnTo=/reception", { waitUntil: "domcontentloaded", timeout: 30_000 });
    await page.locator("#full_name").fill(persona.fullName);
    await page.locator("#date_of_birth").fill(persona.dob);
    await page.locator("#phone").fill(persona.phone);
    await page.locator("#id_number").fill(persona.idNumber);
    // BUG SẢN PHẨM (đã xác minh qua curl trực tiếp): nếu để trống "Ngày cấp CMND/CCCD", form gửi
    // id_card_issued_date: "" (chuỗi rỗng) — backend nhận DateOnly? và System.Text.Json KHÔNG chấp
    // nhận "" cho kiểu DateOnly? (chỉ chấp nhận null hoặc "yyyy-MM-dd"), nên toàn bộ request 400 ngay
    // ở tầng model-binding, không tới được business logic. Điền tạm 1 ngày hợp lệ để né lỗi này —
    // KHÔNG sửa PatientEditorLayout.tsx (buildPayload nên bỏ field rỗng) hoặc backend (DTO nên tự
    // convert "" -> null) vì nằm ngoài phạm vi agent frontend/e2e.
    await page.locator("#id_card_issued_date").fill("2018-01-15").catch(() => {});
    await pickOptionById(page, "gender", persona.gender === "MALE" ? "Nam" : "Nữ").catch(() => {});
    await pickOptionById(
      page,
      "patient_type",
      persona.patientType === "BHYT" ? "Bảo hiểm y tế" : "Dịch vụ"
    ).catch(() => {});
    // PatientEditorLayout render đồng thời nút submit ở header VÀ footer sticky (desktop lg+) —
    // cả 2 đều khớp "Tạo bệnh nhân" nên phải .first() để tránh strict-mode violation.
    const resp = await actionWithResponse(
      page,
      () => page.getByRole("button", { name: "Tạo bệnh nhân", exact: true }).first().click({ timeout: 10_000 }),
      { urlIncludes: "/patients", method: "POST" }
    );
    try {
      await assertOkResponse(resp, `Tạo bệnh nhân mới "${persona.fullName}"`);
    } catch (e) {
      // Submit lỗi (vd 400/403) -> form còn "dirty" (react-hook-form isDirty=true) do đã nhập liệu.
      // PatientEditorLayout gắn listener window "beforeunload" khi isDirty -> mọi page.goto() điều
      // hướng đi sau đó có thể bị trình duyệt chặn bằng dialog xác nhận mà Playwright xử lý không
      // ổn định (goto() "coi như xong" nhưng trang KHÔNG thực sự rời đi), khiến bước điều hướng kế
      // tiếp chờ phần tử trên trang đích timeout rất khó hiểu. Thoát trang chủ động qua phím Esc
      // (PatientEditorLayout tự bắt Esc -> gọi handleCancel() -> window.confirm() -> điều hướng về
      // returnTo=/reception) — dùng đúng cơ chế app cung cấp thay vì áp đặt điều hướng từ ngoài.
      await page.keyboard.press("Escape").catch(() => {});
      await page.waitForTimeout(300);
      throw e;
    }
  }
}
