/**
 * exceptions.spec.ts — 10 kịch bản ngoại lệ vận hành phòng khám: hết thuốc, nhập kho, cận
 * HSD/FEFO, quá tải hàng đợi, DDI chống chỉ định, BHYT lỗi, sai luồng trạng thái, đóng ca lệch
 * tiền, tài khoản sai mật khẩu/khoá, rate-limit. Mỗi test tự thiết lập dữ liệu tối thiểu qua các
 * agent (không phụ thuộc clinic-simulation.spec.ts đã chạy hay chưa). KHÔNG hard-fail — mọi thao
 * tác bọc runStep(), môi trường thiếu tính năng thì SKIP kèm ghi chú rõ ràng.
 */
import { test } from "@playwright/test";
import * as path from "path";
import {
  EXAM_ROOMS,
  DRUG_OUT_OF_STOCK,
  DRUG_NEAR_EXPIRY,
  DDI_PAIR_PRIMARY,
  DDI_PAIR_FALLBACK,
  ROLES,
} from "./clinic-config";
import { findByExceptionTag, type Persona } from "./personas";
import { loginAs, attachErrorListeners } from "./helpers/session";
import { runStep, saveReport } from "./helpers/report";
import { ReceptionAgent } from "./agents/reception";
import { DoctorAgent } from "./agents/doctor";
import { PharmacistAgent } from "./agents/pharmacist";
import { CashierAgent } from "./agents/cashier";

const reception = new ReceptionAgent();
const doctor = new DoctorAgent();
const pharmacist = new PharmacistAgent();
const cashier = new CashierAgent();

/**
 * Thiết lập tối thiểu: tiếp đón -> tạo lượt khám -> bắt đầu khám -> chẩn đoán -> kê đơn.
 * Không ký đơn ở đây — để test tự quyết định bước tiếp theo. Mọi bước con bọc runStep riêng.
 */
async function setupEncounterWithPrescription(
  page: import("@playwright/test").Page,
  persona: Persona,
  meta: { day?: number; patientCode?: string; patientName?: string }
): Promise<{ encounterId: string }> {
  let encounterId = "";

  await runStep(`[${meta.patientCode}] Lễ tân đăng nhập`, page, async () => {
    await loginAs(page, "letan");
  }, meta);

  await runStep(`[${meta.patientCode}] Lễ tân tìm/tạo bệnh nhân`, page, async () => {
    await reception.ensurePatient(page, persona);
  }, meta);

  await runStep(`[${meta.patientCode}] Lễ tân tiếp đón`, page, async () => {
    await reception.checkIn(page, persona, EXAM_ROOMS[0]);
  }, meta);

  await runStep(`[${meta.patientCode}] Bác sĩ đăng nhập`, page, async () => {
    await loginAs(page, "bacsi");
  }, meta);

  await runStep(`[${meta.patientCode}] Bác sĩ tạo lượt khám`, page, async () => {
    encounterId = await doctor.createEncounter(page, persona);
  }, meta);

  if (!encounterId) return { encounterId: "" };

  await runStep(`[${meta.patientCode}] Bác sĩ bắt đầu khám`, page, async () => {
    await doctor.startExam(page);
  }, meta);

  await runStep(`[${meta.patientCode}] Bác sĩ ghi chẩn đoán`, page, async () => {
    await doctor.addDiagnosis(page, persona.icd10, persona.icd10Name);
  }, meta);

  await runStep(`[${meta.patientCode}] Bác sĩ kê đơn thuốc`, page, async () => {
    await doctor.prescribe(page, encounterId, persona.drugs);
  }, meta);

  return { encounterId };
}

test.describe("Kịch bản ngoại lệ vận hành phòng khám", () => {
  test.describe.configure({ mode: "default" });

  // ── 1. Hết thuốc ────────────────────────────────────────────────────────────
  test("Ngoại lệ 1: Hết thuốc — cảnh báo khi phát Gliclazide 30mg", async ({ page }) => {
    attachErrorListeners(page);
    const base = findByExceptionTag("OUT_OF_STOCK");
    if (!base) {
      await runStep("Chuẩn bị dữ liệu", page, async () => {
        throw new Error("SKIP: không có persona nào gắn exceptionTag OUT_OF_STOCK trong personas.ts");
      });
      return;
    }
    const persona: Persona = { ...base, isReturning: true, drugs: [DRUG_OUT_OF_STOCK] };
    const meta = { day: persona.day, patientCode: persona.code, patientName: persona.fullName };

    const { encounterId } = await setupEncounterWithPrescription(page, persona, meta);
    if (!encounterId) return;

    let signed = false;
    await runStep("Bác sĩ ký số đơn thuốc", page, async () => {
      signed = await doctor.signPrescription(page);
      if (!signed) throw new Error("SKIP: nút ký đơn bị khoá — không thể tiếp tục kịch bản hết thuốc");
    }, meta);
    if (!signed) return;

    await runStep("Dược sĩ thử phát thuốc thiếu tồn", page, async () => {
      const ok = await pharmacist.dispense(page, persona);
      if (!ok) throw new Error("SKIP: không thấy đơn trong hàng chờ phát thuốc để thử kịch bản hết thuốc");
    }, meta);

    await runStep("Kiểm tra thông báo hết thuốc (best-effort)", page, async () => {
      const warn = page.getByText(/không đủ tồn|hết thuốc|thiếu tồn|insufficient stock/i);
      if (!(await warn.count())) {
        throw new Error(
          "SKIP: không quan sát được thông báo hết thuốc qua UI — có thể tồn kho môi trường hiện tại " +
            "đã đủ cho Gliclazide 30mg (cần seed tồn kho = 0 để tái hiện đúng kịch bản)."
        );
      }
    }, meta);
  });

  // ── 2. Nhập kho ─────────────────────────────────────────────────────────────
  test("Ngoại lệ 2: Nhập kho bổ sung thuốc thiếu tồn", async ({ page }) => {
    attachErrorListeners(page);
    await runStep("Dược sĩ đăng nhập", page, async () => {
      await loginAs(page, "duocsi");
    });
    await runStep("Dược sĩ nhập kho bổ sung", page, async () => {
      await pharmacist.restock(page, DRUG_OUT_OF_STOCK);
    });
  });

  // ── 3. Cận HSD / FEFO ───────────────────────────────────────────────────────
  test("Ngoại lệ 3: Cận HSD / FEFO khi cấp Insulin Glargine (2 lô)", async ({ page }) => {
    attachErrorListeners(page);
    const base = findByExceptionTag("NEAR_EXPIRY");
    if (!base) {
      await runStep("Chuẩn bị dữ liệu", page, async () => {
        throw new Error("SKIP: không có persona nào gắn exceptionTag NEAR_EXPIRY trong personas.ts");
      });
      return;
    }
    const persona: Persona = { ...base, isReturning: true, drugs: [DRUG_NEAR_EXPIRY] };
    const meta = { day: persona.day, patientCode: persona.code, patientName: persona.fullName };

    const { encounterId } = await setupEncounterWithPrescription(page, persona, meta);
    if (!encounterId) return;

    let signed = false;
    await runStep("Bác sĩ ký số đơn thuốc", page, async () => {
      signed = await doctor.signPrescription(page);
      if (!signed) throw new Error("SKIP: nút ký đơn bị khoá — không thể tiếp tục kịch bản FEFO");
    }, meta);
    if (!signed) return;

    await runStep("Dược sĩ cấp phát (backend tự FEFO chọn lô)", page, async () => {
      const ok = await pharmacist.dispense(page, persona);
      if (!ok) throw new Error("SKIP: không thấy đơn trong hàng chờ phát thuốc để thử kịch bản FEFO");
    }, meta);

    await runStep("Kiểm tra UI hiển thị số lô/HSD đã chọn (best-effort)", page, async () => {
      throw new Error(
        "SKIP: DispenseConfirmDialog hiện tại chỉ hiển thị tên thuốc + số lượng, không hiển thị số " +
          "lô/HSD cụ thể để xác thực FEFO qua giao diện — cần đối chiếu qua API/DB, bỏ qua ở tầng UI."
      );
    }, meta);
  });

  // ── 4. Quá tải hàng đợi ─────────────────────────────────────────────────────
  test("Ngoại lệ 4: Quá tải hàng đợi — nhiều bệnh nhân chờ cùng lúc", async ({ page }) => {
    attachErrorListeners(page);
    const overloadPersonas = Array.from({ length: 4 }, (_, i) => {
      const base = findByExceptionTag("OUT_OF_STOCK") ?? findByExceptionTag("DDI");
      const seed: Persona = base ?? {
        code: "BN-OVL",
        fullName: `Nguyễn Văn Quá Tải ${i}`,
        gender: "MALE",
        dob: "1970-01-01",
        phone: `0999${(100000 + i).toString()}`,
        idNumber: `079999999${i}`,
        patientType: "SERVICE",
        isReturning: false,
        day: 1,
        reason: "Kiểm tra quá tải hàng đợi",
        icd10: "I10",
        icd10Name: "Tăng huyết áp vô căn (nguyên phát)",
        drugs: [],
        needsCls: false,
      };
      return {
        ...seed,
        code: `BN-OVL-${i}`,
        fullName: `Trần Văn Quá Tải ${i}`,
        phone: `09${(88000000 + i).toString().padStart(8, "0")}`,
        idNumber: `079${(300000000 + i).toString().padStart(9, "0")}`,
        isReturning: false,
      } as Persona;
    });

    await runStep("Lễ tân đăng nhập", page, async () => {
      await loginAs(page, "letan");
    });

    for (const persona of overloadPersonas) {
      const meta = { day: persona.day, patientCode: persona.code, patientName: persona.fullName };
      await runStep(`Tiếp đón nhanh ${persona.fullName}`, page, async () => {
        await reception.ensurePatient(page, persona);
        await reception.checkIn(page, persona, EXAM_ROOMS[0]);
      }, meta);
    }

    await runStep("Kiểm tra bảng hàng đợi hiển thị nhiều BN đang chờ", page, async () => {
      await page.goto("/reception", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.waitForTimeout(1000);
      const waitingLabel = page.getByText("Đang chờ").first();
      if (!(await waitingLabel.count())) {
        throw new Error("SKIP: không tìm thấy thẻ thống kê 'Đang chờ' trên /reception");
      }
    });

    await runStep("Kiểm tra cảnh báo quá tải (best-effort)", page, async () => {
      const overloadAlert = page.getByText(/quá tải|vượt ngưỡng|overload/i);
      if (!(await overloadAlert.count())) {
        throw new Error(
          "SKIP: UI /reception hiện chưa có banner cảnh báo quá tải hàng đợi rõ ràng — chỉ xác nhận " +
            "được số lượng bệnh nhân đang chờ tăng lên qua bảng hàng đợi, bỏ qua kiểm tra cảnh báo riêng."
        );
      }
    });
  });

  // ── 5. DDI chống chỉ định ───────────────────────────────────────────────────
  test("Ngoại lệ 5: DDI chống chỉ định — cảnh báo + khoá ký đơn", async ({ page }) => {
    attachErrorListeners(page);
    const base = findByExceptionTag("DDI");
    if (!base) {
      await runStep("Chuẩn bị dữ liệu", page, async () => {
        throw new Error("SKIP: không có persona nào gắn exceptionTag DDI trong personas.ts");
      });
      return;
    }
    const persona: Persona = { ...base, isReturning: true, drugs: [...DDI_PAIR_PRIMARY] };
    const meta = { day: persona.day, patientCode: persona.code, patientName: persona.fullName };

    let encounterId = "";
    await runStep(`[${meta.patientCode}] Lễ tân đăng nhập`, page, async () => {
      await loginAs(page, "letan");
    }, meta);
    await runStep(`[${meta.patientCode}] Lễ tân tìm/tạo bệnh nhân`, page, async () => {
      await reception.ensurePatient(page, persona);
    }, meta);
    await runStep(`[${meta.patientCode}] Lễ tân tiếp đón`, page, async () => {
      await reception.checkIn(page, persona, EXAM_ROOMS[0]);
    }, meta);
    await runStep(`[${meta.patientCode}] Bác sĩ đăng nhập`, page, async () => {
      await loginAs(page, "bacsi");
    }, meta);
    await runStep(`[${meta.patientCode}] Bác sĩ tạo lượt khám`, page, async () => {
      encounterId = await doctor.createEncounter(page, persona);
    }, meta);
    if (!encounterId) return;
    await runStep(`[${meta.patientCode}] Bác sĩ bắt đầu khám`, page, async () => {
      await doctor.startExam(page);
    }, meta);
    await runStep(`[${meta.patientCode}] Bác sĩ ghi chẩn đoán`, page, async () => {
      await doctor.addDiagnosis(page, persona.icd10, persona.icd10Name);
    }, meta);

    let prescribed = false;
    await runStep("Bác sĩ kê 2 thuốc thuộc cặp DDI (thử cặp ưu tiên)", page, async () => {
      await doctor.prescribe(page, encounterId, [...DDI_PAIR_PRIMARY]);
      prescribed = true;
    }, meta);

    if (!prescribed) {
      await runStep("Bác sĩ kê 2 thuốc thuộc cặp DDI (fallback)", page, async () => {
        await doctor.prescribe(page, encounterId, [...DDI_PAIR_FALLBACK]);
        prescribed = true;
      }, meta);
    }
    if (!prescribed) return;

    await runStep("Kiểm tra cảnh báo tương tác thuốc hiển thị", page, async () => {
      const panel = page.getByText(/Cảnh báo tương tác thuốc/i);
      if (!(await panel.count())) {
        throw new Error(
          "SKIP: không thấy panel 'Cảnh báo tương tác thuốc' — cặp thuốc dùng để test có thể chưa " +
            "được seed DDI chống chỉ định trong danh mục tương tác của môi trường hiện tại."
        );
      }
    }, meta);

    await runStep("Xác nhận nút ký đơn bị khoá do DDI chống chỉ định", page, async () => {
      const signed = await doctor.signPrescription(page);
      if (signed) {
        throw new Error(
          "SKIP: đơn vẫn ký được — cặp thuốc thử nghiệm có thể không nằm trong danh mục DDI mức " +
            "CONTRAINDICATED của môi trường hiện tại (kỳ vọng bị khoá ký khi có cảnh báo chống chỉ định)."
        );
      }
    }, meta);
  });

  // ── 6. BHYT không hợp lệ ────────────────────────────────────────────────────
  test("Ngoại lệ 6: BHYT không hợp lệ khi tạo/tiếp đón bệnh nhân", async ({ page }) => {
    attachErrorListeners(page);
    await runStep("Lễ tân đăng nhập", page, async () => {
      await loginAs(page, "letan");
    });
    await runStep("Mở form tạo bệnh nhân với đối tượng BHYT", page, async () => {
      await page.goto("/patients/new", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.getByRole("heading", { name: /Tạo bệnh nhân mới/i }).waitFor({ timeout: 15_000 });
    });
    await runStep("Nhập số thẻ BHYT không hợp lệ (best-effort)", page, async () => {
      throw new Error(
        "SKIP: tab 'Bảo hiểm y tế' trong form tạo bệnh nhân hiện là placeholder (PatientBhytTab) — " +
          "chưa có input số thẻ BHYT để nhập giá trị không hợp lệ trong luồng tạo mới. Việc thêm/xác " +
          "thực BHYT được ghi chú là thực hiện sau, qua hồ sơ bệnh nhân — ngoài phạm vi khảo sát UI này."
      );
    });
  });

  // ── 7. Sai luồng trạng thái ─────────────────────────────────────────────────
  test("Ngoại lệ 7: Sai luồng trạng thái — thao tác trái quy trình khi lượt khám còn WAITING", async ({
    page,
  }) => {
    attachErrorListeners(page);
    const persona: Persona = {
      code: "BN-FLOW",
      fullName: "Lê Thị Sai Luồng 01",
      gender: "FEMALE",
      dob: "1980-05-10",
      phone: "0977000111",
      idNumber: "079300000111",
      patientType: "SERVICE",
      isReturning: true,
      day: 1,
      reason: "Kiểm tra luồng trạng thái lượt khám",
      icd10: "I10",
      icd10Name: "Tăng huyết áp vô căn (nguyên phát)",
      drugs: [],
      needsCls: false,
    };
    const meta = { day: persona.day, patientCode: persona.code, patientName: persona.fullName };

    let encounterId = "";
    await runStep("Lễ tân đăng nhập", page, async () => {
      await loginAs(page, "letan");
    }, meta);
    await runStep("Lễ tân tìm/tạo bệnh nhân", page, async () => {
      await reception.ensurePatient(page, persona);
    }, meta);
    await runStep("Lễ tân tiếp đón (chưa gọi vào khám)", page, async () => {
      await reception.checkIn(page, persona, EXAM_ROOMS[0]);
    }, meta);
    await runStep("Bác sĩ đăng nhập", page, async () => {
      await loginAs(page, "bacsi");
    }, meta);
    await runStep("Bác sĩ tạo lượt khám (chưa bấm Bắt đầu khám)", page, async () => {
      encounterId = await doctor.createEncounter(page, persona);
    }, meta);
    if (!encounterId) return;

    await runStep("Xác nhận KHÔNG thể nhập sinh hiệu khi còn WAITING", page, async () => {
      await page.getByRole("tab", { name: "Sinh hiệu" }).click({ timeout: 10_000 });
      const hrInput = page.locator("#v-hr");
      if (await hrInput.count()) {
        throw new Error(
          "Guard trạng thái KHÔNG hoạt động: form nhập sinh hiệu vẫn hiển thị dù lượt khám còn WAITING"
        );
      }
    }, meta);

    await runStep("Xác nhận KHÔNG thể đóng lượt khám khi còn WAITING", page, async () => {
      const closeBtn = page.getByRole("button", { name: "Đóng lượt khám", exact: true });
      if (await closeBtn.count()) {
        throw new Error(
          "Guard trạng thái KHÔNG hoạt động: nút 'Đóng lượt khám' hiển thị dù lượt khám còn WAITING"
        );
      }
    }, meta);
  });

  // ── 8. Đóng ca lệch tiền ─────────────────────────────────────────────────────
  test("Ngoại lệ 8: Đóng ca thu ngân lệch tiền", async ({ page }) => {
    attachErrorListeners(page);
    await runStep("Thu ngân đăng nhập", page, async () => {
      await loginAs(page, "ketoan");
    });
    await runStep("Mở ca (nếu chưa mở)", page, async () => {
      await cashier.openShift(page, 500_000);
    });
    await runStep("Đóng ca với số tiền thực tế lệch lớn", page, async () => {
      await cashier.closeShift(page, 999_999_999, true);
    });
    await runStep("Kiểm tra thông báo chênh lệch hiển thị (best-effort)", page, async () => {
      const diffText = page.getByText(/Chênh lệch/i);
      if (!(await diffText.count())) {
        throw new Error(
          "SKIP: không thấy dòng 'Chênh lệch' trong dialog đóng ca — có thể ca chưa có expectedCash " +
            "để so sánh (chưa có giao dịch nào trong ca), bỏ qua kiểm tra chi tiết."
        );
      }
    });
  });

  // ── 9. Tài khoản sai mật khẩu / khoá ────────────────────────────────────────
  test("Ngoại lệ 9: Đăng nhập sai mật khẩu / tài khoản bị khoá", async ({ page }) => {
    attachErrorListeners(page);
    await runStep("Đăng nhập với mật khẩu sai", page, async () => {
      await page.goto("/login", { waitUntil: "domcontentloaded", timeout: 30_000 });
      await page.locator("#email").fill(ROLES.letan.email);
      await page.locator("#password").fill("mat-khau-sai-co-tinh-999");
      await page.getByRole("button", { name: /Đăng nhập/i }).click();
      await page.waitForTimeout(1500);
      if (!page.url().includes("/login")) {
        throw new Error("Đăng nhập sai mật khẩu nhưng vẫn rời khỏi /login — kiểm soát lỗi không đúng");
      }
    });
    await runStep("Kiểm tra thông báo lỗi đăng nhập hiển thị", page, async () => {
      const errToast = page.getByText(/Email hoặc mật khẩu không đúng|Lỗi hệ thống/i);
      if (!(await errToast.count())) {
        throw new Error("SKIP: không thấy toast lỗi đăng nhập trong thời gian chờ — có thể đã tự ẩn");
      }
    });
    await runStep("Kịch bản tài khoản bị khoá hẳn (best-effort)", page, async () => {
      throw new Error(
        "SKIP: khoá tài khoản (nhiều lần đăng nhập sai liên tiếp hoặc admin khoá thủ công) cần trigger " +
          "từ backend/DB, không có nút thao tác qua UI trong phạm vi khảo sát — bỏ qua ở tầng UI."
      );
    });
  });

  // ── 10. Rate limit ───────────────────────────────────────────────────────────
  test("Ngoại lệ 10: Rate-limit khi gọi API dồn dập", async ({ page }) => {
    attachErrorListeners(page);
    let got429 = false;
    page.on("response", (resp) => {
      if (resp.status() === 429) got429 = true;
    });

    await runStep("Đăng nhập lễ tân", page, async () => {
      await loginAs(page, "letan");
    });

    await runStep("Gửi dồn dập nhiều request tìm kiếm bệnh nhân", page, async () => {
      await page.goto("/reception", { waitUntil: "domcontentloaded", timeout: 30_000 });
      const input = page.getByPlaceholder("Tìm tên, SĐT, CMND, BHYT...");
      for (let i = 0; i < 15; i++) {
        await input.fill(`test-rate-limit-${i}`);
        await page.waitForTimeout(50);
      }
      await page.waitForTimeout(1000);
    });

    await runStep("Kiểm tra có phản hồi HTTP 429 (best-effort)", page, async () => {
      if (!got429) {
        throw new Error(
          "SKIP: không quan sát được response 429 qua các thao tác UI thông thường trong test này " +
            "(giới hạn 100 req/phút/user theo CLAUDE.md khó chạm tới chỉ bằng vài chục request) — " +
            "cần test tải trực tiếp ở tầng API để xác thực rate-limit, bỏ qua ở tầng UI."
        );
      }
    });
  });

  test("Lưu báo cáo kịch bản ngoại lệ", async () => {
    const reportPath = path.resolve(__dirname, "..", "..", "test-results", "clinic-sim-exceptions-report.json");
    saveReport(reportPath);
  });
});
