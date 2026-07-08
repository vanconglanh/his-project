/**
 * evidence-10.spec.ts — Chụp ảnh bằng chứng TỪNG BƯỚC cho 10 bệnh nhân đi TRỌN luồng khám bệnh
 * (tiếp đón -> khám -> sinh hiệu -> chẩn đoán -> bệnh án -> ký BA -> kê đơn -> ký & gửi ĐTQG ->
 * đóng lượt khám -> cấp phát thuốc -> thu tiền). Mở rộng từ sim/evidence.spec.ts (vốn chỉ 1 BN).
 *
 * Nguyên tắc: LUÔN chụp fullPage sau mỗi bước (kể cả khi bước lỗi) để thấy trạng thái THẬT tại thời
 * điểm đó; KHÔNG hard-fail giữa chừng — mọi lỗi được ghi lại (PASS/FAIL) rồi tiếp tục, tối đa hoá số
 * evidence thu được. Ảnh lưu theo từng BN: docs/test/evidence-shots/p01..p10/NN-buoc.png.
 * Kết quả tổng hợp ghi ra docs/test/evidence-shots/evidence-10-report.json để dựng report + verify.
 *
 * Persona sinh MỚI mỗi lần chạy (hậu tố timestamp+index) để không đụng "đã tiếp đón hôm nay" / trùng
 * dữ liệu giữa các lần chạy — an toàn khi chạy lặp trên prod.
 *
 * Chạy (prod): BASE_URL=https://his.diab.com.vn SIM_USE_ADMIN=1 ADMIN_PASSWORD=admin123 \
 *   npx playwright test evidence-10.spec.ts --config=e2e/sim/playwright.sim.config.ts
 */
import { test } from "@playwright/test";
import * as fs from "fs";
import * as path from "path";
import type { Page } from "@playwright/test";
import type { Persona } from "./personas";
import { EXAM_ROOMS } from "./clinic-config";
import { loginAs, attachErrorListeners } from "./helpers/session";
import { ReceptionAgent } from "./agents/reception";
import { DoctorAgent } from "./agents/doctor";
import { PharmacistAgent } from "./agents/pharmacist";
import { CashierAgent } from "./agents/cashier";

const reception = new ReceptionAgent();
const doctor = new DoctorAgent();
const pharmacist = new PharmacistAgent();
const cashier = new CashierAgent();

// __dirname = frontend/e2e/sim -> lên 3 cấp tới repo root -> docs/test/evidence-shots
const SHOTS_DIR = path.resolve(__dirname, "..", "..", "..", "docs", "test", "evidence-shots");
const REPORT_JSON = path.join(SHOTS_DIR, "evidence-10-report.json");

const PATIENT_COUNT = Number(process.env.EVIDENCE_PATIENTS || 10);

// ─── Danh mục chẩn đoán xoay vòng (rút gọn từ personas.ts, chỉ dùng thuốc chắc chắn có trong seed) ──
const CATALOG = [
  { icd10: "E11.9", name: "Đái tháo đường típ 2 không biến chứng", reason: "Đái tháo đường tái khám định kỳ", drugs: ["Metformin 500mg"] },
  { icd10: "I10", name: "Tăng huyết áp vô căn (nguyên phát)", reason: "Tăng huyết áp, tái khám định kỳ", drugs: ["Amlodipine 5mg"] },
  { icd10: "E78.5", name: "Rối loạn chuyển hoá lipid máu", reason: "Rối loạn mỡ máu, khám định kỳ", drugs: ["Atorvastatin 20mg"] },
  { icd10: "E11.4", name: "Đái tháo đường típ 2 có biến chứng thần kinh", reason: "Đái tháo đường, tê bì đầu chi", drugs: ["Metformin 500mg", "Amlodipine 5mg"] },
  { icd10: "K29.7", name: "Viêm dạ dày, không đặc hiệu", reason: "Đau thượng vị, ợ chua sau ăn", drugs: ["Omeprazole 20mg"] },
  { icd10: "J06.9", name: "Nhiễm khuẩn hô hấp trên cấp", reason: "Sốt, ho, đau họng 3 ngày", drugs: ["Paracetamol 500mg"] },
];

const SURNAMES = ["Nguyễn", "Trần", "Lê", "Phạm", "Hoàng", "Huỳnh", "Phan", "Vũ", "Võ", "Đặng"];
const GIVEN_M = ["An", "Bình", "Cường", "Dũng", "Hải", "Hùng", "Khang", "Long", "Nam", "Sơn"];
const GIVEN_F = ["Anh", "Chi", "Dung", "Hà", "Hoa", "Lan", "Linh", "Mai", "Thảo", "Trang"];

function buildPersonas(count: number): Persona[] {
  const runId = `${Date.now()}`.slice(-7); // 7 chữ số cuối của epoch ms — duy nhất giữa các lần chạy
  const out: Persona[] = [];
  for (let i = 0; i < count; i++) {
    const gender: Persona["gender"] = i % 2 === 0 ? "MALE" : "FEMALE";
    const given = gender === "MALE" ? GIVEN_M[i % GIVEN_M.length] : GIVEN_F[i % GIVEN_F.length];
    const tpl = CATALOG[i % CATALOG.length];
    const seq = String(i + 1).padStart(2, "0");
    const uniq = `${runId}${seq}`; // đủ dài để không trùng SĐT/CCCD giữa các lần chạy
    out.push({
      code: `EVD-${uniq}`,
      // Suffix runId PHẢI nằm trong fullName — nếu không, tên trùng giữa các lần chạy khiến ô tìm
      // kiếm tiếp đón trả nhiều BN cùng tên (chọn nhầm) và tạo hàng loạt bản ghi trùng trên prod.
      fullName: `${SURNAMES[i % SURNAMES.length]} ${gender === "MALE" ? "Văn" : "Thị"} ${given} ${seq}-${runId}`,
      gender,
      dob: `19${55 + i}-0${(i % 9) + 1}-1${(i % 8) + 1}`,
      phone: `09${uniq.padStart(8, "0").slice(0, 8)}`,
      idNumber: `079${uniq.padStart(9, "0")}`,
      patientType: i % 3 === 0 ? "BHYT" : "SERVICE",
      isReturning: false,
      day: 1,
      reason: tpl.reason,
      icd10: tpl.icd10,
      icd10Name: tpl.name,
      drugs: [...tpl.drugs],
      needsCls: false,
    });
  }
  return out;
}

interface StepResult {
  patient: string;
  patientCode: string;
  index: number; // thứ tự BN (1..N)
  step: number; // thứ tự bước trong 1 BN
  file: string; // đường dẫn tương đối từ SHOTS_DIR
  label: string;
  ok: boolean;
  error?: string;
}

const results: StepResult[] = [];

async function waitPageSettled(page: Page, waitReady?: () => Promise<unknown>): Promise<void> {
  await page.waitForLoadState("networkidle", { timeout: 10_000 }).catch(() => {});
  if (waitReady) await waitReady().catch(() => {});
  await page
    .waitForFunction(() => document.querySelectorAll('[data-slot="skeleton"]').length === 0, undefined, { timeout: 8_000 })
    .catch(() => {});
  await page.waitForTimeout(700);
}

test.describe.serial("Evidence 10 BN — luồng khám đầy đủ (prod-ready)", () => {
  test("Chup evidence tung buoc cho 10 benh nhan di tron luong", async ({ page }) => {
    test.setTimeout(30 * 60_000);
    fs.mkdirSync(SHOTS_DIR, { recursive: true });
    attachErrorListeners(page);

    const personas = buildPersonas(PATIENT_COUNT);
    console.log(`[evidence-10] Bat dau ${personas.length} benh nhan. Anh luu tai: ${SHOTS_DIR}`);

    for (let pi = 0; pi < personas.length; pi++) {
      const persona = personas[pi];
      const idx = pi + 1;
      const room = EXAM_ROOMS[pi % EXAM_ROOMS.length];
      const dir = `p${String(idx).padStart(2, "0")}`;
      fs.mkdirSync(path.join(SHOTS_DIR, dir), { recursive: true });
      let stepNo = 0;
      let encounterId = "";
      let billNo = "";
      console.log(`\n[evidence-10] ===== BN ${idx}/${personas.length}: ${persona.fullName} (${room}) =====`);

      // step(): luôn chụp ảnh sau khi chạy fn (dù lỗi), ghi kết quả vào results, KHÔNG throw ra ngoài.
      const step = async (name: string, label: string, fn: () => Promise<void>, waitReady?: () => Promise<unknown>) => {
        stepNo += 1;
        const file = `${dir}/${String(stepNo).padStart(2, "0")}-${name}.png`;
        let error: string | undefined;
        try {
          await fn();
        } catch (e) {
          error = e instanceof Error ? e.message : String(e);
          console.log(`[evidence-10]   LOI ${label} :: ${error}`);
        }
        try {
          await waitPageSettled(page, waitReady);
        } catch { /* trang co the dong dot ngot */ }
        try {
          await page.screenshot({ path: path.join(SHOTS_DIR, file), fullPage: true, timeout: 15_000 });
        } catch (shotErr) {
          console.log(`[evidence-10]   KHONG chup duoc ${file}: ${shotErr}`);
        }
        results.push({ patient: persona.fullName, patientCode: persona.code, index: idx, step: stepNo, file, label, ok: !error, error });
        if (!error) console.log(`[evidence-10]   OK  ${label}`);
      };

      // ── Lễ tân ────────────────────────────────────────────────────────────────
      await step("dang-nhap-le-tan", "Đăng nhập lễ tân", async () => {
        await loginAs(page, "letan");
      }, () => page.getByLabel("Menu tài khoản").waitFor({ state: "visible", timeout: 10_000 }));

      await step("tiep-don-chon-bn", `Tìm/tạo & chọn bệnh nhân ${persona.fullName}`, async () => {
        await reception.ensurePatient(page, persona);
      }, () => page.getByText(persona.fullName, { exact: false }).first().waitFor({ state: "visible", timeout: 8_000 }));

      await step("tiep-don-xong", `Tiếp đón vào ${room}`, async () => {
        await reception.checkIn(page, persona, room);
      }, () => page.getByText(persona.fullName, { exact: false }).first().waitFor({ state: "visible", timeout: 10_000 }));

      await step("goi-so", `Gọi bệnh nhân vào ${room}`, async () => {
        await reception.callNext(page, room);
      });

      // ── Bác sĩ ────────────────────────────────────────────────────────────────
      await loginAs(page, "bacsi").catch(() => {});

      await step("tao-luot-kham", "Tạo lượt khám", async () => {
        encounterId = await doctor.createEncounter(page, persona);
      }, () => page.getByRole("tab", { name: "Khám bệnh" }).waitFor({ state: "visible", timeout: 15_000 }));

      if (encounterId) {
        await step("bat-dau-kham", "Bắt đầu khám", async () => {
          await doctor.startExam(page);
        }, () => page.getByText("Đang khám", { exact: true }).first().waitFor({ state: "visible", timeout: 8_000 }));

        await step("sinh-hieu", "Ghi sinh hiệu", async () => {
          await doctor.recordVitals(page);
        }, () => page.getByText("36.8°C").first().waitFor({ state: "visible", timeout: 8_000 }));

        await step("chan-doan", `Ghi chẩn đoán ${persona.icd10}`, async () => {
          await doctor.addDiagnosis(page, persona.icd10, persona.icd10Name);
        }, () => page.getByText(persona.icd10, { exact: true }).first().waitFor({ state: "visible", timeout: 8_000 }));

        await step("benh-an", "Viết bệnh án (EMR)", async () => {
          await doctor.writeEmr(page, `Bệnh nhân ${persona.fullName} — ${persona.reason}. Chẩn đoán: ${persona.icd10Name}.`);
        }, () => page.getByText(/Đã lưu lúc/).first().waitFor({ state: "visible", timeout: 8_000 }));

        await step("ky-benh-an", "Ký số bệnh án", async () => {
          await doctor.signEmr(page);
        }, () => page.getByRole("button", { name: "Đã ký số BA", exact: true }).waitFor({ state: "visible", timeout: 8_000 }));

        await step("ke-don", "Kê đơn thuốc", async () => {
          await doctor.prescribe(page, encounterId, persona.drugs);
        }, () => page.getByRole("button", { name: /Ký số & gửi ĐTQG/i }).waitFor({ state: "visible", timeout: 8_000 }));

        let signed = false;
        await step("ky-dtqg", "Ký số & gửi ĐTQG", async () => {
          signed = await doctor.signPrescription(page);
          if (!signed) throw new Error("Nút ký đơn bị khoá (nghi cảnh báo DDI) — không ký được");
        }, async () => {
          await page.getByRole("dialog").waitFor({ state: "hidden", timeout: 8_000 }).catch(() => {});
          await page.getByText(/Đã ký|Đã gửi ĐTQG/).first().waitFor({ state: "visible", timeout: 8_000 });
        });

        await step("dong-luot-kham", "Đóng lượt khám", async () => {
          await doctor.closeEncounter(page);
        }, () => page.getByText("Lượt khám hoàn thành", { exact: false }).first().waitFor({ state: "visible", timeout: 8_000 }));

        // ── Dược sĩ: cấp phát ─────────────────────────────────────────────────
        if (signed) {
          await loginAs(page, "duocsi").catch(() => {});
          await step("cap-phat-thuoc", "Dược sĩ cấp phát thuốc", async () => {
            const ok = await pharmacist.dispense(page, persona);
            if (!ok) throw new Error(`Không thấy đơn của "${persona.fullName}" trong hàng chờ phát thuốc`);
          });

          // ── Tạo & finalize hoá đơn từ lượt khám (sau cấp phát -> gồm cả thuốc đã phát) để có
          //     hoá đơn ở "chờ thu" cho thu ngân. Dùng API (JWT từ localStorage) — HIS này không tự
          //     sinh hoá đơn khi đóng lượt khám. ──────────────────────────────────────────────────
          await step("tao-hoa-don", "Tạo & finalize hoá đơn từ lượt khám", async () => {
            const tk: string = await page.evaluate(() => {
              try { return JSON.parse(localStorage.getItem("auth-store") || "{}").state?.accessToken || ""; } catch { return ""; }
            });
            const apiBase = `${process.env.BASE_URL || "http://localhost:3100"}/api/v1`;
            const cr = await page.request.post(`${apiBase}/billings`, {
              headers: { Authorization: `Bearer ${tk}` },
              data: { encounter_id: encounterId, include_dispensing: true, payer: "SELF" },
            });
            if (!cr.ok()) throw new Error(`Tạo hoá đơn HTTP ${cr.status()}: ${(await cr.text()).slice(0, 200)}`);
            const created = (await cr.json())?.data;
            const bid = created?.id;
            billNo = created?.bill_no || "";
            if (!bid) throw new Error("Không lấy được id hoá đơn vừa tạo");
            // Lượt khám không có dịch vụ tính phí -> hoá đơn 0đ. Thử thêm 1 dòng phí khám để hoá đơn
            // > 0 (thu ngân mới có nút "Thu tiền"). KHÔNG fatal nếu backend build cũ trả 500 — vẫn
            // finalize để hoá đơn tồn tại (dù 0đ).
            const ir = await page.request.post(`${apiBase}/billings/${bid}/items`, {
              headers: { Authorization: `Bearer ${tk}` },
              data: { type: "SERVICE", name: "Phí khám bệnh", quantity: 1, unit_price: 150000 },
            }).catch(() => null);
            if (ir && !ir.ok()) console.log(`[evidence-10]   (thêm dòng phí khám HTTP ${ir.status()} — bỏ qua, finalize hoá đơn 0đ)`);
            const fr = await page.request.post(`${apiBase}/billings/${bid}/finalize`, {
              headers: { Authorization: `Bearer ${tk}` },
            });
            if (!fr.ok()) throw new Error(`Finalize hoá đơn HTTP ${fr.status()}: ${(await fr.text()).slice(0, 200)}`);
          });

          // ── Thu ngân: thu tiền ──────────────────────────────────────────────
          await loginAs(page, "ketoan").catch(() => {});
          await step("mo-ca-thu-ngan", "Thu ngân mở ca (nếu chưa mở)", async () => {
            await cashier.openShift(page);
          });
          await step("thu-tien", "Thu ngân thu tiền", async () => {
            const ok = await cashier.collect(page, persona, billNo);
            if (!ok) throw new Error(`Không thấy hoá đơn chờ thu (số HĐ ${billNo || "?"}) của "${persona.fullName}"`);
          });
        }
      }
    }

    // ── Ghi report tổng hợp ─────────────────────────────────────────────────────
    const summary = {
      generatedAt: new Date().toISOString(),
      baseUrl: process.env.BASE_URL || "http://localhost:3100",
      patientCount: personas.length,
      totalSteps: results.length,
      passed: results.filter((r) => r.ok).length,
      failed: results.filter((r) => !r.ok).length,
      patients: personas.map((p, i) => ({ index: i + 1, code: p.code, fullName: p.fullName, icd10: p.icd10, room: EXAM_ROOMS[i % EXAM_ROOMS.length] })),
      results,
    };
    fs.writeFileSync(REPORT_JSON, JSON.stringify(summary, null, 2), "utf-8");
    console.log(`\n[evidence-10] Hoan tat. ${summary.passed}/${summary.totalSteps} buoc OK. Report: ${REPORT_JSON}`);
  });
});
