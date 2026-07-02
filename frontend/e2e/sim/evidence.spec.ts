/**
 * evidence.spec.ts — Chụp ảnh bằng chứng TỪNG BƯỚC (lúc THÀNH CÔNG) cho 1 ca khám đầy đủ, chạy
 * qua UI thật bằng các agent có sẵn (ReceptionAgent/DoctorAgent). Khác với clinic-simulation.spec.ts
 * (dùng runStep() chỉ chụp ảnh khi FAIL/SKIP), spec này LUÔN chụp ảnh fullPage sau mỗi bước — kể cả
 * khi bước lỗi (để vẫn thấy trạng thái tại thời điểm lỗi) — nhằm tạo bộ evidence trực quan chứng
 * minh luồng nghiệp vụ chạy thật, không phải mock.
 *
 * Luồng: Tiếp đón -> Gọi số -> Tạo lượt khám -> Bắt đầu khám -> Sinh hiệu -> Chẩn đoán -> Bệnh án
 * -> Ký bệnh án -> Kê đơn thuốc -> Ký & gửi ĐTQG -> Đóng lượt khám.
 * (Cấp phát/thu tiền KHÔNG nằm trong phạm vi evidence này — còn vướng bug Dispensing INT<->GUID.)
 *
 * Persona dùng riêng, tên DUY NHẤT theo từng lần chạy (hậu tố timestamp+random) để tránh đụng
 * "đã tiếp đón hôm nay" hoặc trùng dữ liệu giữa các lần chạy lặp lại.
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

const reception = new ReceptionAgent();
const doctor = new DoctorAgent();

// Thư mục evidence — đường dẫn tuyệt đối tới docs/test/evidence-shots/ ở repo root.
// __dirname = frontend/e2e/sim -> lên 3 cấp (sim -> e2e -> frontend) tới repo root.
const SHOTS_DIR = path.resolve(__dirname, "..", "..", "..", "docs", "test", "evidence-shots");

/** Sinh persona MỚI mỗi lần chạy — tên/SĐT/CMND duy nhất để không đụng dữ liệu lần chạy trước. */
function buildEvidencePersona(): Persona {
  const now = Date.now();
  const rand = Math.floor(Math.random() * 900) + 100; // 3 chữ số ngẫu nhiên
  const suffix = `${now}${rand}`.slice(-9); // 9 ký tự cuối, đủ để tránh trùng giữa các lần chạy
  const phone = `09${suffix.padStart(8, "0").slice(0, 8)}`;
  const idNumber = `079${suffix.padStart(9, "0")}`;

  return {
    code: `EVD-${suffix}`,
    fullName: `BN Evidence ${suffix}`,
    gender: "FEMALE",
    dob: "1975-05-20",
    phone,
    idNumber,
    patientType: "BHYT",
    isReturning: false,
    day: 1,
    reason: "Kiểm tra đường huyết định kỳ",
    icd10: "E11.9",
    icd10Name: "Đái tháo đường típ 2 không biến chứng",
    drugs: ["Metformin 500mg"],
    needsCls: false,
  };
}

/**
 * Chờ trang "ổn định" trước khi chụp ảnh — best-effort, KHÔNG throw (dù chờ không như kỳ vọng vẫn
 * phải chụp, tốt hơn không chụp). Kết hợp 3 lớp phòng thủ để tránh lặp lại bug đã gặp (ảnh
 * "06-tao-luot-kham.png" chụp đúng lúc trang chi tiết lượt khám còn đang Skeleton loading vì
 * doctor.createEncounter() trả về ngay sau khi URL đổi, chưa chờ dữ liệu trang đích load xong):
 *   1) waitForLoadState("networkidle") — hết các request nền đang chạy.
 *   2) waitReady() (nếu truyền vào) — chờ đúng 1 phần tử ĐẶC TRƯNG cho trạng thái UI mong đợi của
 *      bước đó (vd tab "Khám bệnh", badge trạng thái, dòng dữ liệu vừa lưu...).
 *   3) Chờ KHÔNG còn phần tử nào có data-slot="skeleton" (mọi shadcn <Skeleton> đều gắn attribute
 *      này — xem components/ui/skeleton.tsx) — lưới an toàn chung cho mọi trang, phòng trường hợp
 *      waitReady() không bao quát hết các mảng UI khác vẫn còn loading.
 * Cuối cùng chờ thêm 800ms cho hiệu ứng chuyển đổi/animation (fade, toast) ổn định hình ảnh.
 */
async function waitPageSettled(page: Page, waitReady?: () => Promise<unknown>): Promise<void> {
  await page.waitForLoadState("networkidle", { timeout: 10_000 }).catch(() => {});
  if (waitReady) {
    await waitReady().catch(() => {});
  }
  await page
    .waitForFunction(() => document.querySelectorAll('[data-slot="skeleton"]').length === 0, undefined, {
      timeout: 8_000,
    })
    .catch(() => {});
  await page.waitForTimeout(800);
}

/**
 * Chạy 1 bước, LUÔN chụp ảnh fullPage sau khi kết thúc (dù thành công hay lỗi) — không dùng
 * runStep() của helpers/report.ts vì hàm đó chỉ chụp khi FAIL/SKIP. Lỗi được bắt tại đây và
 * KHÔNG ném ra ngoài để các bước sau vẫn tiếp tục chạy, tối đa hoá số ảnh evidence thu được.
 * `waitReady` (tuỳ chọn): callback chờ phần tử đặc trưng cho trạng thái THẬT của bước này trước
 * khi chụp — xem waitPageSettled() ở trên.
 */
async function step(
  page: Page,
  fileName: string,
  label: string,
  fn: () => Promise<void>,
  waitReady?: () => Promise<unknown>
): Promise<void> {
  let errorMsg: string | undefined;
  try {
    await fn();
    console.log(`[evidence] THANH CONG — ${label}`);
  } catch (e) {
    errorMsg = e instanceof Error ? e.message : String(e);
    console.log(`[evidence] LOI (van chup anh trang thai hien tai) — ${label} :: ${errorMsg}`);
  }
  // Luôn chờ trang render ổn định (hết skeleton, hết network) trước khi chụp — kể cả khi bước lỗi,
  // để ảnh phản ánh đúng trạng thái THẬT tại thời điểm đó, không phải lúc còn đang loading.
  // BỌC try/catch: nếu page/context/browser đã bị đóng đột ngột giữa chừng (crash trình duyệt/dev
  // server), waitForTimeout() bên trong waitPageSettled() SẼ throw dù các bước con trước đó đã có
  // .catch() riêng — không bọc ở đây thì lỗi này thoát khỏi step() không bị bắt, làm SẬP toàn bộ
  // test ngay lập tức thay vì tiếp tục cố chụp các bước còn lại.
  try {
    await waitPageSettled(page, waitReady);
  } catch (settleErr) {
    console.log(`[evidence] Loi khi cho trang on dinh truoc khi chup "${label}": ${settleErr}`);
  }
  try {
    const filePath = path.join(SHOTS_DIR, fileName);
    await page.screenshot({ path: filePath, fullPage: true, timeout: 15_000 });
    console.log(`[evidence] Da chup: ${fileName}`);
  } catch (shotErr) {
    console.log(`[evidence] KHONG chup duoc anh ${fileName}: ${shotErr}`);
  }
  if (errorMsg) {
    console.log(`[evidence] Buoc "${label}" that bai nhung spec tiep tuc chay buoc ke tiep.`);
  }
}

test.describe("Evidence: ca kham day du (tiep don -> ke don -> ky ĐTQG -> dong luot kham)", () => {
  test("Chup anh tung buoc thanh cong cho 1 ca kham day du", async ({ page }) => {
    fs.mkdirSync(SHOTS_DIR, { recursive: true });
    attachErrorListeners(page);

    const persona = buildEvidencePersona();
    const room = EXAM_ROOMS[0];
    console.log(`[evidence] Persona: ${persona.fullName} | SĐT ${persona.phone} | CCCD ${persona.idNumber}`);

    let encounterId = "";

    // ── Lễ tân: đăng nhập -> mở trang tiếp đón -> chọn/tạo BN -> tiếp đón -> gọi số ────────────
    await step(
      page,
      "01-dang-nhap-le-tan.png",
      "Đăng nhập lễ tân",
      async () => {
        await loginAs(page, "letan");
      },
      // Topbar (AppTopbar/UserMenu) chỉ render menu tài khoản sau khi auth-store có user thật —
      // dấu hiệu đáng tin cậy rằng đã đăng nhập xong, không còn ở form /login.
      () => page.getByLabel("Menu tài khoản").waitFor({ state: "visible", timeout: 10_000 })
    );

    await step(
      page,
      "02-trang-tiep-don.png",
      "Mở trang tiếp đón",
      async () => {
        if (!page.url().includes("/reception")) {
          await page.goto("/reception", { waitUntil: "domcontentloaded", timeout: 30_000 });
        }
        await page.getByRole("heading", { level: 2, name: "Tiếp đón bệnh nhân" }).waitFor({ timeout: 15_000 });
      },
      // Danh sách phòng khám (useRooms) load xong — tránh chụp lúc form check-in còn rỗng phòng.
      () => page.locator("label", { hasText: room }).first().waitFor({ state: "visible", timeout: 10_000 })
    );

    await step(
      page,
      "03-chon-benh-nhan.png",
      "Tìm/tạo & chọn bệnh nhân trong form tiếp đón",
      async () => {
        await reception.ensurePatient(page, persona);
      },
      // Ô "bệnh nhân đã chọn" (ReceptionCheckInForm) hiển thị tên BN sau khi selectPatient() chạy.
      () => page.getByText(persona.fullName, { exact: false }).first().waitFor({ state: "visible", timeout: 8_000 })
    );

    await step(
      page,
      "04-tiep-don-xong.png",
      `Tiếp đón vào ${room}`,
      async () => {
        await reception.checkIn(page, persona, room);
      },
      // Sau checkIn(), form tự reset (mất tên BN khỏi form) nhưng vé mới xuất hiện trên
      // ReceptionQueueBoard với tên BN — cùng 1 locator vẫn đúng nhờ vị trí xuất hiện đã đổi.
      () => page.getByText(persona.fullName, { exact: false }).first().waitFor({ state: "visible", timeout: 10_000 })
    );

    await step(
      page,
      "05-goi-so.png",
      `Gọi bệnh nhân vào ${room}`,
      async () => {
        await reception.callNext(page, room);
      },
      // TicketCard chuyển badge "Chờ" -> "Đã gọi" sau khi gọi số thành công.
      () => page.getByText("Đã gọi", { exact: true }).first().waitFor({ state: "visible", timeout: 8_000 })
    );

    // ── Bác sĩ: đăng nhập -> khám -> kê đơn -> ký ĐTQG -> đóng lượt khám ────────────────────────
    // Lưu ý: với SIM_USE_ADMIN=1 (khuyến nghị khi chạy spec này), loginAs() LUÔN dùng tài khoản
    // admin bất kể roleKey truyền vào — gọi "bacsi" ở đây để giữ đúng ngữ nghĩa nghiệp vụ (chuyển
    // vai bác sĩ) và vẫn tương thích nếu sau này chạy không bật admin-bypass.
    await loginAs(page, "bacsi").catch((e) => {
      console.log(`[evidence] Dang nhap bac si that bai (khong chup anh rieng buoc nay): ${e}`);
    });

    await step(
      page,
      "06-tao-luot-kham.png",
      "Tạo lượt khám",
      async () => {
        encounterId = await doctor.createEncounter(page, persona);
      },
      // QUAN TRỌNG (fix bug ảnh cũ chụp lúc Skeleton): createEncounter() trả về NGAY sau khi URL
      // đổi sang /encounters/{id}, nhưng trang đích còn đang isLoading (EncounterDetailClient render
      // toàn bộ Skeleton, KHÔNG có Tabs) cho tới khi GET /encounters/{id} xong. Tab "Khám bệnh" chỉ
      // tồn tại trong DOM ở nhánh UI thật (không phải Skeleton) -> chờ nó xuất hiện là điều kiện
      // chính xác để biết trang đã render nội dung thật.
      () => page.getByRole("tab", { name: "Khám bệnh" }).waitFor({ state: "visible", timeout: 15_000 })
    );

    await step(
      page,
      "07-bat-dau-kham.png",
      "Bắt đầu khám (IN_PROGRESS)",
      async () => {
        await doctor.startExam(page);
      },
      // EncounterStatusBadge đổi nhãn "Chờ khám" -> "Đang khám" khi status chuyển IN_PROGRESS.
      () => page.getByText("Đang khám", { exact: true }).first().waitFor({ state: "visible", timeout: 8_000 })
    );

    await step(
      page,
      "08-sinh-hieu.png",
      "Ghi sinh hiệu",
      async () => {
        await doctor.recordVitals(page);
      },
      // Giá trị nhiệt độ mặc định của DoctorAgent.recordVitals() (DEFAULT_VITALS.temperature=36.8)
      // xuất hiện lại ở cả widget "Sinh hiệu gần nhất" lẫn bảng lịch sử sau khi lưu + refetch xong.
      () => page.getByText("36.8°C").first().waitFor({ state: "visible", timeout: 8_000 })
    );

    await step(
      page,
      "09-chan-doan.png",
      `Ghi chẩn đoán ${persona.icd10}`,
      async () => {
        await doctor.addDiagnosis(page, persona.icd10, persona.icd10Name);
      },
      // Badge mã ICD-10 (chính xác = icd10_code gửi lên, không phụ thuộc tên bệnh backend trả về)
      // xuất hiện trong "Danh sách chẩn đoán" sau khi lưu thành công.
      () => page.getByText(persona.icd10, { exact: true }).first().waitFor({ state: "visible", timeout: 8_000 })
    );

    await step(
      page,
      "10-benh-an.png",
      "Viết bệnh án (EMR)",
      async () => {
        await doctor.writeEmr(
          page,
          `Bệnh nhân ${persona.fullName} — ${persona.reason}. Chẩn đoán: ${persona.icd10Name}.`
        );
      },
      // EmrEditor gọi onSaved() sau khi PUT /emr thành công -> hiện "Đã lưu lúc HH:mm".
      () => page.getByText(/Đã lưu lúc/).first().waitFor({ state: "visible", timeout: 8_000 })
    );

    await step(
      page,
      "11-ky-benh-an.png",
      "Ký số bệnh án điện tử",
      async () => {
        await doctor.signEmr(page);
      },
      // Nút đổi nhãn "Ký số bệnh án" -> "Đã ký số BA" (disabled) khi isSigned=true.
      () => page.getByRole("button", { name: "Đã ký số BA", exact: true }).waitFor({ state: "visible", timeout: 8_000 })
    );

    // Kê đơn TRƯỚC khi đóng lượt khám (đơn phải gắn vào lượt khám đang mở), rồi ký & gửi ĐTQG,
    // cuối cùng mới đóng lượt khám — đúng thứ tự nghiệp vụ backend yêu cầu.
    await step(
      page,
      "12-ke-don-thuoc.png",
      "Kê đơn thuốc",
      async () => {
        if (!encounterId) throw new Error("Không có encounterId — bỏ qua kê đơn");
        await doctor.prescribe(page, encounterId, persona.drugs);
      },
      // Nút "Ký số & gửi ĐTQG" (canSign) chỉ render khi đơn có >=1 dòng thuốc đã lưu thành công.
      () => page.getByRole("button", { name: /Ký số & gửi ĐTQG/i }).waitFor({ state: "visible", timeout: 8_000 })
    );

    // ── Bước 13 — Ký số & gửi ĐTQG: BẮT response THẬT của POST .../dtqg/submit (không chỉ dựa vào
    // hành vi UI) để biết chính xác ĐTQG (mock) trả gì, log lại nguyên văn cho Lành điều tra nếu lỗi.
    // Lưu ý: waitForResponse() phải được set up TRƯỚC khi trigger action (đúng pattern Playwright) —
    // ở đây gọi trước doctor.signPrescription() vì hàm đó tự bấm hết wizard 3 bước bên trong.
    await step(
      page,
      "13-ky-va-gui-dtqg.png",
      "Ký số & gửi ĐTQG đơn thuốc",
      async () => {
        const dtqgRespPromise = page
          .waitForResponse(
            (r) => r.url().includes("/dtqg/submit") && r.request().method() === "POST",
            { timeout: 20_000 }
          )
          .catch(() => null);

        const signed = await doctor.signPrescription(page);
        if (!signed) throw new Error("Nút ký đơn bị khoá (nghi do cảnh báo DDI) — không ký được");

        const dtqgResp = await dtqgRespPromise;
        if (dtqgResp) {
          const status = dtqgResp.status();
          let bodyDump = "";
          try {
            bodyDump = JSON.stringify(await dtqgResp.json());
          } catch {
            try {
              bodyDump = await dtqgResp.text();
            } catch {
              bodyDump = "(khong doc duoc body)";
            }
          }
          console.log(`[evidence][DTQG] POST ${dtqgResp.url()} -> HTTP ${status}`);
          console.log(`[evidence][DTQG] Body: ${bodyDump}`);
          if (status >= 400) {
            console.log(
              `[evidence][DTQG] CANH BAO: gui DTQG THAT BAI (HTTP ${status}). Van chup anh trang thai that ` +
                `(khong che giau) — xem toast/badge trong 13-ky-va-gui-dtqg.png.`
            );
          }
        } else {
          console.log(
            "[evidence][DTQG] KHONG bat duoc response POST .../dtqg/submit trong 20s " +
              "(request co the khong duoc goi, hoac URL/method khac ky vong)."
          );
        }
      },
      // Dialog wizard (SignPrescriptionWizard) đóng lại (nút "Đóng" đã được doctor.signPrescription
      // bấm) rồi badge trạng thái đơn cập nhật thành "Đã ký" (SIGNED) hoặc "Đã gửi ĐTQG"
      // (SUBMITTED_DTQG) tuỳ ĐTQG thành công hay thất bại — chờ 1 trong 2, không giả định kết quả.
      async () => {
        await page.getByRole("dialog").waitFor({ state: "hidden", timeout: 8_000 }).catch(() => {});
        await page.getByText(/Đã ký|Đã gửi ĐTQG/).first().waitFor({ state: "visible", timeout: 8_000 });
      }
    );

    await step(
      page,
      "14-dong-luot-kham.png",
      "Đóng lượt khám (DONE)",
      async () => {
        await doctor.closeEncounter(page);
      },
      // Alert xanh "Lượt khám hoàn thành" chỉ hiện khi status = DONE (isDone).
      () => page.getByText("Lượt khám hoàn thành", { exact: false }).first().waitFor({ state: "visible", timeout: 8_000 })
    );

    console.log(`[evidence] Hoan tat. Anh luu tai: ${SHOTS_DIR}`);
  });
});
