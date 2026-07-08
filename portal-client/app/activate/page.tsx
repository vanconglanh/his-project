"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { BigButton } from "@/components/BigButton";
import { CheckCircleIcon } from "@/components/icons";
import { NumPad } from "@/components/NumPad";
import { ApiRequestError } from "@/lib/api";
import { setTokenCookie } from "@/lib/auth";
import { useActivateMutation } from "@/lib/hooks";

type Step = 1 | 2 | 3;

export default function ActivatePage() {
  const router = useRouter();
  const [step, setStep] = useState<Step>(1);
  const [phone, setPhone] = useState("");
  const [activationCode, setActivationCode] = useState("");
  const [pin, setPin] = useState("");
  const [error, setError] = useState<string | null>(null);

  const activateMutation = useActivateMutation();

  function handleActivate() {
    setError(null);
    activateMutation.mutate(
      { phone, activationCode, pin },
      {
        onSuccess: (data) => {
          setTokenCookie(data.accessToken);
          setStep(3);
        },
        onError: (err) => {
          setError(err instanceof ApiRequestError ? err.message : "Kích hoạt thất bại, vui lòng thử lại");
        },
      },
    );
  }

  return (
    <div className="flex min-h-screen flex-col justify-center px-6 py-10">
      <h1 className="mb-1 text-center text-slate-900">Kích hoạt tài khoản</h1>
      <p className="mb-8 text-center text-base text-slate-500">Bước {step}/3</p>

      {error && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {error}
        </div>
      )}

      {step === 1 && (
        <div className="flex flex-col gap-5">
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Số điện thoại</span>
            <input
              type="tel"
              inputMode="numeric"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              placeholder="09xxxxxxxx"
              className="min-h-14 rounded-2xl border-2 border-slate-300 px-4 text-lg focus-visible:border-blue-500"
              aria-label="Số điện thoại"
            />
          </label>
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Mã kích hoạt</span>
            <input
              value={activationCode}
              onChange={(e) => setActivationCode(e.target.value)}
              placeholder="Mã do phòng khám cung cấp"
              className="min-h-14 rounded-2xl border-2 border-slate-300 px-4 text-lg focus-visible:border-blue-500"
              aria-label="Mã kích hoạt"
            />
          </label>
          <BigButton
            onClick={() => setStep(2)}
            disabled={phone.length < 9 || activationCode.length < 4}
          >
            Tiếp tục
          </BigButton>
          <Link
            href="/login"
            className="text-center text-base font-semibold text-blue-600 underline-offset-4 hover:underline"
          >
            Đã có tài khoản? Đăng nhập
          </Link>
        </div>
      )}

      {step === 2 && (
        <div className="flex flex-col gap-5">
          <NumPad value={pin} onChange={setPin} label="Đặt mã PIN (4-6 số)" />
          <BigButton onClick={handleActivate} disabled={activateMutation.isPending || pin.length < 4}>
            {activateMutation.isPending ? "Đang kích hoạt..." : "Hoàn tất kích hoạt"}
          </BigButton>
          <BigButton variant="outline" onClick={() => setStep(1)}>
            Quay lại
          </BigButton>
        </div>
      )}

      {step === 3 && (
        <div className="flex flex-col items-center gap-5 text-center">
          <CheckCircleIcon className="h-16 w-16 text-green-600" />
          <p className="text-xl font-semibold text-slate-900">Kích hoạt thành công!</p>
          <p className="text-base text-slate-500">Tài khoản của bạn đã sẵn sàng sử dụng.</p>
          <BigButton onClick={() => router.push("/")}>Vào trang chủ</BigButton>
        </div>
      )}
    </div>
  );
}
