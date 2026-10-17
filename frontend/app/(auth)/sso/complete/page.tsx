"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import axios from "axios";
import { API_BASE_URL } from "@/lib/api/client";
import { SSO_PENDING_KEY } from "@/lib/sso-pending";
import { useAuthStore } from "@/lib/stores/auth-store";
import { safeReturnUrl } from "@/components/forms/SsoLoginSection";
import type { LoginResponse } from "@/lib/api/types";

/**
 * Backend callback đã set cookie httpOnly rồi 302 về đây. Trang này gọi /auth/me
 * (kèm cookie) để khởi tạo Zustand store (user, permissions, roles) rồi chuyển returnUrl.
 */
function SsoCompleteInner() {
  const t = useTranslations("Auth.sso");
  const router = useRouter();
  const params = useSearchParams();
  const [failed, setFailed] = useState(false);
  const started = useRef(false);

  useEffect(() => {
    if (started.current) return;
    started.current = true;
    const returnUrl = safeReturnUrl(params.get("returnUrl"));
    (async () => {
      try {
        const { data } = await axios.post<{ data: LoginResponse }>(
          `${API_BASE_URL}/api/v1/auth/sso/complete`,
          {},
          { withCredentials: true, timeout: 30000 }
        );
        const res = data.data;
        // Nhánh 2FA: chuyển về màn login, LoginForm đọc trạng thái chờ từ sessionStorage.
        if (res.requires2fa && res.mfaPendingToken) {
          sessionStorage.setItem(
            SSO_PENDING_KEY,
            JSON.stringify({ kind: "mfa", token: res.mfaPendingToken })
          );
          router.replace("/login");
          return;
        }
        if (res.mfaSetupRequired && res.mfaSetupToken) {
          sessionStorage.setItem(
            SSO_PENDING_KEY,
            JSON.stringify({
              kind: "setup",
              token: res.mfaSetupToken,
              message: res.mfaSetupMessage ?? "",
            })
          );
          router.replace("/login");
          return;
        }
        // Nhánh thường: giống login mật khẩu.
        const roles = [...(res.user?.roles ?? []), ...(res.user?.roleCodes ?? [])];
        useAuthStore
          .getState()
          .setAuth(res.user, res.accessToken, res.refreshToken, res.permissions ?? [], roles);
        await fetch("/session/set-cookie", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ accessToken: res.accessToken, expiresIn: res.expiresIn }),
        });
        router.replace(returnUrl);
      } catch {
        setFailed(true);
      }
    })();
  }, [params, router]);

  if (failed) {
    return (
      <div className="space-y-4 text-center" role="alert">
        <p className="text-sm text-destructive">{t("completeFailed")}</p>
        <Button variant="outline" className="h-11" onClick={() => router.replace("/login")}>
          {t("backToLogin")}
        </Button>
      </div>
    );
  }

  return (
    <div className="flex items-center justify-center gap-2 py-8 text-muted-foreground" role="status">
      <Loader2 className="h-5 w-5 animate-spin" />
      {t("completing")}
    </div>
  );
}

export default function SsoCompletePage() {
  return (
    <Suspense fallback={null}>
      <SsoCompleteInner />
    </Suspense>
  );
}
