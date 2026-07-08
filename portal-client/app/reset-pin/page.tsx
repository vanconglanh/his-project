"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { BigButton } from "@/components/BigButton";
import { NumPad } from "@/components/NumPad";
import { TopBar } from "@/components/TopBar";
import { ErrorNotice } from "@/components/ErrorNotice";
import { saveToken } from "@/lib/auth";
import { ApiError } from "@/lib/api";
import { useForgotPin, useResetPin } from "@/lib/queries";

const OTP_LENGTH = 6;
const PIN_LENGTH = 6;

export default function ResetPinPage() {
  const router = useRouter();
  const forgotPinMutation = useForgotPin();
  const resetPinMutation = useResetPin();

  const [step, setStep] = useState<1 | 2>(1);
  const [phone, setPhone] = useState("");
  const [otp, setOtp] = useState("");
  const [newPin, setNewPin] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function handleSendOtp() {
    setErrorMessage(null);
    try {
      await forgotPinMutation.mutateAsync({ phone: phone.trim() });
      setStep(2);
    } catch {
      // Theo hợp đồng API luôn trả 202, nhưng vẫn phòng khi lỗi mạng
      setErrorMessage("Không gửi được yêu cầu. Vui lòng kiểm tra kết nối và thử lại.");
    }
  }

  async function handleReset() {
    setErrorMessage(null);
    try {
      const session = await resetPinMutation.mutateAsync({ phone: phone.trim(), otp, newPin });
      saveToken(session.accessToken);
      router.replace("/");
    } catch (error) {
      setErrorMessage(error instanceof ApiError ? error.message : "Không đặt lại được mã PIN. Vui lòng thử lại.");
      setOtp("");
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col gap-6 px-5 py-6">
      <TopBar title="Quên mã PIN" showBack />

      {step === 1 ? (
        <div className="flex flex-col gap-5">
          <p className="text-base text-[--color-text-muted]">
            Nhập số điện thoại đã đăng ký. Mã OTP sẽ được gửi qua email đã đăng ký với phòng khám (nếu có).
          </p>
          <div className="flex flex-col gap-2">
            <label htmlFor="phone" className="text-base font-semibold">
              Số điện thoại
            </label>
            <input
              id="phone"
              type="tel"
              inputMode="tel"
              value={phone}
              onChange={(event) => setPhone(event.target.value)}
              className="min-h-[56px] rounded-2xl border-2 border-[--color-border] bg-white px-4 text-lg focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
            />
          </div>
          {errorMessage ? <ErrorNotice message={errorMessage} /> : null}
          <BigButton onClick={handleSendOtp} disabled={phone.trim().length < 9 || forgotPinMutation.isPending}>
            {forgotPinMutation.isPending ? "Đang gửi..." : "Gửi mã OTP"}
          </BigButton>
        </div>
      ) : (
        <div className="flex flex-col gap-6">
          <p className="text-base text-[--color-text-muted]">
            Nhập mã OTP đã nhận và mã PIN mới ({PIN_LENGTH} số).
          </p>
          <div>
            <p className="mb-2 text-base font-semibold">Mã OTP</p>
            <NumPad value={otp} maxLength={OTP_LENGTH} onChange={setOtp} label="Mã OTP đã nhập" />
          </div>
          <div>
            <p className="mb-2 text-base font-semibold">Mã PIN mới</p>
            <NumPad value={newPin} maxLength={PIN_LENGTH} onChange={setNewPin} label="Mã PIN mới đã nhập" />
          </div>
          {errorMessage ? <ErrorNotice message={errorMessage} /> : null}
          <BigButton
            onClick={handleReset}
            disabled={otp.length !== OTP_LENGTH || newPin.length !== PIN_LENGTH || resetPinMutation.isPending}
          >
            {resetPinMutation.isPending ? "Đang xử lý..." : "Đặt lại mã PIN"}
          </BigButton>
        </div>
      )}
    </main>
  );
}
