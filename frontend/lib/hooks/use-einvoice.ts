import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
  listEInvoices,
  getEInvoice,
  issueEInvoice,
  cancelEInvoice,
  type EInvoiceListParams,
  type EInvoiceProvider,
} from "@/lib/api/einvoice";

export const EINVOICE_KEYS = {
  all: ["einvoices"] as const,
  list: (params?: EInvoiceListParams) => ["einvoices", "list", params] as const,
  detail: (id: string) => ["einvoices", id] as const,
};

export function useEInvoices(params?: EInvoiceListParams) {
  return useQuery({
    queryKey: EINVOICE_KEYS.list(params),
    queryFn: () => listEInvoices(params),
  });
}

export function useEInvoice(id: string) {
  return useQuery({
    queryKey: EINVOICE_KEYS.detail(id),
    queryFn: () => getEInvoice(id),
    enabled: Boolean(id),
  });
}

export function useIssueEInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: {
      billing_id: string;
      provider: EInvoiceProvider;
      buyer?: { name?: string; tax_code?: string | null; address?: string | null; email?: string | null; phone?: string | null };
      send_email?: boolean;
    }) => issueEInvoice(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: EINVOICE_KEYS.all }),
  });
}

export function useCancelEInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => cancelEInvoice(id, reason),
    onSuccess: () => qc.invalidateQueries({ queryKey: EINVOICE_KEYS.all }),
  });
}
