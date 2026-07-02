/**
 * clinic-simulation.spec.ts — Mô phỏng luồng khám bệnh thật cho tối đa 50 bệnh nhân (rải 5 "ngày"
 * x 10 BN/ngày) qua UI: Tiếp đón -> Khám (sinh hiệu, chẩn đoán, bệnh án) -> (CLS nếu cần)
 * -> Kê đơn & ký số -> Cấp phát -> Thu ngân. Chạy tuần tự (workers:1, xem playwright.sim.config.ts).
 *
 * Nguyên tắc: KHÔNG hard-fail giữa chừng — mọi bước bọc runStep() (PASS/FAIL/SKIP được ghi lại,
 * không throw ra ngoài). Assert cứng DUY NHẤT là bước đăng nhập sanity ban đầu.
 */
import { test, expect } from "@playwright/test";
import * as path from "path";
import { SIM_PATIENTS, EXAM_ROOMS } from "./clinic-config";
import { limitTo, patientsForDay, type Persona } from "./personas";
import { loginAs, attachErrorListeners } from "./helpers/session";
import { runStep, saveReport } from "./helpers/report";
import { ReceptionAgent } from "./agents/reception";
import { DoctorAgent } from "./agents/doctor";
import { PharmacistAgent } from "./agents/pharmacist";
import { CashierAgent } from "./agents/cashier";
import { LabTechAgent } from "./agents/labtech";

const reception = new ReceptionAgent();
const doctor = new DoctorAgent();
const pharmacist = new PharmacistAgent();
const cashier = new CashierAgent();
const labtech = new LabTechAgent();

const ALL_DAYS = [1, 2, 3, 4, 5] as const;

test.describe.serial("Mô phỏng phòng khám Pro-Diab HIS", () => {
  const patients = limitTo(SIM_PATIENTS);

  test("Sanity: đăng nhập lễ tân trước khi mô phỏng", async ({ page }) => {
    attachErrorListeners(page);
    await loginAs(page, "letan");
    expect(page.url(), "Đăng nhập lễ tân phải rời khỏi trang /login").not.toContain("/login");
  });

  for (const day of ALL_DAYS) {
    const dayPatients = patientsForDay(day, patients);
    if (dayPatients.length === 0) continue;

    test(`Ngày ${day}: mô phỏng ${dayPatients.length} bệnh nhân`, async ({ page }) => {
      attachErrorListeners(page);

      for (let i = 0; i < dayPatients.length; i++) {
        const persona: Persona = dayPatients[i];
        const room = EXAM_ROOMS[i % EXAM_ROOMS.length];
        const meta = { day, patientCode: persona.code, patientName: persona.fullName };
        const doctorRole = day % 2 === 0 ? "bacsi2" : "bacsi";

        // ── Lễ tân: tiếp đón ────────────────────────────────────────────────
        await runStep("Lễ tân đăng nhập", page, async () => {
          await loginAs(page, "letan");
        }, meta);

        await runStep(`Lễ tân tìm/tạo bệnh nhân ${persona.fullName}`, page, async () => {
          await reception.ensurePatient(page, persona);
        }, meta);

        await runStep(`Lễ tân tiếp đón vào ${room}`, page, async () => {
          await reception.checkIn(page, persona, room);
        }, meta);

        await runStep("Lễ tân gọi bệnh nhân vào khám", page, async () => {
          await reception.callNext(page, room);
        }, meta);

        // ── Bác sĩ: khám + kê đơn ───────────────────────────────────────────
        let encounterId = "";
        await runStep("Bác sĩ đăng nhập", page, async () => {
          await loginAs(page, doctorRole);
        }, meta);

        await runStep("Bác sĩ tạo lượt khám", page, async () => {
          encounterId = await doctor.createEncounter(page, persona);
        }, meta);

        if (!encounterId) {
          continue; // Không có lượt khám thì không thể tiếp tục các bước sau cho BN này.
        }

        await runStep("Bác sĩ bắt đầu khám", page, async () => {
          await doctor.startExam(page);
        }, meta);

        await runStep("Bác sĩ ghi sinh hiệu", page, async () => {
          await doctor.recordVitals(page);
        }, meta);

        await runStep(`Bác sĩ ghi chẩn đoán ${persona.icd10}`, page, async () => {
          await doctor.addDiagnosis(page, persona.icd10, persona.icd10Name);
        }, meta);

        await runStep("Bác sĩ ghi bệnh án", page, async () => {
          await doctor.writeEmr(
            page,
            `Bệnh nhân ${persona.fullName} — ${persona.reason}. Chẩn đoán: ${persona.icd10Name}.`
          );
        }, meta);

        // Backend yêu cầu bệnh án phải được ký số trước khi đóng lượt khám (422 nếu bỏ qua) — ký
        // ngay sau khi ghi xong nội dung bệnh án, trước khi sang CLS/kê đơn/đóng lượt khám.
        await runStep("Bác sĩ ký số bệnh án", page, async () => {
          await doctor.signEmr(page);
        }, meta);

        if (persona.needsCls) {
          await runStep("Kỹ thuật viên nhập kết quả CLS", page, async () => {
            await labtech.enterResults(page, persona, encounterId);
          }, meta);
        }

        await runStep("Bác sĩ kê đơn thuốc", page, async () => {
          await doctor.prescribe(page, encounterId, persona.drugs);
        }, meta);

        let signed = false;
        await runStep("Bác sĩ ký số & gửi ĐTQG", page, async () => {
          signed = await doctor.signPrescription(page);
          if (!signed) {
            throw new Error(
              `SKIP: nút ký đơn bị khoá (nghi do cảnh báo DDI chống chỉ định) cho "${persona.fullName}" — ` +
                `bỏ qua ký, sẽ không cấp phát/thu tiền được đơn này`
            );
          }
        }, meta);

        await runStep("Bác sĩ đóng lượt khám", page, async () => {
          await doctor.closeEncounter(page);
        }, meta);

        if (!signed) {
          continue; // Đơn chưa ký -> dược sĩ không có gì để phát, thu ngân không có gì để thu.
        }

        // ── Dược sĩ: cấp phát ───────────────────────────────────────────────
        await runStep("Dược sĩ đăng nhập", page, async () => {
          await loginAs(page, "duocsi");
        }, meta);

        await runStep("Dược sĩ cấp phát thuốc", page, async () => {
          const ok = await pharmacist.dispense(page, persona);
          if (!ok) {
            throw new Error(`SKIP: không thấy đơn thuốc của "${persona.fullName}" trong hàng chờ phát thuốc`);
          }
        }, meta);

        // ── Thu ngân: thu tiền ──────────────────────────────────────────────
        await runStep("Thu ngân đăng nhập", page, async () => {
          await loginAs(page, "ketoan");
        }, meta);

        await runStep("Thu ngân mở ca (nếu chưa mở)", page, async () => {
          await cashier.openShift(page);
        }, meta);

        await runStep("Thu ngân thu tiền", page, async () => {
          const ok = await cashier.collect(page, persona);
          if (!ok) {
            throw new Error(`SKIP: không thấy hoá đơn chờ thu của "${persona.fullName}"`);
          }
        }, meta);
      }
    });
  }

  test("Lưu báo cáo mô phỏng", async () => {
    const reportPath = path.resolve(__dirname, "..", "..", "test-results", "clinic-sim-report.json");
    saveReport(reportPath);
  });
});
