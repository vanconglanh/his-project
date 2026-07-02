"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { toast } from "sonner";
import { Eye, EyeOff, Loader2, ShieldCheck, ShieldOff, Copy, Download } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
} from "@/components/ui/dialog";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { useMe, useChangePassword, useSetup2FA, useEnable2FA, useDisable2FA } from "@/lib/hooks/use-users";

// ─── Password schema ──────────────────────────────────────────────────────────

const passwordSchema = z
  .object({
    old_password: z.string().min(1, "Nhập mật khẩu hiện tại"),
    new_password: z
      .string()
      .min(12, "Tối thiểu 12 ký tự")
      .regex(/[A-Z]/, "Cần có chữ hoa")
      .regex(/[a-z]/, "Cần có chữ thường")
      .regex(/\d/, "Cần có số")
      .regex(/[^A-Za-z0-9]/, "Cần có ký tự đặc biệt"),
    confirm_password: z.string(),
  })
  .refine((d) => d.new_password === d.confirm_password, {
    message: "Mật khẩu xác nhận không khớp",
    path: ["confirm_password"],
  });

type PasswordFormData = z.infer<typeof passwordSchema>;

// ─── 2FA wizard steps ─────────────────────────────────────────────────────────

type TwoFAStep = "idle" | "setup" | "verify" | "done";

// ─── Main page ────────────────────────────────────────────────────────────────

export default function SecurityPage() {
  const { data: me } = useMe();
  const changePasswordMutation = useChangePassword();
  const setup2FAMutation = useSetup2FA();
  const enable2FAMutation = useEnable2FA();
  const disable2FAMutation = useDisable2FA();

  // Password form
  const [showOld, setShowOld] = useState(false);
  const [showNew, setShowNew] = useState(false);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<PasswordFormData>({
    resolver: standardSchemaResolver(passwordSchema),
  });

  async function onPasswordSubmit(data: PasswordFormData) {
    await changePasswordMutation.mutateAsync({
      old_password: data.old_password,
      new_password: data.new_password,
    });
    reset();
  }

  // 2FA wizard
  const [twoFAStep, setTwoFAStep] = useState<TwoFAStep>("idle");
  const [setupData, setSetupData] = useState<{ secret: string; qr_png_base64: string } | null>(null);
  const [totpCode, setTotpCode] = useState("");
  const [recoveryCodes, setRecoveryCodes] = useState<string[]>([]);
  const [disableOpen, setDisableOpen] = useState(false);
  const [disablePassword, setDisablePassword] = useState("");
  const [disableCode, setDisableCode] = useState("");

  async function startSetup2FA() {
    const data = await setup2FAMutation.mutateAsync();
    setSetupData({ secret: data.secret, qr_png_base64: data.qr_png_base64 });
    setTwoFAStep("setup");
  }

  async function verifyAndEnable() {
    const result = await enable2FAMutation.mutateAsync(totpCode);
    setRecoveryCodes(result.recovery_codes);
    setTotpCode("");
    setTwoFAStep("done");
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

  async function handleDisable2FA() {
    await disable2FAMutation.mutateAsync({ password: disablePassword, code: disableCode || undefined });
    setDisableOpen(false);
    setDisablePassword("");
    setDisableCode("");
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-bold tracking-tight">Bảo mật tài khoản</h1>
        <p className="text-muted-foreground text-sm mt-1">
          Quản lý mật khẩu và xác thực 2 lớp
        </p>
      </div>

      {/* Change password */}
      <Card>
        <CardHeader>
          <CardTitle>Đổi mật khẩu</CardTitle>
          <CardDescription>
            Mật khẩu tối thiểu 12 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSubmit(onPasswordSubmit)} noValidate className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="old-pass">Mật khẩu hiện tại</Label>
              <div className="relative">
                <Input
                  id="old-pass"
                  type={showOld ? "text" : "password"}
                  className="pr-10"
                  {...register("old_password")}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7"
                  onClick={() => setShowOld((p) => !p)}
                  aria-label={showOld ? "Ẩn" : "Hiện"}
                >
                  {showOld ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
              </div>
              {errors.old_password && (
                <p className="text-xs text-destructive">{errors.old_password.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="new-pass">Mật khẩu mới</Label>
              <div className="relative">
                <Input
                  id="new-pass"
                  type={showNew ? "text" : "password"}
                  className="pr-10"
                  {...register("new_password")}
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="icon"
                  className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7"
                  onClick={() => setShowNew((p) => !p)}
                  aria-label={showNew ? "Ẩn" : "Hiện"}
                >
                  {showNew ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
              </div>
              {errors.new_password && (
                <p className="text-xs text-destructive">{errors.new_password.message}</p>
              )}
            </div>

            <div className="space-y-1.5">
              <Label htmlFor="confirm-pass">Xác nhận mật khẩu mới</Label>
              <Input
                id="confirm-pass"
                type="password"
                {...register("confirm_password")}
              />
              {errors.confirm_password && (
                <p className="text-xs text-destructive">{errors.confirm_password.message}</p>
              )}
            </div>

            <Button type="submit" disabled={isSubmitting || changePasswordMutation.isPending}>
              {(isSubmitting || changePasswordMutation.isPending) && (
                <Loader2 className="mr-2 h-4 w-4 animate-spin" />
              )}
              Đổi mật khẩu
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* 2FA section */}
      <Card>
        <CardHeader>
          <CardTitle>Xác thực 2 lớp (2FA)</CardTitle>
          <CardDescription>
            Tăng cường bảo mật bằng mã xác thực TOTP qua ứng dụng Authenticator.
          </CardDescription>
        </CardHeader>
        <CardContent>
          {me?.two_fa_enabled ? (
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <ShieldCheck className="h-5 w-5 text-green-500" />
                <span className="text-sm font-medium text-green-700 dark:text-green-400">
                  Xác thực 2 lớp đã được bật
                </span>
              </div>
              <Button
                variant="outline"
                size="sm"
                className="text-destructive border-destructive hover:bg-destructive/10"
                onClick={() => setDisableOpen(true)}
              >
                <ShieldOff className="mr-2 h-4 w-4" />
                Tắt 2FA
              </Button>
            </div>
          ) : twoFAStep === "idle" ? (
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">Chưa bật xác thực 2 lớp</p>
              <Button
                size="sm"
                onClick={startSetup2FA}
                disabled={setup2FAMutation.isPending}
              >
                {setup2FAMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                <ShieldCheck className="mr-2 h-4 w-4" />
                Bật 2FA
              </Button>
            </div>
          ) : twoFAStep === "setup" && setupData ? (
            <div className="space-y-4">
              <p className="text-sm font-medium">Bước 1: Quét mã QR bằng ứng dụng Authenticator</p>
              <div className="flex flex-col items-center gap-4">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={`data:image/png;base64,${setupData.qr_png_base64}`}
                  alt="QR Code 2FA"
                  className="w-48 h-48 border rounded"
                />
                <div className="text-center">
                  <p className="text-xs text-muted-foreground mb-1">Hoặc nhập thủ công mã bí mật:</p>
                  <code className="bg-muted px-3 py-1 rounded text-sm font-mono">
                    {setupData.secret}
                  </code>
                </div>
              </div>
              <div className="flex justify-between">
                <Button variant="outline" onClick={() => setTwoFAStep("idle")}>
                  Huỷ
                </Button>
                <Button onClick={() => setTwoFAStep("verify")}>
                  Tiếp theo
                </Button>
              </div>
            </div>
          ) : twoFAStep === "verify" ? (
            <div className="space-y-4">
              <p className="text-sm font-medium">Bước 2: Nhập mã 6 số từ ứng dụng Authenticator</p>
              <div className="space-y-1.5">
                <Label htmlFor="totp-code">Mã xác thực</Label>
                <Input
                  id="totp-code"
                  placeholder="123456"
                  maxLength={6}
                  value={totpCode}
                  onChange={(e) => setTotpCode(e.target.value.replace(/\D/g, ""))}
                  className="text-center text-2xl tracking-widest max-w-[200px]"
                />
              </div>
              <div className="flex justify-between">
                <Button variant="outline" onClick={() => setTwoFAStep("setup")}>
                  Quay lại
                </Button>
                <Button
                  onClick={verifyAndEnable}
                  disabled={totpCode.length !== 6 || enable2FAMutation.isPending}
                >
                  {enable2FAMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                  Xác minh &amp; Bật 2FA
                </Button>
              </div>
            </div>
          ) : twoFAStep === "done" ? (
            <div className="space-y-4">
              <Alert className="border-yellow-500 bg-yellow-50 dark:bg-yellow-900/20">
                <AlertDescription className="text-yellow-800 dark:text-yellow-200 font-medium">
                  Lưu các mã khôi phục này ngay! Chúng chỉ hiển thị 1 lần và không thể khôi phục.
                </AlertDescription>
              </Alert>
              <p className="text-sm font-medium">Bước 3: Lưu mã khôi phục (10 mã)</p>
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
              <Button onClick={() => setTwoFAStep("idle")} className="w-full">
                Hoàn tất
              </Button>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {/* Disable 2FA dialog */}
      <Dialog open={disableOpen} onOpenChange={setDisableOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Tắt xác thực 2 lớp</DialogTitle>
            <DialogDescription>
              Nhập mật khẩu để xác nhận. Hành động này sẽ giảm bảo mật tài khoản.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="dis-password">Mật khẩu</Label>
              <Input
                id="dis-password"
                type="password"
                value={disablePassword}
                onChange={(e) => setDisablePassword(e.target.value)}
              />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="dis-code">Mã TOTP hiện tại (tuỳ chọn)</Label>
              <Input
                id="dis-code"
                placeholder="123456"
                maxLength={6}
                value={disableCode}
                onChange={(e) => setDisableCode(e.target.value.replace(/\D/g, ""))}
              />
            </div>
            <div className="flex justify-end gap-3">
              <Button variant="outline" onClick={() => setDisableOpen(false)}>
                Huỷ
              </Button>
              <Button
                variant="destructive"
                onClick={handleDisable2FA}
                disabled={!disablePassword || disable2FAMutation.isPending}
              >
                {disable2FAMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
                Tắt 2FA
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  );
}
