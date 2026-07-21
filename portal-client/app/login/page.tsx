"use client";

import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { Suspense, useState } from "react";
import { BigButton } from "@/components/BigButton";
import { NumPad } from "@/components/NumPad";
import { ApiRequestError } from "@/lib/api";
import { setTokenCookie } from "@/lib/auth";
import {
  useForgotPinMutation,
  useLoginPinMutation,
  useResetPinMutation,
} from "@/lib/hooks";

type Step = "login" | "forgot" | "reset";

function LoginContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirect = searchParams.get("redirect") ?? "/";
  const sessionExpired = searchParams.get("reason") === "session_expired";

  const [step, setStep] = useState<Step>("login");
  const [phone, setPhone] = useState("");
  const [pin, setPin] = useState("");
  const [otp, setOtp] = useState("");
  const [newPin, setNewPin] = useState("");
  const [error, setError] = useState<string | null>(null);

  const loginMutation = useLoginPinMutation();
  const forgotMutation = useForgotPinMutation();
  const resetMutation = useResetPinMutation();

  function handleLogin() {
    setError(null);
    loginMutation.mutate(
      { phone, pin },
      {
        onSuccess: (data) => {
          setTokenCookie(data.accessToken);
          router.push(redirect);
        },
        onError: (err) => {
          if (err instanceof ApiRequestError) {
            setError(err.message);
          } else {
            setError("Đăng nhập thất bại, vui lòng thử lại");
          }
        },
      },
    );
  }

  function handleForgot() {
    setError(null);
    forgotMutation.mutate(
      { phone },
      {
        onSuccess: () => setStep("reset"),
        onError: (err) => {
          setError(err instanceof ApiRequestError ? err.message : "Không gửi được mã, vui lòng thử lại");
        },
      },
    );
  }

  function handleReset() {
    setError(null);
    resetMutation.mutate(
      { phone, otp, newPin },
      {
        onSuccess: (data) => {
          setTokenCookie(data.accessToken);
          router.push(redirect);
        },
        onError: (err) => {
          setError(err instanceof ApiRequestError ? err.message : "Đặt lại PIN thất bại, vui lòng thử lại");
        },
      },
    );
  }

  return (
    <div className="flex min-h-screen flex-col justify-center px-6 py-10">
      <h1 className="mb-1 text-center text-slate-900">Cổng bệnh nhân</h1>
      <p className="mb-8 text-center text-base text-slate-500">
        {step === "login" && "Đăng nhập bằng số điện thoại và mã PIN"}
        {step === "forgot" && "Nhập số điện thoại để nhận mã xác nhận"}
        {step === "reset" && "Nhập mã xác nhận và đặt PIN mới"}
      </p>

      {sessionExpired && (
        <div className="mb-4 rounded-xl border-2 border-amber-200 bg-amber-50 p-3 text-center text-base font-medium text-amber-700">
          Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.
        </div>
      )}

      {error && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {error}
        </div>
      )}

      {step === "login" && (
        <div className="flex flex-col gap-5">
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Số điện thoại</span>
            <input
              type="tel"
              inputMode="numeric"
              value={phone}
              onChange={(e) => setPhone(e.target.value.replace(/[\s-]/g, ""))}
              placeholder="09xxxxxxxx"
              className="min-h-14 rounded-full border border-slate-300 bg-white px-5 text-lg shadow-[0_2px_8px_rgba(15,23,42,0.04)] focus-visible:border-[#01645A] focus-visible:ring-2 focus-visible:ring-teal-100"
              aria-label="Số điện thoại"
            />
          </label>

          <NumPad value={pin} onChange={setPin} label="Nhập mã PIN" />

          <BigButton
            onClick={handleLogin}
            disabled={loginMutation.isPending || phone.length < 9 || pin.length < 4}
          >
            {loginMutation.isPending ? "Đang đăng nhập..." : "Đăng nhập"}
          </BigButton>

          <div className="flex flex-col items-center gap-2 text-base">
            <button
              type="button"
              onClick={() => {
                setError(null);
                setStep("forgot");
              }}
              className="font-semibold text-teal-700 underline-offset-4 hover:underline"
            >
              Quên mã PIN?
            </button>
            <Link href="/activate" className="font-semibold text-teal-700 underline-offset-4 hover:underline">
              Kích hoạt tài khoản mới
            </Link>
          </div>
        </div>
      )}

      {step === "forgot" && (
        <div className="flex flex-col gap-5">
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Số điện thoại</span>
            <input
              type="tel"
              inputMode="numeric"
              value={phone}
              onChange={(e) => setPhone(e.target.value.replace(/[\s-]/g, ""))}
              placeholder="09xxxxxxxx"
              className="min-h-14 rounded-full border border-slate-300 bg-white px-5 text-lg shadow-[0_2px_8px_rgba(15,23,42,0.04)] focus-visible:border-[#01645A] focus-visible:ring-2 focus-visible:ring-teal-100"
              aria-label="Số điện thoại"
            />
          </label>
          <BigButton onClick={handleForgot} disabled={forgotMutation.isPending || phone.length < 9}>
            {forgotMutation.isPending ? "Đang gửi..." : "Gửi mã xác nhận"}
          </BigButton>
          <BigButton variant="outline" onClick={() => setStep("login")}>
            Quay lại đăng nhập
          </BigButton>
        </div>
      )}

      {step === "reset" && (
        <div className="flex flex-col gap-5">
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Mã xác nhận (OTP)</span>
            <input
              inputMode="numeric"
              value={otp}
              onChange={(e) => setOtp(e.target.value)}
              placeholder="Nhập mã đã gửi qua SMS"
              className="min-h-14 rounded-full border border-slate-300 bg-white px-5 text-lg shadow-[0_2px_8px_rgba(15,23,42,0.04)] focus-visible:border-[#01645A] focus-visible:ring-2 focus-visible:ring-teal-100"
              aria-label="Mã xác nhận"
            />
          </label>

          <NumPad value={newPin} onChange={setNewPin} label="Đặt mã PIN mới" />

          <BigButton
            onClick={handleReset}
            disabled={resetMutation.isPending || otp.length < 4 || newPin.length < 4}
          >
            {resetMutation.isPending ? "Đang lưu..." : "Xác nhận"}
          </BigButton>
        </div>
      )}
    </div>
  );
}

export default function LoginPage() {
  return (
    <Suspense fallback={null}>
      <LoginContent />
    </Suspense>
  );
}
