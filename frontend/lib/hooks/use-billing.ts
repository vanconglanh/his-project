import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listBillings,
  getBilling,
  createBilling,
  updateBilling,
  addBillingItem,
  removeBillingItem,
  finalizeBilling,
  voidBilling,
  applyBhyt,
  getBillingsByEncounter,
  type BillingListParams,
  type BillingItemUpsert,
  type PayerType,
} from "@/lib/api/billing";

export const BILLING_KEYS = {
  all: ["billings"] as const,
  list: (params?: BillingListParams) => ["billings", "list", params] as const,
  detail: (id: string) => ["billings", id] as const,
  byEncounter: (encounterId: string) => ["billings", "encounter", encounterId] as const,
};

export function useBillings(params?: BillingListParams) {
  return useQuery({
    queryKey: BILLING_KEYS.list(params),
    queryFn: () => listBillings(params),
  });
}

export function useBilling(id: string) {
  return useQuery({
    queryKey: BILLING_KEYS.detail(id),
    queryFn: () => getBilling(id),
    enabled: Boolean(id),
  });
}

export function useBillingsByEncounter(encounterId: string) {
  return useQuery({
    queryKey: BILLING_KEYS.byEncounter(encounterId),
    queryFn: () => getBillingsByEncounter(encounterId),
    enabled: Boolean(encounterId),
  });
}

export function useCreateBilling() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { encounter_id: string; include_dispensing?: boolean; payer?: PayerType; note?: string }) =>
      createBilling(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: BILLING_KEYS.all }),
  });
}

export function useUpdateBilling() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: { note?: string; discount_amount?: number; payment_due_date?: string } }) =>
      updateBilling(id, body),
    onSuccess: (_, { id }) => {
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(id) });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}

export function useAddBillingItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, item }: { id: string; item: BillingItemUpsert }) =>
      addBillingItem(id, item),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(data.id) });
    },
  });
}

export function useRemoveBillingItem() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ itemId, billingId }: { itemId: string; billingId: string }) =>
      removeBillingItem(itemId).then(() => billingId),
    onSuccess: (billingId) => {
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(billingId) });
    },
  });
}

export function useFinalizeBilling() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => finalizeBilling(id),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(data.id) });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}

export function useVoidBilling() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => voidBilling(id, reason),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(data.id) });
      qc.invalidateQueries({ queryKey: BILLING_KEYS.all });
    },
  });
}

export function useApplyBhyt() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: string; body: { bhyt_card_no: string; copay_rate: 80 | 95 | 100; right_route?: "DUNG_TUYEN" | "TRAI_TUYEN" | "CAP_CUU" } }) =>
      applyBhyt(id, body),
    onSuccess: (data) => {
      qc.invalidateQueries({ queryKey: BILLING_KEYS.detail(data.id) });
    },
  });
}
