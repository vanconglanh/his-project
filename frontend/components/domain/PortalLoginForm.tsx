"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Activity, ArrowLeft } from "lucide-react";
import { usePortalAuth } from "@/lib/hooks/use-portal-auth";

const phoneSchema = z.object({
  phone: z
    .string()
    .min(9, "Số điện thoại không hợp lệ")
    .regex(/^\+?[1-9]\d{7,14}$/, "Định dạng: +84xxxxxxxxx hoặc 0xxxxxxxxx"),
});

const otpSchema = z.object({
  otp: z.string().length(6, "Mã OTP gồm 6 chữ số").regex(/^\d{6}$/, "Chỉ nhập số"),
});

type PhoneForm = z.infer<typeof phoneSchema>;
type OtpForm = z.infer<typeof otpSchema>;

function normalizePhone(phone: string): string {
  if (phone.startsWith("0")) {
    return "+84" + phone.slice(1);
  }
  if (!phone.startsWith("+")) {
    return "+" + phone;
  }
  return phone;
}

export function PortalLoginForm() {
  const [phoneValue, setPhoneValue] = useState("");
  const { step, setStep, requestOtp, verifyOtp, isRequestingOtp, isVerifyingOtp } =
    usePortalAuth();

  const phoneForm = useForm<PhoneForm>({
    resolver: zodResolver(phoneSchema),
    defaultValues: { phone: "" },
  });

  const otpForm = useForm<OtpForm>({
    resolver: zodResolver(otpSchema),
    defaultValues: { otp: "" },
  });

  function onSubmitPhone(values: PhoneForm) {
    const normalized = normalizePhone(values.phone);
    setPhoneValue(normalized);
    requestOtp({ phone: normalized });
  }

  function onSubmitOtp(values: OtpForm) {
    verifyOtp({ phone: phoneValue, otp: values.otp });
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/30 px-4">
      <div className="w-full max-w-sm">
        {/* Logo */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center h-14 w-14 rounded-full bg-primary/10 mb-4">
            <Activity className="h-7 w-7 text-primary" />
          </div>
          <h1 className="text-2xl font-bold">Cổng bệnh nhân</h1>
          <p className="text-sm text-muted-foreground mt-1">
            {step === "phone"
              ? "Nhập số điện thoại để nhận mã OTP"
              : `Nhập mã OTP đã gửi tới ${phoneValue}`}
          </p>
        </div>

        {step === "phone" ? (
          <form onSubmit={phoneForm.handleSubmit(onSubmitPhone)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="phone">Số điện thoại</Label>
              <Input
                id="phone"
                type="tel"
                placeholder="+84 9xx xxx xxx"
                autoComplete="tel"
                {...phoneForm.register("phone")}
                className="min-h-[44px]"
              />
              {phoneForm.formState.errors.phone && (
                <p className="text-xs text-destructive">
                  {phoneForm.formState.errors.phone.message}
                </p>
              )}
            </div>
            <Button type="submit" className="w-full min-h-[44px]" disabled={isRequestingOtp}>
              {isRequestingOtp ? "Đang gửi OTP..." : "Gửi mã OTP"}
            </Button>
          </form>
        ) : (
          <form onSubmit={otpForm.handleSubmit(onSubmitOtp)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="otp">Mã OTP (6 chữ số)</Label>
              <Input
                id="otp"
                type="text"
                inputMode="numeric"
                placeholder="••••••"
                maxLength={6}
                autoComplete="one-time-code"
                {...otpForm.register("otp")}
                className="min-h-[44px] text-center text-2xl tracking-[0.5em] font-mono"
              />
              {otpForm.formState.errors.otp && (
                <p className="text-xs text-destructive">{otpForm.formState.errors.otp.message}</p>
              )}
              <p className="text-xs text-muted-foreground">Mã có hiệu lực trong 5 phút</p>
            </div>
            <Button type="submit" className="w-full min-h-[44px]" disabled={isVerifyingOtp}>
              {isVerifyingOtp ? "Đang xác thực..." : "Xác thực"}
            </Button>
            <Button
              type="button"
              variant="ghost"
              className="w-full"
              onClick={() => {
                setStep("phone");
                otpForm.reset();
              }}
            >
              <ArrowLeft className="mr-2 h-4 w-4" />
              Đổi số điện thoại
            </Button>
          </form>
        )}
      </div>
    </div>
  );
}
