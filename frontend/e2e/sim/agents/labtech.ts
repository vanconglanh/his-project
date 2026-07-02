/**
 * agents/labtech.ts — Mô phỏng kỹ thuật viên xét nghiệm nhập kết quả CLS trên /labrad.
 * Màn hình hiện quản lý theo danh sách kết quả toàn hệ thống, chưa có bước lọc/khớp rõ ràng
 * theo đúng bệnh nhân + lượt khám trong phạm vi khảo sát UI — best-effort, SKIP an toàn.
 */
import type { Page } from "@playwright/test";
import type { Persona } from "../personas";

export class LabTechAgent {
  /**
   * Khảo sát màn hình nhập kết quả CLS cho bệnh nhân (luôn SKIP có ghi chú — xem docstring class).
   * QUAN TRỌNG: dù PASS/SKIP/lỗi, LUÔN điều hướng page trở lại đúng trang chi tiết lượt khám
   * (encounterId) trước khi return/throw — nếu không các bước kê đơn/ký số sau đó (chạy trên cùng
   * `page`, không re-login) sẽ bị kẹt trên /labrad và FAIL với lý do gây nhiễu (vd "không tìm thấy
   * tab Đơn thuốc") thay vì phản ánh đúng bản chất (CLS chưa hỗ trợ trong phạm vi harness).
   */
  async enterResults(page: Page, persona: Persona, encounterId: string): Promise<void> {
    if (!persona.needsCls) {
      throw new Error(`SKIP: bệnh nhân "${persona.fullName}" không có chỉ định CLS trong kịch bản này`);
    }

    try {
      await page.goto("/labrad", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.getByRole("heading", { name: "Cận lâm sàng (CLS)" }).waitFor({ timeout: 15_000 });
      await page.getByRole("tab", { name: "Kết quả xét nghiệm" }).click({ timeout: 10_000 });

      const enterBtn = page.getByRole("button", { name: /Nhập kết quả/i });
      if (!(await enterBtn.count())) {
        throw new Error('SKIP: không tìm thấy nút "+ Nhập kết quả" trên tab Kết quả xét nghiệm');
      }
      await enterBtn.click({ timeout: 10_000 });
      await page.waitForTimeout(500);

      throw new Error(
        `SKIP: đã mở form nhập kết quả XN nhưng /labrad hiện quản lý theo danh sách chỉ định toàn hệ thống, ` +
          `chưa xác định được bước lọc/khớp đúng theo bệnh nhân "${persona.fullName}" và lượt khám tương ứng ` +
          `trong phạm vi khảo sát UI — bỏ qua nhập chi tiết kết quả trong mô phỏng này.`
      );
    } finally {
      await page
        .goto(`/encounters/${encounterId}`, { waitUntil: "domcontentloaded", timeout: 30_000 })
        .catch(() => {});
      await page
        .getByRole("tab", { name: "Khám bệnh" })
        .waitFor({ timeout: 15_000 })
        .catch(() => {});
    }
  }
}
