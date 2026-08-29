# -*- coding: utf-8 -*-
"""
ITE 2026-08-30 — chup evidence tung step qua browser that (Playwright) va KHOANH VUNG:
  - khung XANH DUONG = INPUT   (o nhap du lieu)
  - khung VANG       = ACTION  (nut / thao tac vua thuc hien)
  - khung XANH LA    = RESULT  (vung du lieu ket qua)

Chay:
  set UTE_ENCOUNTER_ID=<guid> & set UTE_PATIENT_ID=<guid> & python capture_evidence.py
Yeu cau: frontend http://localhost:3000 + backend http://localhost:5000 dang chay,
         FE bat NEXT_PUBLIC_TEST_LOGIN_PANEL=true (panel dang nhap nhanh dev-only).
"""
import os
import sys
from PIL import Image, ImageDraw, ImageFont
from playwright.sync_api import sync_playwright

BASE = "http://localhost:3000"
OUT = os.path.dirname(os.path.abspath(__file__))
COLORS = {"INPUT": (56, 132, 255), "ACTION": (245, 176, 26), "RESULT": (35, 190, 120)}
ENCOUNTER_ID = os.environ.get("UTE_ENCOUNTER_ID", "")
PATIENT_ID = os.environ.get("UTE_PATIENT_ID", "")

http_errors = []


def _font(size=15):
    for p in (r"C:\Windows\Fonts\segoeui.ttf", r"C:\Windows\Fonts\arial.ttf"):
        if os.path.exists(p):
            return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def annotate(png_path, boxes):
    img = Image.open(png_path).convert("RGB")
    d = ImageDraw.Draw(img)
    f = _font(15)
    for kind, box, label in boxes:
        if not box:
            continue
        c = COLORS[kind]
        x0, y0 = int(box["x"]) - 3, int(box["y"]) - 3
        x1, y1 = int(box["x"] + box["width"]) + 3, int(box["y"] + box["height"]) + 3
        for w in range(3):
            d.rectangle([x0 - w, y0 - w, x1 + w, y1 + w], outline=c)
        text = f"{kind}: {label}"
        tb = d.textbbox((0, 0), text, font=f)
        tw, th = tb[2] - tb[0], tb[3] - tb[1]
        ty = max(0, y0 - th - 12)
        d.rectangle([x0, ty, x0 + tw + 14, ty + th + 10], fill=c)
        d.text((x0 + 7, ty + 5), text, fill=(255, 255, 255), font=f)
    img.save(png_path)


def box_of(page, selector, nth=0):
    try:
        loc = page.locator(selector).nth(nth)
        loc.wait_for(state="visible", timeout=5000)
        return loc.bounding_box()
    except Exception:
        print(f"  [warn] khong tim thay selector: {selector}")
        return None


def shot(page, name, boxes=None):
    p = os.path.join(OUT, name)
    page.screenshot(path=p, full_page=False)
    if boxes:
        annotate(p, boxes)
    print("  ->", name)
    return p


def login(page, role_button_text="Quản trị viên"):
    page.goto(f"{BASE}/login", wait_until="networkidle")
    page.get_by_role("button", name=role_button_text, exact=True).click()
    page.wait_for_url(lambda u: "/login" not in u, timeout=30000)
    page.wait_for_load_state("networkidle")


def ite_h10_mfa(page):
    """ITE-H10: bat buoc 2FA theo role KHONG duoc khoa nham dang nhap."""
    print("[ITE-H10] Dang nhap voi role bat buoc 2FA (admin)")
    page.goto(f"{BASE}/login", wait_until="networkidle")
    page.wait_for_timeout(1500)
    shot(page, "ITE-H10_step1_man-dang-nhap.png", [
        ("INPUT", box_of(page, "input[type='email'], input[name='email']"), "Email tai khoan"),
        ("INPUT", box_of(page, "input[type='password']"), "Mat khau"),
        ("ACTION", box_of(page, "button:has-text('Quản trị viên')"), "Chon dang nhap nhanh role Quan tri vien"),
    ])
    page.get_by_role("button", name="Quản trị viên", exact=True).click()
    page.wait_for_url(lambda u: "/login" not in u, timeout=30000)
    page.wait_for_load_state("networkidle")
    page.wait_for_timeout(3000)
    shot(page, "ITE-H10_step2_dang-nhap-thanh-cong.png", [
        ("RESULT", box_of(page, "main"),
         "Admin (role bat buoc 2FA, chua bat 2FA) VAN vao duoc he thong — khong bi khoa nham"),
    ])


def ite_c_cls(page):
    """ITE-C: luong CLS sau khi DROP 2 bang chet lab/rad_orders (migration 9171)."""
    print("[ITE-C] Luong CLS sau khi DROP bang chet")
    page.goto(f"{BASE}/labrad", wait_until="networkidle")
    page.wait_for_timeout(3000)
    shot(page, "ITE-C_step1_man-CLS-danh-sach.png", [
        ("RESULT", box_of(page, "main"), "Man CLS mo duoc, doc du lieu tu bang diab_his_cli_*"),
    ])
    if not ENCOUNTER_ID:
        return
    page.goto(f"{BASE}/encounters/{ENCOUNTER_ID}", wait_until="networkidle")
    page.wait_for_timeout(3000)
    tab = box_of(page, "button:has-text('Cận lâm sàng'), [role='tab']:has-text('Cận lâm sàng')")
    shot(page, "ITE-C_step2_luot-kham-truoc-khi-mo-tab-CLS.png", [
        ("ACTION", tab, "Bam tab 'Cận lâm sàng' de tai danh sach chi dinh XN/CDHA"),
    ])
    try:
        page.get_by_role("tab", name="Cận lâm sàng").click()
    except Exception:
        page.locator("button:has-text('Cận lâm sàng')").first.click()
    page.wait_for_timeout(5000)
    shot(page, "ITE-C_step3_tab-CLS-loi-tai-danh-sach-CDHA.png", [
        ("RESULT", box_of(page, "main"),
         "Vung CLS: danh sach CDHA khong tai duoc — API rad-orders tra HTTP 500 (BUG-02)"),
    ])


def ite_h14_package(page):
    """ITE-H14/H12/H13: goi dich vu tren man chi tiet benh nhan."""
    print("[ITE-H14] Goi dich vu tren chi tiet benh nhan")
    if not PATIENT_ID:
        print("  [skip] thieu UTE_PATIENT_ID")
        return
    page.goto(f"{BASE}/patients/{PATIENT_ID}", wait_until="networkidle")
    page.wait_for_timeout(4000)
    shot(page, "ITE-H14_step1_chi-tiet-benh-nhan.png", [
        ("RESULT", box_of(page, "main"), "Khu vuc goi dich vu: trang thai / con X-Y dinh muc / nut Gia han"),
    ])
    gh = box_of(page, "button:has-text('Gia hạn')")
    if gh:
        shot(page, "ITE-H14_step2_nut-gia-han.png", [
            ("ACTION", gh, "Nut 'Gia hạn' (H-14 FR-1211) hien thi khi goi het han con dinh muc"),
        ])


def ite_h1_notification(page):
    """ITE-H1: man cau hinh kenh gui SMS/Zalo ZNS."""
    print("[ITE-H1] Cau hinh kenh thong bao")
    page.goto(f"{BASE}/admin/notification-channels", wait_until="networkidle")
    page.wait_for_timeout(3000)
    shot(page, "ITE-H1_step1_man-cau-hinh-kenh.png", [
        ("RESULT", box_of(page, "main"), "Man cau hinh kenh SMS / Zalo ZNS per-tenant"),
    ])


def ite_e3_stock(page):
    """ITE-E3: dieu chuyen kho — kiem tra co man hinh nao khong."""
    print("[ITE-E3] Tim man dieu chuyen kho")
    page.goto(f"{BASE}/pharmacy", wait_until="networkidle")
    page.wait_for_timeout(3000)
    shot(page, "ITE-E3_step1_man-duoc-khong-co-menu-dieu-chuyen.png", [
        ("RESULT", box_of(page, "main"),
         "Module Duoc: KHONG co muc 'Dieu chuyen kho' — API co, UI chua co (GAP-01)"),
    ])


def main():
    with sync_playwright() as pw:
        browser = pw.chromium.launch()
        ctx = browser.new_context(viewport={"width": 1440, "height": 900}, locale="vi-VN")
        page = ctx.new_page()
        page.on("response", lambda r: http_errors.append(f"{r.status} {r.request.method} {r.url}")
                if r.status >= 500 else None)
        try:
            ite_h10_mfa(page)
            ite_c_cls(page)
            ite_h14_package(page)
            ite_h1_notification(page)
            ite_e3_stock(page)
        finally:
            if http_errors:
                print("\n=== HTTP 5xx quan sat duoc trong phien browser ===")
                for e in dict.fromkeys(http_errors):
                    print("  ", e)
            else:
                print("\n=== Khong co HTTP 5xx trong phien browser ===")
            browser.close()


if __name__ == "__main__":
    sys.exit(main())
