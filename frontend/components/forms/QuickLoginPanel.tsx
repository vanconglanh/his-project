"use client";

// TODO(REMOVE-BEFORE-PROD): tien ich dev/test, xoa file nay truoc khi len
// production. Grep marker "TODO(REMOVE-BEFORE-PROD)" de tim tat ca cho lien
// quan (xem them LoginForm.tsx) khi don dep truoc prod.
// Panel dang nhap nhanh CHI DUNG CHO DEV/TEST - KHONG bao gio hien tren
// production/staging build thuong (env NEXT_PUBLIC_TEST_LOGIN_PANEL phai duoc
// bat tuong minh luc build qua Docker build-arg, mac dinh la tat).
//
// Component nay CHI render nut, khong tu goi API dang nhap - viec dien
// email/mat khau + submit duoc giao lai cho LoginForm.quickLogin() de tai su
// dung dung nguyen luong 2FA (mfa/mfaSetup) da co san. Truoc day panel nay
// (ten cu: TestLoginPanel) tu goi API rieng roi push("/") thang, bo qua case
// mfaSetupRequired -> tai khoan admin (bat buoc 2FA) bam nut khong dang nhap
// duoc, chi bi day nguoc ve trang login vi accessToken rong.
import { useState } from "react";

interface QuickAccount {
  roleCode: string;
  roleLabel: string;
  email: string;
}

// Danh sach tai khoan test khop voi seed dang dung THAT (db/seeds/diab_test_tenant.sql,
// tenant_id=2 "DIAB-TEST") - email/mat khau phai dong bo neu doi seed. Admin bat buoc
// 2FA nen sau khi bam se chuyen sang man thiet lap 2FA ngay trong LoginForm, dung
// hanh vi that cua he thong (khong bi day nguoc ve trang login nhu truoc).
const QUICK_ACCOUNTS: QuickAccount[] = [
  { roleCode: "admin", roleLabel: "Quản trị viên", email: "admin.test@diabtest.local" },
  { roleCode: "bac_si", roleLabel: "Bác sĩ", email: "bacsi.test@diabtest.local" },
  { roleCode: "bac_si_2", roleLabel: "Bác sĩ 2", email: "bacsi2.test@diabtest.local" },
  { roleCode: "le_tan", roleLabel: "Lễ tân", email: "letan.test@diabtest.local" },
  { roleCode: "duoc_si", roleLabel: "Dược sĩ", email: "duocsi.test@diabtest.local" },
  { roleCode: "ke_toan", roleLabel: "Kế toán", email: "ketoan.test@diabtest.local" },
  { roleCode: "ky_thuat_vien", roleLabel: "Kỹ thuật viên", email: "ktv.test@diabtest.local" },
];

// Mat khau chung cho tat ca tai khoan test - chi ton tai o moi truong dev/staging test.
const QUICK_PASSWORD = "admin123";

interface QuickLoginPanelProps {
  onQuickLogin: (email: string, password: string) => Promise<void>;
  disabled?: boolean;
}

export function QuickLoginPanel({ onQuickLogin, disabled }: QuickLoginPanelProps) {
  const [loadingRole, setLoadingRole] = useState<string | null>(null);

  async function handleClick(acc: QuickAccount) {
    setLoadingRole(acc.roleCode);
    try {
      await onQuickLogin(acc.email, QUICK_PASSWORD);
    } finally {
      setLoadingRole(null);
    }
  }

  return (
    <div className="mt-6 rounded-lg border border-dashed border-amber-500/50 bg-amber-500/5 p-4">
      <div className="mb-3 flex items-center gap-2">
        <span className="rounded bg-amber-500/20 px-1.5 py-0.5 text-xs font-bold uppercase tracking-wide text-amber-600 dark:text-amber-400">
          Dev only
        </span>
        <p className="text-sm font-medium">Đăng nhập nhanh theo vai trò (test)</p>
      </div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
        {QUICK_ACCOUNTS.map((acc) => (
          <button
            key={acc.roleCode}
            type="button"
            disabled={disabled || loadingRole !== null}
            onClick={() => handleClick(acc)}
            className="inline-flex items-center justify-start gap-1.5 rounded-md border border-input bg-background px-3 py-1.5 text-sm font-medium shadow-sm transition-colors hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-50"
          >
            {loadingRole === acc.roleCode ? (
              <span className="h-3.5 w-3.5 animate-spin rounded-full border-2 border-current border-t-transparent" />
            ) : null}
            {acc.roleLabel}
          </button>
        ))}
      </div>
      <p className="mt-2 text-xs text-muted-foreground">
        Chọn role để đăng nhập ngay (Quản trị viên sẽ chuyển sang bước thiết lập 2FA bắt buộc).
      </p>
    </div>
  );
}
