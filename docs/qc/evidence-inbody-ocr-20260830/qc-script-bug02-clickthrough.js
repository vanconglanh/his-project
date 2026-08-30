// QC BUG-02: click that nut "Xem file goc" tren tab Lich su InBody (/patients/[id])
const fs = require("fs");
const ROOT = "D:/_Project/08.ATDS/02.Onetech/202501_CV10/_project/git/screen/_git/atds/his-project";
const lib = require(ROOT + "/frontend/e2e/qc-lib.js");
const EVID = ROOT + "/docs/qc/evidence-inbody-ocr-20260830";
const PATIENT = "f0000000-0000-0000-0000-000000000008";
const log = []; const L = s => { console.log(s); log.push(s); };

(async () => {
  const { browser, ctx, page } = await lib.newSession();
  const errs = []; page.on("pageerror", e => errs.push(e.message));
  // Ghi lai MOI request browser gui di (khong chi /api/) de soi Network that
  const allReq = [];
  ctx.on("request", r => allReq.push(r.method() + " " + r.url()));

  await lib.quickLogin(page, "Bác sĩ");
  L("B1 dang nhap OK, url = " + page.url());

  await page.goto(lib.BASE + "/patients/" + PATIENT, { waitUntil: "domcontentloaded" });
  await page.waitForTimeout(3500);
  await page.screenshot({ path: EVID + "/bug02-01-patient-page.png", fullPage: true });

  const tab = page.getByRole("button", { name: /Lịch sử InBody/ })
    .or(page.getByRole("tab", { name: /Lịch sử InBody/ })).first();
  await tab.click();
  await page.waitForTimeout(3000);
  await page.screenshot({ path: EVID + "/bug02-02-inbody-history-tab.png", fullPage: true });

  const links = page.getByRole("link", { name: /Xem file gốc/ });
  const n = await links.count();
  L("B2 so luong link 'Xem file goc' = " + n);
  if (n === 0) { L("FAIL: khong co ban ghi InBody nao co file_url"); throw new Error("no link"); }

  const link = links.first();
  const href = await link.getAttribute("href");
  L("B3 href thuc te tren the <a> = " + href);
  L("B3 href chua 'minio:9000' (host noi bo)? = " + href.includes("minio:9000"));
  L("B3 host = " + new URL(href).host);

  await link.scrollIntoViewIfNeeded();
  await page.screenshot({ path: EVID + "/bug02-03-link-xem-file-goc.png", fullPage: true });

  // Click THAT: target=_blank -> popup, hoac headless chromium se download PDF
  let popupUrl = null, dlPath = null, popupStatus = null;
  const popupP = ctx.waitForEvent("page", { timeout: 12000 }).catch(() => null);
  const dlP = page.waitForEvent("download", { timeout: 12000 }).catch(() => null);
  await link.click();
  const popup = await popupP;
  if (popup) {
    popupUrl = popup.url();
    L("B4 CLICK -> mo tab moi, url = " + popupUrl.slice(0, 140) + "...");
    const dl2 = await popup.waitForEvent("download", { timeout: 8000 }).catch(() => null);
    if (dl2) {
      dlPath = EVID + "/bug02-clickthrough-downloaded.pdf";
      await dl2.saveAs(dlPath);
      L("B4 tab moi tra ve file -> da tai ve: " + dl2.suggestedFilename());
    } else {
      await popup.waitForTimeout(3000);
      await popup.screenshot({ path: EVID + "/bug02-04-popup-pdf.png" }).catch(e => L("  (khong chup duoc popup: " + e.message + ")"));
      const body = await popup.locator("body").innerText().catch(() => "");
      L("B4 noi dung text tab moi (200 ky tu) = " + JSON.stringify(body.slice(0, 200)));
      popupStatus = body;
    }
  }
  const dl = await dlP;
  if (dl && !dlPath) {
    dlPath = EVID + "/bug02-clickthrough-downloaded.pdf";
    await dl.saveAs(dlPath);
    L("B4 CLICK -> trinh duyet tai file: " + dl.suggestedFilename());
  }
  if (!popup && !dl) L("B4 CLICK -> KHONG mo tab moi, KHONG download (nghi ngo loi)");

  // Xac nhan browser context thuc su goi duoc URL nay (dung network stack cua browser)
  const resp = await ctx.request.get(href);
  const ct = resp.headers()["content-type"];
  const buf = await resp.body();
  L("B5 browser goi truc tiep URL: HTTP " + resp.status() + ", content-type = " + ct + ", size = " + buf.length + " bytes");
  L("B5 magic bytes = " + JSON.stringify(buf.slice(0, 5).toString("latin1")));
  if (!dlPath) {
    dlPath = EVID + "/bug02-clickthrough-downloaded.pdf";
    fs.writeFileSync(dlPath, buf);
  }

  const hits = allReq.filter(u => u.includes("9000") || u.includes("minio"));
  L("B6 cac request browser gui toi MinIO:");
  hits.forEach(h => L("   " + h.slice(0, 150)));
  L("B6 co request nao toi host noi bo 'minio:9000'? = " + allReq.some(u => u.includes("//minio:9000")));

  L("PAGEERROR = " + JSON.stringify(errs));
  fs.writeFileSync(EVID + "/bug02-clickthrough-log.txt", log.join("\n"), "utf8");
  await browser.close();
})().catch(e => {
  log.push("FATAL " + e.message);
  fs.writeFileSync(EVID + "/bug02-clickthrough-log.txt", log.join("\n"), "utf8");
  console.error("FATAL", e.message);
  process.exit(1);
});
