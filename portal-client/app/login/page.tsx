"use client";

import { useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { BigButton } from "@/components/BigButton";
import { NumPad } from "@/components/NumPad";
import { ErrorNotice } from "@/components/ErrorNotice";
import { saveToken } from "@/lib/auth";
import { ApiError } from "@/lib/api";
import { useLoginPin, useTenantInfo } from "@/lib/queries";

const PIN_LENGTH = 6;

const ERROR_MESSAGES: Record<string, string> = {
  PORTAL_PHONE_NOT_REGISTERED: "Số điện thoại chưa được đăng ký. Vui lòng liên hệ phòng khám.",
  PORTAL_NOT_ACTIVATED: "Tài khoản chưa kích hoạt. Vui lòng bấm \"Kích hoạt tài khoản\" bên dưới.",
  PORTAL_ACCOUNT_LOCKED: "Tài khoản đã bị khoá do nhập sai nhiều lần. Vui lòng thử lại sau.",
  PORTAL_PIN_INVALID: "Mã PIN không đúng. Vui lòng kiểm tra lại.",
};

export default function LoginPage() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const redirectTo = searchParams.get("redirect") || "/";

  const { data: tenant } = useTenantInfo();
  const loginMutation = useLoginPin();

  const [phone, setPhone] = useState("");
  const [pin, setPin] = useState("");
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const canSubmit = phone.trim().length >= 9 && pin.length === PIN_LENGTH && !loginMutation.isPending;

  async function handleSubmit() {
    setErrorMessage(null);
    try {
      const session = await loginMutation.mutateAsync({ phone: phone.trim(), pin });
      saveToken(session.accessToken);
      router.replace(redirectTo);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(ERROR_MESSAGES[error.code] ?? error.message);
      } else {
        setErrorMessage("Không thể đăng nhập. Vui lòng thử lại.");
      }
      setPin("");
    }
  }

  return (
    <main className="mx-auto flex min-h-screen max-w-md flex-col justify-center gap-6 px-5 py-10">
      <div className="flex flex-col items-center gap-3 text-center">
        {tenant?.logoUrl ? (
          // eslint-disable-next-line @next/next/no-img-element -- domain logo phòng khám không cố định trước, dùng img thường để tránh cấu hình remotePatterns
          <img
            src={tenant.logoUrl}
            alt={tenant.name}
            width={72}
            height={72}
            className="rounded-2xl object-contain"
          />
        ) : (
          <div className="flex h-18 w-18 items-center justify-center rounded-2xl bg-[--color-primary-soft] text-4xl">
            🏥
          </div>
        )}
        <h1 className="text-2xl font-bold">{tenant?.name ?? "Cổng bệnh nhân"}</h1>
        <p className="text-base text-[--color-text-muted]">Đăng nhập bằng số điện thoại và mã PIN</p>
      </div>

      <div className="flex flex-col gap-2">
        <label htmlFor="phone" className="text-base font-semibold">
          Số điện thoại
        </label>
        <input
          id="phone"
          type="tel"
          inputMode="tel"
          autoComplete="tel"
          placeholder="VD: 0912345678"
          value={phone}
          onChange={(event) => setPhone(event.target.value)}
          className="min-h-[56px] rounded-2xl border-2 border-[--color-border] bg-white px-4 text-lg focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
        />
      </div>

      <div className="flex flex-col gap-2">
        <p className="text-base font-semibold">Mã PIN ({PIN_LENGTH} số)</p>
        <NumPad value={pin} maxLength={PIN_LENGTH} onChange={setPin} label="Mã PIN đã nhập" />
      </div>

      {errorMessage ? <ErrorNotice message={errorMessage} /> : null}

      <BigButton onClick={handleSubmit} disabled={!canSubmit}>
        {loginMutation.isPending ? "Đang đăng nhập..." : "Đăng nhập"}
      </BigButton>

      <div className="flex flex-col items-center gap-3 text-base">
        <Link href="/reset-pin" className="font-semibold text-[--color-primary] underline underline-offset-4">
          Quên mã PIN?
        </Link>
        <Link href="/activate" className="font-semibold text-[--color-primary] underline underline-offset-4">
          Kích hoạt tài khoản mới
        </Link>
      </div>
    </main>
  );
}
