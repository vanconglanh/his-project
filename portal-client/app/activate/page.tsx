"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { BigButton } from "@/components/BigButton";
import { NumPad } from "@/components/NumPad";
import { TopBar } from "@/components/TopBar";
import { ErrorNotice } from "@/components/ErrorNotice";
import { saveToken } from "@/lib/auth";
import { ApiError } from "@/lib/api";
import { useActivateAccount } from "@/lib/queries";

const CODE_LENGTH = 6;
const PIN_LENGTH = 6;

type Step = 1 | 2 | 3;

export default function ActivatePage() {
  const router = useRouter();
  const activateMutation = useActivateAccount();

  const [step, setStep] = useState<Step>(1);
  const [phone, setPhone] = useState("");
  const [activationCode, setActivationCode] = useState("");
  const [pin, setPin] = useState("");
  const [pinConfirm, setPinConfirm] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [fullName, setFullName] = useState<string | null>(null);

  const step1Valid = phone.trim().length >= 9 && activationCode.length === CODE_LENGTH;
  const step2Valid = pin.length === PIN_LENGTH && pinConfirm.length === PIN_LENGTH;

  async function handleActivate() {
    setErrorMessage(null);
    if (pin !== pinConfirm) {
      setErrorMessage("Mã PIN nhập lại không khớp. Vui lòng kiểm tra lại.");
      return;
    }
    try {
      const session = await activateMutation.mutateAsync({
        phone: phone.trim(),
        activationCode,
        pin,
      });
      saveToken(session.accessToken);
      setFullName(session.fullName);
      setStep(3);
    } catch (error) {
      setErrorMessage(
        error instanceof ApiError ? error.message : "Kích hoạt thất bại. Vui lòng kiểm tra lại mã kích hoạt."
      );
      setPin("");
      setPinConfirm("");
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col gap-6 px-5 py-6">
      <TopBar title="Kích hoạt tài khoản" showBack />

      <ol className="flex items-center justify-center gap-2" aria-label="Tiến trình kích hoạt">
        {[1, 2, 3].map((s) => (
          <li
            key={s}
            className={`h-2 w-10 rounded-full ${s <= step ? "bg-[--color-primary]" : "bg-[--color-border]"}`}
            aria-current={s === step ? "step" : undefined}
          />
        ))}
      </ol>

      {step === 1 ? (
        <div className="flex flex-col gap-5">
          <h2>Bước 1: Xác thực</h2>
          <p className="text-base text-[--color-text-muted]">
            Nhập số điện thoại và mã kích hoạt được phòng khám cung cấp.
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
          <div>
            <p className="mb-2 text-base font-semibold">Mã kích hoạt ({CODE_LENGTH} số)</p>
            <NumPad value={activationCode} maxLength={CODE_LENGTH} onChange={setActivationCode} label="Mã kích hoạt đã nhập" />
          </div>
          <BigButton onClick={() => setStep(2)} disabled={!step1Valid}>
            Tiếp tục
          </BigButton>
        </div>
      ) : null}

      {step === 2 ? (
        <div className="flex flex-col gap-6">
          <h2>Bước 2: Đặt mã PIN</h2>
          <p className="text-base text-[--color-text-muted]">
            Chọn mã PIN gồm {PIN_LENGTH} số để đăng nhập những lần sau.
          </p>
          <div>
            <p className="mb-2 text-base font-semibold">Mã PIN</p>
            <NumPad value={pin} maxLength={PIN_LENGTH} onChange={setPin} label="Mã PIN đã nhập" />
          </div>
          <div>
            <p className="mb-2 text-base font-semibold">Nhập lại mã PIN</p>
            <NumPad value={pinConfirm} maxLength={PIN_LENGTH} onChange={setPinConfirm} label="Mã PIN nhập lại" />
          </div>
          {errorMessage ? <ErrorNotice message={errorMessage} /> : null}
          <BigButton onClick={handleActivate} disabled={!step2Valid || activateMutation.isPending}>
            {activateMutation.isPending ? "Đang kích hoạt..." : "Hoàn tất kích hoạt"}
          </BigButton>
          <BigButton variant="ghost" onClick={() => setStep(1)}>
            Quay lại bước trước
          </BigButton>
        </div>
      ) : null}

      {step === 3 ? (
        <div className="flex flex-col items-center gap-6 py-10 text-center">
          <span className="text-6xl" aria-hidden="true">
            ✅
          </span>
          <h2>Kích hoạt thành công!</h2>
          <p className="text-lg text-[--color-text-muted]">
            Chào mừng {fullName ?? "bạn"} đến với Cổng bệnh nhân.
          </p>
          <BigButton onClick={() => router.replace("/")}>Vào trang chủ</BigButton>
        </div>
      ) : null}

      {step !== 3 ? (
        <div className="mt-auto flex justify-center pb-4">
          <Link href="/login" className="text-base font-semibold text-[--color-primary] underline underline-offset-4">
            Đã có tài khoản? Đăng nhập
          </Link>
        </div>
      ) : null}
    </main>
  );
}
