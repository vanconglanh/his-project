"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useTranslations } from "next-intl";
import { toast } from "sonner";
import { Eye, EyeOff, Loader2, ShieldCheck, ArrowLeft, Copy, Download } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { QuickLoginPanel } from "@/components/forms/QuickLoginPanel";
import { useAuth } from "@/lib/hooks/use-auth";
import { useSetup2FA, useEnable2FA } from "@/lib/hooks/use-users";
import { verify2fa } from "@/lib/api/auth";
import { getErrorMessage } from "@/lib/utils/errors";
import type { AxiosError } from "axios";
import { cn } from "@/lib/utils";

const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Email là bắt buộc")
    .email("Email không hợp lệ"),
  password: z
    .string()
    .min(1, "Mật khẩu là bắt buộc")
    .min(6, "Mật khẩu tối thiểu 6 ký tự"),
  rememberMe: z.boolean().default(false),
});

type LoginFormData = z.infer<typeof loginSchema>;

// Bước hiện tại của luồng đăng nhập.
// - credentials: nhập email + mật khẩu
// - mfa:         user đã bật 2FA → nhập mã TOTP / recovery code (POST /auth/2fa/verify)
// - mfaSetup:    role bắt buộc 2FA nhưng chưa bật → thiết lập 2FA ngay tại đây
type LoginStep = "credentials" | "mfa" | "mfaSetup";
type MfaSetupSub = "loading" | "setup" | "verify" | "done";

// TODO(REMOVE-BEFORE-PROD): tien ich dev/test dang nhap nhanh, xoa truoc khi
// len production. Grep marker "TODO(REMOVE-BEFORE-PROD)" de don dep het cho
// lien quan (bao gom ca QuickLoginPanel.tsx).
// Chi bat panel dang nhap nhanh khi build voi NEXT_PUBLIC_TEST_LOGIN_PANEL=true
// (Docker build-arg, xem frontend/Dockerfile). Mac dinh KHONG bat - production/
// staging build binh thuong khong truyen arg nay nen panel khong bao gio xuat hien.
const SHOW_QUICK_LOGIN = process.env.NEXT_PUBLIC_TEST_LOGIN_PANEL === "true";

export function LoginForm() {
  const t = useTranslations("Auth");
  const router = useRouter();
  const { login, establishSession, setMfaSetupToken } = useAuth();
  const setup2FAMutation = useSetup2FA();
  const enable2FAMutation = useEnable2FA();

  const [showPassword, setShowPassword] = useState(false);
  const [step, setStep] = useState<LoginStep>("credentials");

  // ─── State cho bước MFA (verify TOTP) ─────────────────────────────────────────
  const [mfaPendingToken, setMfaPendingToken] = useState<string>("");
  const [mfaCode, setMfaCode] = useState("");
  const [mfaVerifying, setMfaVerifying] = useState(false);

  // ─── State cho bước thiết lập 2FA bắt buộc ───────────────────────────────────
  const [setupSub, setSetupSub] = useState<MfaSetupSub>("loading");
  const [setupData, setSetupData] = useState<{ secret: string; qr_png_base64: string } | null>(null);
  const [setupCode, setSetupCode] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [setupMessage, setSetupMessage] = useState<string>("");

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormData>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(loginSchema) as any,
    defaultValues: {
      email: "",
      password: "",
      rememberMe: false,
    },
  });

  const rememberMe = watch("rememberMe");

  // ─── Bước 1: đăng nhập bằng email + mật khẩu ─────────────────────────────────
  async function onSubmit(data: LoginFormData) {
    try {
      const res = await login({
        email: data.email,
        password: data.password,
        rememberMe: data.rememberMe,
      });

      // Trạng thái 2: đã bật 2FA → chuyển sang màn nhập mã TOTP.
      if (res.requires2fa && res.mfaPendingToken) {
        setMfaPendingToken(res.mfaPendingToken);
        setMfaCode("");
        setStep("mfa");
        return;
      }

      // Trạng thái 3: role bắt buộc 2FA nhưng chưa bật → thiết lập 2FA ngay.
      if (res.mfaSetupRequired && res.mfaSetupToken) {
        setSetupMessage(
          res.mfaSetupMessage ??
            "Tài khoản của bạn bắt buộc bật xác thực 2 lớp trước khi sử dụng hệ thống."
        );
        // Đặt token tạm (aud="mfa-setup") để apiClient tự đính kèm khi gọi
        // me/2fa/setup + me/2fa/enable. Không thiết lập phiên đăng nhập đầy đủ.
        setMfaSetupToken(res.mfaSetupToken);
        setStep("mfaSetup");
        setSetupSub("loading");
        void startForcedSetup();
        return;
      }

      // Trạng thái 1: đăng nhập bình thường → vào dashboard.
      router.push("/");
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 401) {
        toast.error(t("invalidCredentials"));
      } else {
        toast.error(t("serverError"));
      }
    }
  }

  // ─── Bước 2 (verify): xác minh mã TOTP / recovery code ───────────────────────
  async function onVerifyMfa() {
    const code = mfaCode.trim();
    if (!code) return;
    setMfaVerifying(true);
    try {
      const res = await verify2fa({ mfaPendingToken, code });
      // Thành công → response là LoginResponse đầy đủ, thiết lập phiên như login thường.
      await establishSession(res);
      router.push("/");
    } catch (err: unknown) {
      const axiosErr = err as AxiosError<{ error?: { code?: string } }>;
      const errCode = axiosErr?.response?.data?.error?.code;
      toast.error(getErrorMessage(err, "Xác thực 2 lớp thất bại"));
      // Token tạm hết hạn → đưa user về màn nhập email/mật khẩu.
      if (errCode === "AUTH_MFA_TOKEN_INVALID") {
        backToCredentials();
      }
    } finally {
      setMfaVerifying(false);
    }
  }

  // ─── Bước 3 (thiết lập 2FA bắt buộc) ─────────────────────────────────────────
  async function startForcedSetup() {
    try {
      const data = await setup2FAMutation.mutateAsync();
      setSetupData({ secret: data.secret, qr_png_base64: data.qr_png_base64 });
      setSetupSub("setup");
    } catch {
      // useSetup2FA đã toast lỗi. Đưa user về màn đăng nhập để thử lại.
      backToCredentials();
    }
  }

  async function verifyAndEnable() {
    try {
      const result = await enable2FAMutation.mutateAsync(setupCode);
      setRecoveryCodes(result.recovery_codes);
      setSetupCode("");
      setSetupSub("done");
    } catch {
      // useEnable2FA đã toast lỗi (TWO_FA_INVALID_CODE...). Giữ nguyên màn nhập mã.
    }
  }

  function copyRecoveryCodes() {
    navigator.clipboard.writeText(recoveryCodes.join("\n"));
    toast.success("Đã sao chép mã khôi phục");
  }

  function downloadRecoveryCodes() {
    const blob = new Blob([recoveryCodes.join("\n")], { type: "text/plain" });
    const url = URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = "prodiab-recovery-codes.txt";
    a.click();
    URL.revokeObjectURL(url);
  }

  // Quay lại màn đăng nhập, dọn sạch mọi state 2FA tạm.
  function backToCredentials() {
    setStep("credentials");
    setMfaPendingToken("");
    setMfaCode("");
    setSetupData(null);
    setSetupCode("");
    setRecoveryCodes([]);
    setSetupSub("loading");
  }

  // ─── Render: bước nhập mã TOTP (đã bật 2FA) ──────────────────────────────────
  if (step === "mfa") {
    return (
      <div className="space-y-4">
        <div className="space-y-1">
          <h2 className="text-base font-semibold">Xác thực 2 lớp</h2>
          <p className="text-sm text-muted-foreground">
            Nhập mã 6 số từ ứng dụng Authenticator, hoặc mã khôi phục dạng
            xxxxx-xxxxx.
          </p>
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="mfa-code">Mã xác thực</Label>
          <Input
            id="mfa-code"
            autoFocus
            inputMode="text"
            autoComplete="one-time-code"
            placeholder="123456"
            value={mfaCode}
            // Cho phép chữ số + dấu gạch ngang (recovery code). Không cho ký tự khác.
            onChange={(e) => setMfaCode(e.target.value.replace(/[^0-9a-zA-Z-]/g, ""))}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                void onVerifyMfa();
              }
            }}
            className="text-center text-2xl tracking-widest"
          />
        </div>
        <Button
          type="button"
          className="w-full h-11"
          onClick={() => void onVerifyMfa()}
          disabled={!mfaCode.trim() || mfaVerifying}
        >
          {mfaVerifying && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          Xác minh
        </Button>
        <Button
          type="button"
          variant="link"
          className="w-full h-auto p-0 text-sm"
          onClick={backToCredentials}
        >
          <ArrowLeft className="mr-1 h-4 w-4" />
          Quay lại đăng nhập
        </Button>
      </div>
    );
  }

  // ─── Render: bước thiết lập 2FA bắt buộc ─────────────────────────────────────
  if (step === "mfaSetup") {
    return (
      <div className="space-y-4">
        <div className="space-y-1">
          <h2 className="text-base font-semibold">Thiết lập xác thực 2 lớp</h2>
        </div>

        {setupMessage && (
          <Alert>
            <AlertDescription>{setupMessage}</AlertDescription>
          </Alert>
        )}

        {setupSub === "loading" && (
          <div className="flex items-center justify-center py-8 text-muted-foreground">
            <Loader2 className="mr-2 h-5 w-5 animate-spin" />
            Đang tạo mã QR...
          </div>
        )}

        {setupSub === "setup" && setupData && (
          <div className="space-y-4">
            <p className="text-sm font-medium">
              Bước 1: Quét mã QR bằng ứng dụng Authenticator
            </p>
            <div className="flex flex-col items-center gap-4">
              {/* eslint-disable-next-line @next/next/no-img-element */}
              <img
                src={`data:image/png;base64,${setupData.qr_png_base64}`}
                alt="QR Code 2FA"
                className="w-48 h-48 border rounded"
              />
              <div className="text-center">
                <p className="text-xs text-muted-foreground mb-1">
                  Hoặc nhập thủ công mã bí mật:
                </p>
                <code className="bg-muted px-3 py-1 rounded text-sm font-mono">
                  {setupData.secret}
                </code>
              </div>
            </div>
            <div className="flex justify-between gap-2">
              <Button variant="outline" onClick={backToCredentials}>
                Huỷ
              </Button>
              <Button onClick={() => setSetupSub("verify")}>Tiếp theo</Button>
            </div>
          </div>
        )}

        {setupSub === "verify" && (
          <div className="space-y-4">
            <p className="text-sm font-medium">
              Bước 2: Nhập mã 6 số từ ứng dụng Authenticator
            </p>
            <div className="space-y-1.5">
              <Label htmlFor="setup-code">Mã xác thực</Label>
              <Input
                id="setup-code"
                autoFocus
                placeholder="123456"
                maxLength={6}
                value={setupCode}
                onChange={(e) => setSetupCode(e.target.value.replace(/\D/g, ""))}
                className="text-center text-2xl tracking-widest max-w-[200px]"
              />
            </div>
            <div className="flex justify-between gap-2">
              <Button variant="outline" onClick={() => setSetupSub("setup")}>
                Quay lại
              </Button>
              <Button
                onClick={verifyAndEnable}
                disabled={setupCode.length !== 6 || enable2FAMutation.isPending}
              >
                {enable2FAMutation.isPending && (
                  <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                )}
                Xác minh &amp; Bật 2FA
              </Button>
            </div>
          </div>
        )}

        {setupSub === "done" && (
          <div className="space-y-4">
            <Alert className="border-yellow-500 bg-yellow-50 dark:bg-yellow-900/20">
              <AlertDescription className="text-yellow-800 dark:text-yellow-200 font-medium">
                Lưu các mã khôi phục này ngay! Chúng chỉ hiển thị 1 lần và không
                thể khôi phục.
              </AlertDescription>
            </Alert>
            <p className="text-sm font-medium">Lưu mã khôi phục (10 mã)</p>
            <div className="grid grid-cols-2 gap-2 bg-muted rounded p-3">
              {recoveryCodes.map((code, i) => (
                <code key={i} className="text-sm font-mono text-center py-1">
                  {code}
                </code>
              ))}
            </div>
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={copyRecoveryCodes}>
                <Copy className="mr-2 h-4 w-4" />
                Sao chép
              </Button>
              <Button variant="outline" size="sm" onClick={downloadRecoveryCodes}>
                <Download className="mr-2 h-4 w-4" />
                Tải xuống
              </Button>
            </div>
            <Button className="w-full" onClick={backToCredentials}>
              <ShieldCheck className="mr-2 h-4 w-4" />
              Đã lưu, đăng nhập lại
            </Button>
          </div>
        )}
      </div>
    );
  }

  // Dang nhap nhanh (dev/test) doi bang cach dien san email/mat khau roi goi
  // dung onSubmit() nhu form that - tai su dung nguyen ven luong 2FA (mfa/
  // mfaSetup) o tren, khong duplicate logic rieng (da tung gay bug: panel cu
  // tu goi API roi push("/") thang, bo qua case mfaSetupRequired -> dang
  // nhap "khong duoc" voi tai khoan admin bat buoc 2FA).
  async function quickLogin(email: string, password: string) {
    setValue("email", email);
    setValue("password", password);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    await handleSubmit(onSubmit as any)();
  }

  // ─── Render: bước nhập email + mật khẩu (mặc định) ───────────────────────────
  return (
    <>
    {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
    <form onSubmit={handleSubmit(onSubmit as any)} noValidate className="space-y-4">
      {/* Email */}
      <div className="space-y-1.5">
        <Label htmlFor="email">{t("email")}</Label>
        <Input
          id="email"
          type="email"
          autoComplete="email"
          placeholder={t("emailPlaceholder")}
          aria-invalid={!!errors.email}
          aria-describedby={errors.email ? "email-error" : undefined}
          {...register("email")}
        />
        {errors.email && (
          <p id="email-error" className="text-xs text-destructive">
            {errors.email.message}
          </p>
        )}
      </div>

      {/* Password */}
      <div className="space-y-1.5">
        <Label htmlFor="password">{t("password")}</Label>
        <div className="relative">
          <Input
            id="password"
            type={showPassword ? "text" : "password"}
            autoComplete="current-password"
            placeholder={t("passwordPlaceholder")}
            className="pr-10"
            aria-invalid={!!errors.password}
            aria-describedby={errors.password ? "password-error" : undefined}
            {...register("password")}
          />
          <Button
            type="button"
            variant="ghost"
            size="icon"
            className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7"
            onClick={() => setShowPassword((p) => !p)}
            aria-label={showPassword ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
          >
            {showPassword ? (
              <EyeOff className="h-4 w-4" />
            ) : (
              <Eye className="h-4 w-4" />
            )}
          </Button>
        </div>
        {errors.password && (
          <p id="password-error" className="text-xs text-destructive">
            {errors.password.message}
          </p>
        )}
      </div>

      {/* Remember me + forgot password */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Checkbox
            id="rememberMe"
            checked={rememberMe}
            onCheckedChange={(checked) =>
              setValue("rememberMe", checked === true)
            }
            aria-label={t("rememberMe")}
          />
          <Label
            htmlFor="rememberMe"
            className="text-sm font-normal cursor-pointer"
          >
            {t("rememberMe")}
          </Label>
        </div>
        <Button
          type="button"
          variant="link"
          className="h-auto p-0 text-sm"
        >
          {t("forgotPassword")}
        </Button>
      </div>

      {/* Submit */}
      <Button
        type="submit"
        className={cn("w-full h-11", isSubmitting && "opacity-80")}
        disabled={isSubmitting}
      >
        {isSubmitting ? (
          <>
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
            {t("loggingIn")}
          </>
        ) : (
          t("loginButton")
        )}
      </Button>
    </form>
    {/* TODO(REMOVE-BEFORE-PROD): render panel dang nhap nhanh dev/test - xoa cung SHOW_QUICK_LOGIN o tren */}
    {SHOW_QUICK_LOGIN && (
      <QuickLoginPanel onQuickLogin={quickLogin} disabled={isSubmitting} />
    )}
    </>
  );
}
