import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listPayments,
  createPayment,
  refundPayment,
  voidPayment,
  generateQr,
  getQrStatus,
  chargeCard,
  type PaymentListParams,
  type PaymentMethod,
} from "@/lib/api/payments";
import { BILLING_KEYS } from "./use-billing";

export const PAYMENT_KEYS = {
  all: ["payments"] as const,
  list: (params?: PaymentListParams) => ["payments", "list", params] as const,
  qrStatus: (qrId: string) => ["payments", "qr", qrId] as const,
};

export function usePayments(params?: PaymentListParams) {
  return useQuery({
    queryKey: PAYMENT_KEYS.list(params),
    queryFn: () => listPayments(params),
  });
}

export function useCreatePayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { billing_id: string; amount: number; method: PaymentMethod; reference?: string; note?: string }) =>
      createPayment(body),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: PAYMENT_KEYS.all });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(data.billing_id) });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}

export function useRefundPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: { amount: number; reason: string } }) =>
      refundPayment(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: PAYMENT_KEYS.all });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}

export function useVoidPayment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) => voidPayment(id, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: PAYMENT_KEYS.all });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}

export function useGenerateQr() {
  return useMutation({
    mutationFn: (body: { billing_id: string; provider: "VIETQR" | "MOMO" | "VNPAY"; amount: number; expires_in_seconds?: number }) =>
      generateQr(body),
  });
}

export function useQrStatusPoll(qrId: string | null, enabled = true) {
  const qc = useQueryClient();
  const query = useQuery({
    queryKey: PAYMENT_KEYS.qrStatus(qrId ?? ""),
    queryFn: () => getQrStatus(qrId!),
    enabled: Boolean(qrId) && enabled,
    refetchInterval: (q) => {
      const status = q.state.data?.status;
      if (status === "PAID" || status === "EXPIRED" || status === "CANCELLED") {
        return false;
      }
      return 3000;
    },
  });

  // Invalidate billings when QR is paid
  if (query.data?.status === "PAID") {
    qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
  }

  return query;
}

export function useChargeCard() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { billing_id: string; amount: number; card_token: string; provider?: "VISA" | "MASTER" }) =>
      chargeCard(body),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: PAYMENT_KEYS.all });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(data.billing_id) });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}
