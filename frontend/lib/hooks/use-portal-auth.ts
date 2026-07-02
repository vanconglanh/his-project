"use client";

import { useState, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { toast } from "sonner";
import { portalRequestOtp, portalVerifyOtp, portalLogout } from "@/lib/api/portal";
import type { PortalAuthOtpRequest, PortalVerifyRequest } from "@/lib/api/portal";

const PORTAL_TOKEN_KEY = "portal-token";
const PORTAL_USER_KEY = "portal-user";

export function usePortalAuth() {
  const router = useRouter();
  const [step, setStep] = useState<"phone" | "otp">("phone");
  const [phone, setPhone] = useState("");

  const requestOtpMutation = useMutation({
    mutationFn: (body: PortalAuthOtpRequest) => portalRequestOtp(body),
    onSuccess: () => {
      setStep("otp");
      toast.success("Mã OTP đã được gửi đến số điện thoại của bạn");
    },
    onError: (err: { response?: { data?: { error?: { code?: string; message?: string } } } }) => {
      const code = err?.response?.data?.error?.code;
      if (code === "PORTAL_PHONE_NOT_REGISTERED") {
        toast.error("Số điện thoại chưa đăng ký");
      } else if (code === "PORTAL_OTP_TOO_MANY_ATTEMPTS") {
        toast.error("Quá nhiều yêu cầu OTP, thử lại sau 1 giờ");
      } else {
        toast.error("Gửi OTP thất bại, vui lòng thử lại");
      }
    },
  });

  const verifyOtpMutation = useMutation({
    mutationFn: (body: PortalVerifyRequest) => portalVerifyOtp(body),
    onSuccess: (data) => {
      if (typeof window !== "undefined") {
        localStorage.setItem(PORTAL_TOKEN_KEY, data.access_token);
        localStorage.setItem(
          PORTAL_USER_KEY,
          JSON.stringify({ patient_code: data.patient_code, full_name: data.full_name })
        );
      }
      router.push("/portal/me");
    },
    onError: (err: { response?: { data?: { error?: { code?: string } }; status?: number } }) => {
      const code = err?.response?.data?.error?.code;
      const status = err?.response?.status;
      if (code === "PORTAL_OTP_EXPIRED" || status === 410) {
        toast.error("OTP đã hết hạn, vui lòng yêu cầu mã mới");
      } else {
        toast.error("Mã OTP không đúng");
      }
    },
  });

  const logout = useCallback(async () => {
    try {
      await portalLogout();
    } catch {
      // ignore
    }
    if (typeof window !== "undefined") {
      localStorage.removeItem(PORTAL_TOKEN_KEY);
      localStorage.removeItem(PORTAL_USER_KEY);
    }
    router.push("/portal/login");
  }, [router]);

  const getPortalUser = useCallback(() => {
    if (typeof window === "undefined") return null;
    const raw = localStorage.getItem(PORTAL_USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as { patient_code: string; full_name: string };
    } catch {
      return null;
    }
  }, []);

  const isAuthenticated = useCallback(() => {
    if (typeof window === "undefined") return false;
    return !!localStorage.getItem(PORTAL_TOKEN_KEY);
  }, []);

  return {
    step,
    phone,
    setPhone,
    setStep,
    requestOtp: (body: PortalAuthOtpRequest) => requestOtpMutation.mutate(body),
    verifyOtp: (body: PortalVerifyRequest) => verifyOtpMutation.mutate(body),
    isRequestingOtp: requestOtpMutation.isPending,
    isVerifyingOtp: verifyOtpMutation.isPending,
    logout,
    getPortalUser,
    isAuthenticated,
  };
}

export function usePortalUser() {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem("portal-user");
  if (!raw) return null;
  try {
    return JSON.parse(raw) as { patient_code: string; full_name: string };
  } catch {
    return null;
  }
}
