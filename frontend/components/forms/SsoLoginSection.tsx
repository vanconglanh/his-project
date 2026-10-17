"use client";

import { useEffect, useState } from "react";
import { useSearchParams } from "next/navigation";
import { useTranslations } from "next-intl";
import { Loader2, LogIn } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { API_BASE_URL } from "@/lib/api/client";

const KNOWN_ERRORS = [
  "SSO_DISABLED",
  "SSO_INVALID_STATE",
  "SSO_IDP_ERROR",
  "SSO_TOKEN_INVALID",
  "SSO_EMAIL_NOT_VERIFIED",
  "SSO_USER_NOT_PROVISIONED",
  "SSO_USER_DISABLED",
  "SSO_AMBIGUOUS_ACCOUNT",
  "SSO_IDP_UNAVAILABLE",
] as const;

/** Chỉ chấp nhận path tương đối nội bộ (chống open redirect). */
export function safeReturnUrl(raw: string | null | undefined, fallback = "/dashboard"): string {
  if (!raw || !raw.startsWith("/") || raw.startsWith("//") || raw.startsWith("/\\")) {
    return fallback;
  }
  return raw;
}

/**
 * Nút "Đăng nhập bằng tài khoản DiaB" + hiển thị lỗi ?error= (SSO_*).
 * Khi mount gọi GET /api/v1/auth/sso/config; disabled/lỗi fetch → ẩn nút.
 */
export function SsoLoginSection() {
  const t = useTranslations("Auth.sso");
  const params = useSearchParams();
  const [enabled, setEnabled] = useState(false);
  const [configFailed, setConfigFailed] = useState(false);
  const [redirecting, setRedirecting] = useState(false);

  const errorCode = params.get("error");
  const returnUrl = safeReturnUrl(params.get("returnUrl") ?? params.get("redirect"));

  useEffect(() => {
    const ctrl = new AbortController();
    (async () => {
      try {
        const res = await fetch(`${API_BASE_URL}/api/v1/auth/sso/config`, {
          signal: ctrl.signal,
          credentials: "include",
        });
        if (!res.ok) throw new Error(String(res.status));
        const json = (await res.json()) as { data?: { enabled?: boolean } };
        setEnabled(json?.data?.enabled === true);
      } catch {
        if (!ctrl.signal.aborted) {
          setEnabled(false);
          setConfigFailed(true);
        }
      }
    })();
    return () => ctrl.abort();
  }, []);

  function startSso() {
    setRedirecting(true);
    // Đi qua /sso/complete để FE khởi tạo store từ cookie phiên rồi mới vào returnUrl.
    const target = `/sso/complete?returnUrl=${encodeURIComponent(returnUrl)}`;
    // Điều hướng toàn trang, KHÔNG fetch/XHR.
    window.location.href = `${API_BASE_URL}/api/v1/auth/sso/login?returnUrl=${encodeURIComponent(target)}`;
  }

  const errorMessage = errorCode
    ? (KNOWN_ERRORS as readonly string[]).includes(errorCode)
      ? t(`errors.${errorCode}` as never)
      : t("errors.UNKNOWN")
    : null;

  return (
    <div className="space-y-4">
      {errorMessage && (
        <Alert role="alert" className="border-destructive/50 bg-destructive/10">
          <AlertDescription className="text-destructive">{errorMessage}</AlertDescription>
        </Alert>
      )}
      {!errorMessage && configFailed && (
        <Alert role="status">
          <AlertDescription>{t("unavailable")}</AlertDescription>
        </Alert>
      )}
      {enabled && (
        <>
          <div className="flex items-center gap-3" role="separator" aria-label={t("or")}>
            <div className="h-px flex-1 bg-border" />
            <span className="text-xs uppercase text-muted-foreground">{t("or")}</span>
            <div className="h-px flex-1 bg-border" />
          </div>
          <Button
            type="button"
            variant="outline"
            className="w-full h-11"
            onClick={startSso}
            disabled={redirecting}
          >
            {redirecting ? (
              <>
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                {t("redirecting")}
              </>
            ) : (
              <>
                <LogIn className="mr-2 h-4 w-4" />
                {t("button")}
              </>
            )}
          </Button>
        </>
      )}
    </div>
  );
}
