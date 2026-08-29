"use client";

// Panel dang nhap nhanh CHI DUNG CHO DEV/TEST LOCAL - KHONG bao gio hien tren
// production/staging (env NEXT_PUBLIC_TEST_LOGIN_PANEL phai duoc bat tuong minh
// luc build, mac dinh la tat). Chon role -> tu dong dien + submit dung luong dang
// nhap that (khong bypass xac thuc backend), chi tiet kiem thao tac go tay.
import { useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/lib/hooks/use-auth";
import { cn } from "@/lib/utils";

interface TestAccount {
  roleCode: string;
  roleLabel: string;
  email: string;
}

const TEST_ACCOUNTS: TestAccount[] = [
  { roleCode: "admin", roleLabel: "Quản trị viên", email: "qc.admin@prodiab.test" },
  { roleCode: "bac_si", roleLabel: "Bác sĩ", email: "bacsi.test@prodiab.test" },
  { roleCode: "le_tan", roleLabel: "Lễ tân", email: "letan.test@prodiab.test" },
  { roleCode: "duoc_si", roleLabel: "Dược sĩ", email: "duocsi.test@prodiab.test" },
  { roleCode: "ke_toan", roleLabel: "Kế toán", email: "ketoan.test@prodiab.test" },
  { roleCode: "ky_thuat_vien", roleLabel: "Kỹ thuật viên", email: "ktv.test@prodiab.test" },
];

// Mat khau chung cho tat ca tai khoan test - chi ton tai o moi truong dev/local.
const TEST_PASSWORD = "Test@123";

export function TestLoginPanel() {
  const router = useRouter();
  const { login } = useAuth();
  const [loadingRole, setLoadingRole] = useState<string | null>(null);

  async function handleQuickLogin(account: TestAccount) {
    setLoadingRole(account.roleCode);
    try {
      await login({ email: account.email, password: TEST_PASSWORD, rememberMe: false });
      router.push("/");
    } catch {
      toast.error(`Không đăng nhập được với tài khoản ${account.roleLabel} — kiểm tra seed data test.`);
    } finally {
      setLoadingRole(null);
    }
  }

  return (
    <div className="mt-6 rounded-lg border border-dashed border-amber-500/50 bg-amber-500/5 p-4">
      <div className="mb-3 flex items-center gap-2">
        <span className="rounded bg-amber-500/20 px-1.5 py-0.5 text-[10px] font-bold uppercase tracking-wide text-amber-600 dark:text-amber-400">
          Dev only
        </span>
        <p className="text-sm font-medium">Đăng nhập nhanh theo vai trò (test)</p>
      </div>
      <div className="grid grid-cols-2 gap-2 sm:grid-cols-3">
        {TEST_ACCOUNTS.map((acc) => (
          <Button
            key={acc.roleCode}
            type="button"
            variant="outline"
            size="sm"
            disabled={loadingRole !== null}
            onClick={() => handleQuickLogin(acc)}
            className={cn("justify-start", loadingRole === acc.roleCode && "opacity-80")}
          >
            {loadingRole === acc.roleCode ? (
              <Loader2 className="mr-1.5 h-3.5 w-3.5 animate-spin" />
            ) : null}
            {acc.roleLabel}
          </Button>
        ))}
      </div>
      <p className="mt-2 text-xs text-muted-foreground">
        Chọn role để đăng nhập ngay, không cần nhập email/mật khẩu thủ công.
      </p>
    </div>
  );
}
