/**
 * agents/doctor.ts — Mô phỏng thao tác bác sĩ trên UI thật (/encounters/new, /encounters/[id]):
 * tạo lượt khám, bắt đầu khám, ghi sinh hiệu, chẩn đoán ICD-10, viết bệnh án, kê đơn thuốc,
 * ký số & gửi ĐTQG, đóng lượt khám. Tôn trọng state machine: WAITING -> IN_PROGRESS -> DONE.
 */
import type { Page } from "@playwright/test";
import type { Persona } from "../personas";
import { pickOptionById, escapeRegExp, actionWithResponse, assertOkResponse } from "../helpers/ui";

export interface VitalsInput {
  heartRate: number;
  temperature: number;
  systolic: number;
  diastolic: number;
  spo2: number;
  weight: number;
  height: number;
}

const DEFAULT_VITALS: VitalsInput = {
  heartRate: 78,
  temperature: 36.8,
  systolic: 128,
  diastolic: 82,
  spo2: 98,
  weight: 62,
  height: 162,
};

export class DoctorAgent {
  /** Tạo lượt khám mới cho bệnh nhân qua /encounters/new, trả về encounterId lấy từ URL sau khi tạo. */
  async createEncounter(page: Page, persona: Persona, doctorFullName?: string): Promise<string> {
    await page.goto("/encounters/new", { waitUntil: "domcontentloaded", timeout: 30_000 });

    const patientInput = page.locator("#enc-patient-search");
    // Gõ tên kích hoạt GET /patients/search — bắt response song song để chẩn đoán rõ nếu lỗi quyền,
    // thay vì chỉ thấy dropdown không hiện rồi timeout mơ hồ khi chờ nút kết quả.
    // BN vừa được tạo có thể chưa xuất hiện ngay trong kết quả search (độ trễ index/replica) -> thử
    // lại tối đa 3 lần, xoá rồi gõ lại tên, chờ giữa các lần, trước khi bỏ cuộc.
    const option = page.getByRole("button", { name: new RegExp(escapeRegExp(persona.fullName)) }).first();
    let picked = false;
    for (let attempt = 1; attempt <= 3 && !picked; attempt++) {
      await patientInput.fill("");
      const searchResp = await actionWithResponse(page, () => patientInput.fill(persona.fullName), {
        urlIncludes: "/patients/search",
        method: "GET",
      });
      await assertOkResponse(searchResp, `Tìm bệnh nhân "${persona.fullName}" khi tạo lượt khám`);
      await page.waitForTimeout(600);
      picked = await option.waitFor({ state: "visible", timeout: 5000 }).then(() => true).catch(() => false);
      if (!picked && attempt < 3) await page.waitForTimeout(1500);
    }
    if (!picked) {
      throw new Error(`Không thấy bệnh nhân "${persona.fullName}" trong kết quả tìm khi tạo lượt khám (sau 3 lần thử)`);
    }
    await option.click();
    await page.getByText(`Đã chọn: ${persona.fullName}`).waitFor({ timeout: 5000 });

    if (doctorFullName) {
      await pickOptionById(page, "enc-doctor-select", doctorFullName).catch(() => {});
    }

    await pickOptionById(page, "enc-type", persona.isReturning ? "Tái khám" : "Khám mới").catch(() => {});

    await page.locator("#enc-reason").fill(persona.reason);
    await page
      .locator("#enc-complaint")
      .fill(`${persona.reason} — mô phỏng harness Pro-Diab HIS`)
      .catch(() => {});

    // Header sticky + footer sticky (desktop lg+) đều render nút "Tạo lượt khám" cùng lúc —
    // dùng .first() để tránh strict-mode violation (tương tự PatientEditorLayout).
    const createResp = await actionWithResponse(
      page,
      () =>
        page.getByRole("button", { name: "Tạo lượt khám", exact: true }).first().click({ timeout: 10_000 }),
      { urlIncludes: "/encounters", method: "POST" }
    );
    await assertOkResponse(createResp, `Tạo lượt khám cho "${persona.fullName}"`);
    // QUAN TRỌNG: regex PHẢI loại trừ "new" — nếu không, vì trang xuất phát chính là
    // "/encounters/new" nên waitForURL sẽ khớp NGAY LẬP TỨC với URL hiện tại (chưa hề điều hướng
    // thật), khiến encounterId bị gán nhầm literal "new". Bug này từng gây lỗi dây chuyền khó hiểu
    // ở các bước sau (mở nhầm "/encounters/new" thay vì trang chi tiết thật).
    await page.waitForURL(/\/encounters\/(?!new(?:$|[/?]))[^/?]+$/, { timeout: 20_000 });

    const match = page.url().match(/\/encounters\/(?!new(?:$|[/?]))([^/?]+)/);
    if (!match) throw new Error("Không lấy được encounterId từ URL sau khi tạo lượt khám");
    return match[1];
  }

  /** Mở lại trang chi tiết lượt khám theo id (dùng khi cần điều hướng lại giữa các bước). */
  async openEncounter(page: Page, encounterId: string): Promise<void> {
    if (!page.url().includes(`/encounters/${encounterId}`)) {
      await page.goto(`/encounters/${encounterId}`, { waitUntil: "domcontentloaded", timeout: 30_000 });
    }
    await page.getByRole("tab", { name: "Khám bệnh" }).waitFor({ timeout: 15_000 });
  }

  /** Best-effort: tìm lượt khám gần nhất của bệnh nhân từ danh sách /encounters. */
  async openEncounterForPatient(page: Page, persona: Persona): Promise<string> {
    await page.goto("/encounters", { waitUntil: "domcontentloaded", timeout: 30_000 });
    await page.waitForTimeout(800);
    const row = page.getByText(persona.fullName, { exact: false }).first();
    if (!(await row.count())) {
      throw new Error(`SKIP: không thấy lượt khám nào của "${persona.fullName}" trong danh sách /encounters`);
    }
    await row.click();
    await page.waitForURL(/\/encounters\/(?!new(?:$|[/?]))[^/?]+$/, { timeout: 15_000 });
    const match = page.url().match(/\/encounters\/(?!new(?:$|[/?]))([^/?]+)/);
    if (!match) throw new Error("SKIP: không xác định được encounterId từ danh sách lượt khám");
    return match[1];
  }

  /** Bấm "Bắt đầu khám" nếu lượt khám đang WAITING. Nếu đã IN_PROGRESS/DONE thì bỏ qua an toàn. */
  async startExam(page: Page): Promise<void> {
    const startBtn = page.getByRole("button", { name: "Bắt đầu khám", exact: true });
    // Ngay sau khi vừa điều hướng từ bước "Tạo lượt khám", trang chi tiết có thể còn đang loading
    // (Skeleton) -> .count() tức thời dễ chụp nhầm lúc 0 phần tử rồi bỏ qua click một cách SAI
    // (quan sát thực tế: bước này báo PASS ~100ms nhưng trạng thái DB vẫn WAITING, không hề chuyển
    // IN_PROGRESS). Chờ rõ ràng cho tới khi biết chắc nút có xuất hiện hay không.
    const appeared = await startBtn
      .waitFor({ state: "visible", timeout: 8000 })
      .then(() => true)
      .catch(() => false);
    if (appeared) {
      const resp = await actionWithResponse(page, () => startBtn.click({ timeout: 10_000 }), {
        urlIncludes: "/start",
        method: "POST",
      });
      await assertOkResponse(resp, "Bắt đầu khám");
      await page.waitForTimeout(600);
    }
  }

  /** Ghi sinh hiệu (chỉ khả dụng khi lượt khám IN_PROGRESS). */
  async recordVitals(page: Page, vitals?: Partial<VitalsInput>): Promise<void> {
    await page.getByRole("tab", { name: "Sinh hiệu" }).click({ timeout: 10_000 });
    const v: VitalsInput = { ...DEFAULT_VITALS, ...vitals };

    const hrInput = page.locator("#v-hr");
    if (!(await hrInput.count())) {
      throw new Error("SKIP: form nhập sinh hiệu không hiển thị (lượt khám có thể chưa IN_PROGRESS)");
    }
    await hrInput.fill(String(v.heartRate));
    await page.locator("#v-temp").fill(String(v.temperature));
    await page.getByLabel("HA tâm thu").fill(String(v.systolic));
    await page.getByLabel("HA tâm trương").fill(String(v.diastolic));
    await page.locator("#v-spo2").fill(String(v.spo2));
    await page.locator("#v-wt").fill(String(v.weight));
    await page.locator("#v-ht").fill(String(v.height));
    const resp = await actionWithResponse(
      page,
      () => page.getByRole("button", { name: "Lưu sinh hiệu" }).click({ timeout: 10_000 }),
      { urlIncludes: "/vital-signs", method: "POST" }
    );
    await assertOkResponse(resp, "Lưu sinh hiệu");
    await page.waitForTimeout(500);
  }

  /** Thêm 1 dòng chẩn đoán ICD-10 qua form nhập nhanh (#diag-code-0/#diag-name-0/#diag-type-0). */
  async addDiagnosis(page: Page, icd10Code: string, icd10Name: string): Promise<void> {
    await page.getByRole("tab", { name: "Chẩn đoán" }).click({ timeout: 10_000 });
    const codeInput = page.locator("#diag-code-0");
    if (!(await codeInput.count())) {
      throw new Error("SKIP: form nhập chẩn đoán nhanh không hiển thị (lượt khám có thể chưa IN_PROGRESS)");
    }
    await codeInput.fill(icd10Code);
    await page.locator("#diag-name-0").fill(icd10Name);
    await pickOptionById(page, "diag-type-0", "Chính").catch(() => {});
    const resp = await actionWithResponse(
      page,
      () => page.getByRole("button", { name: "Lưu chẩn đoán", exact: true }).click({ timeout: 10_000 }),
      { urlIncludes: "/diagnoses", method: "POST" }
    );
    await assertOkResponse(resp, `Lưu chẩn đoán ${icd10Code}`);
    await page.waitForTimeout(500);
  }

  /**
   * Nhập nội dung bệnh án tối giản vào EMR editor (tiptap contentEditable, class .ProseMirror) rồi
   * LƯU NHÁP ngay. EmrEditor.tsx chỉ auto-save sau 30s debounce (onUpdate) — nếu không bấm "Lưu nháp"
   * chủ động, bước ký số ở ngay sau sẽ nhận 400 "Chưa có bệnh án để ký" vì backend chưa có bản ghi nào.
   */
  async writeEmr(page: Page, text: string): Promise<void> {
    await page.getByRole("tab", { name: "Khám bệnh" }).click({ timeout: 10_000 });
    const editor = page.locator(".ProseMirror");
    if (!(await editor.count())) {
      throw new Error("SKIP: không tìm thấy EMR editor (.ProseMirror) để nhập nội dung bệnh án");
    }
    await editor.click();
    await editor.type(text, { delay: 5 });
    await page.waitForTimeout(300);

    const saveBtn = page.getByRole("button", { name: "Lưu nháp", exact: true });
    if (await saveBtn.count()) {
      const resp = await actionWithResponse(
        page,
        () => saveBtn.click({ timeout: 10_000 }),
        { urlIncludes: "/emr", method: "PUT" }
      );
      await assertOkResponse(resp, "Lưu nháp bệnh án");
    }
    await page.waitForTimeout(300);
  }

  /**
   * Ký số bệnh án điện tử (EmrSignDialog, mock signer) — BẮT BUỘC trước khi đóng lượt khám: backend
   * trả 422 "Bệnh án cần được ký số trước khi đóng lượt khám" nếu bỏ qua bước này. Nút "Ký số bệnh án"
   * chỉ render khi IN_PROGRESS (EncounterDetailClient.tsx); nếu đã ký trước đó (isSigned) nút đổi
   * thành "Đã ký số BA" và bị disable — coi như xong, không lỗi.
   */
  async signEmr(page: Page, pin = "123456"): Promise<void> {
    const alreadySigned = page.getByRole("button", { name: "Đã ký số BA", exact: true });
    if (await alreadySigned.count()) {
      return; // Bệnh án đã ký từ trước — không cần thao tác thêm.
    }

    const signBtn = page.getByRole("button", { name: "Ký số bệnh án", exact: true });
    const appeared = await signBtn
      .waitFor({ state: "visible", timeout: 8000 })
      .then(() => true)
      .catch(() => false);
    if (!appeared) {
      throw new Error("SKIP: không thấy nút \"Ký số bệnh án\" (lượt khám có thể chưa IN_PROGRESS)");
    }
    await signBtn.click({ timeout: 10_000 });

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });
    await dialog.locator("#pin-input").fill(pin);
    // Mock signer: onSign() gọi ngay POST /encounters/{id}/emr/sign, không có bước trung gian như
    // wizard ký đơn thuốc (chọn token -> PIN -> gửi ĐTQG).
    try {
      const resp = await actionWithResponse(
        page,
        () => dialog.getByRole("button", { name: "Ký số", exact: true }).click({ timeout: 10_000 }),
        { urlIncludes: "/emr/sign", method: "POST" }
      );
      await assertOkResponse(resp, "Ký số bệnh án điện tử");
      await page.waitForTimeout(500);
    } catch (e) {
      // EmrSignDialog CHỈ đóng khi onSign() thành công (setSignDialogOpen(false) nằm trong onSuccess
      // của mutation) — nếu API lỗi, dialog vẫn mở. Base UI Dialog gắn aria-hidden lên toàn bộ nội
      // dung nền khi modal đang mở, khiến các bước sau dùng getByRole trên trang nền (vd tab "Khám
      // bệnh") KHÔNG tìm thấy phần tử dù vẫn hiển thị -> FAIL dây chuyền rất khó hiểu. Chủ động đóng
      // dialog qua nút "Hủy" trước khi rethrow để trang trở lại trạng thái sạch cho bước kế tiếp.
      const cancelBtn = dialog.getByRole("button", { name: "Hủy", exact: true });
      if (await cancelBtn.count()) {
        await cancelBtn.click({ timeout: 5000 }).catch(() => {});
      }
      throw e;
    }
  }

  /**
   * Kê đơn: thêm lần lượt các thuốc trong drugNames vào đơn thuốc DRAFT (tab "Đơn thuốc").
   * Tự đảm bảo đang đứng đúng trang chi tiết lượt khám trước khi thao tác — quan sát thực tế: sau
   * bước CLS (điều hướng sang /labrad) đôi khi trang không quay lại đúng URL như kỳ vọng (nghi do
   * race điều hướng phía client), khiến tab "Đơn thuốc" không tồn tại và bước này FAIL mơ hồ.
   */
  async prescribe(page: Page, encounterId: string, drugNames: string[]): Promise<void> {
    await this.openEncounter(page, encounterId);
    await page.getByRole("tab", { name: "Đơn thuốc" }).click({ timeout: 10_000 });
    for (const drugName of drugNames) {
      await this.addOneDrug(page, drugName);
    }
  }

  private async addOneDrug(page: Page, drugName: string): Promise<void> {
    const searchInput = page.getByPlaceholder(/Tìm thuốc/i);
    await searchInput.waitFor({ timeout: 10_000 });
    await searchInput.fill("");

    // Gõ tên thuốc kích hoạt GET /drugs/search — bắt response song song để chẩn đoán rõ nếu lỗi quyền.
    const searchResp = await actionWithResponse(page, () => searchInput.fill(drugName), {
      urlIncludes: "/drugs/search",
      method: "GET",
    });
    await assertOkResponse(searchResp, `Tìm thuốc "${drugName}" trong DrugAutocomplete`);
    await page.waitForTimeout(500);

    const option = page.getByRole("option", { name: new RegExp(escapeRegExp(drugName)) }).first();
    if (!(await option.count())) {
      throw new Error(`Không tìm thấy thuốc "${drugName}" trong DrugAutocomplete`);
    }
    await option.waitFor({ state: "visible", timeout: 6000 });
    await option.click();

    // PrescriptionItemForm hiện ra để nhập liều dùng — điền tối thiểu, số lượng tự tính từ tần suất*số ngày.
    await page.locator("#dosage").fill("1 viên");
    await page.locator("#frequency").fill("2 lần/ngày");
    await page.locator("#duration_days").fill("7");
    // Lần thêm thuốc đầu tiên có thể vừa tạo đơn (POST /prescriptions) vừa thêm dòng
    // (POST /prescriptions/{id}/items) — dùng chuỗi con chung "/prescriptions" để bắt cả hai.
    const addResp = await actionWithResponse(
      page,
      () => page.getByRole("button", { name: "Thêm vào đơn", exact: true }).click({ timeout: 10_000 }),
      { urlIncludes: "/prescriptions", method: "POST" }
    );
    await assertOkResponse(addResp, `Thêm thuốc "${drugName}" vào đơn`);
    await page.waitForTimeout(500);
  }

  /**
   * Ký số & gửi ĐTQG đơn thuốc hiện tại (wizard 3 bước: chọn token -> nhập PIN -> gửi ĐTQG).
   * Trả về false (không throw) nếu nút ký bị khoá — kỳ vọng xảy ra khi có DDI chống chỉ định.
   */
  async signPrescription(page: Page, pin = "123456"): Promise<boolean> {
    const signBtn = page.getByRole("button", { name: /Ký số & gửi ĐTQG/i });
    // Nút CHỈ render khi đơn có >=1 dòng thuốc (canSign) — nếu bước kê đơn trước đó SKIP/FAIL (vd
    // do lỗi API tìm thuốc) thì nút này không tồn tại trong DOM, khác hẳn trường hợp "có nhưng bị
    // khoá do DDI". Phân biệt rõ 2 tình huống thay vì để timeout mơ hồ.
    const appeared = await signBtn
      .waitFor({ state: "visible", timeout: 10_000 })
      .then(() => true)
      .catch(() => false);
    if (!appeared) {
      throw new Error(
        'SKIP: không thấy nút "Ký số & gửi ĐTQG" — đơn thuốc chưa có dòng thuốc nào ' +
          "(bước kê đơn trước đó có thể đã SKIP/FAIL)"
      );
    }
    if (await signBtn.isDisabled()) {
      return false;
    }
    await signBtn.click();

    const dialog = page.getByRole("dialog");
    await dialog.waitFor({ timeout: 10_000 });

    await dialog.getByRole("combobox").first().click();
    await page.getByRole("option", { name: /Chứng thư phần mềm/i }).click();
    await dialog.getByRole("button", { name: "Tiếp theo", exact: true }).click();

    await dialog.locator("#pin").fill(pin);
    const signResp = await actionWithResponse(
      page,
      () => dialog.getByRole("button", { name: "Ký số", exact: true }).click(),
      { urlIncludes: "/sign", method: "POST" }
    );
    await assertOkResponse(signResp, "Ký số đơn thuốc");

    await dialog.getByRole("button", { name: /Gửi lên ĐTQG/i }).click({ timeout: 15_000 });
    await page.waitForTimeout(1000);

    const closeBtn = dialog.getByRole("button", { name: "Đóng", exact: true });
    if (await closeBtn.count()) {
      await closeBtn.click({ timeout: 10_000 });
    }
    return true;
  }

  /** Đóng lượt khám (chỉ hiển thị khi IN_PROGRESS). */
  async closeEncounter(page: Page): Promise<void> {
    const closeBtn = page.getByRole("button", { name: "Đóng lượt khám", exact: true });
    // Chờ rõ ràng (thay vì .count() tức thời) để tránh chụp nhầm lúc panel trạng thái đang re-render
    // sau khi đóng dialog ký số/ĐTQG — cùng nguyên nhân với race đã sửa ở startExam().
    const appeared = await closeBtn
      .waitFor({ state: "visible", timeout: 5000 })
      .then(() => true)
      .catch(() => false);
    if (!appeared) {
      throw new Error("SKIP: không thấy nút Đóng lượt khám (lượt khám có thể chưa IN_PROGRESS)");
    }
    const resp = await actionWithResponse(page, () => closeBtn.click({ timeout: 10_000 }), {
      urlIncludes: "/close",
      method: "POST",
    });
    await assertOkResponse(resp, "Đóng lượt khám");
    await page.waitForTimeout(800);
  }
}
