import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  getDispenseQueue,
  dispensePrescription,
  rejectDispense,
  returnDispense,
  listDispenseHistory,
  type DispenseRequest,
} from "../api/pharmacy-dispensing";
import { prescriptionKeys } from "./use-prescriptions";
import { stockKeys } from "./use-pharmacy-warehouse";

export const dispenseKeys = {
  all: ["dispense"] as const,
  queue: (params?: object) => [...dispenseKeys.all, "queue", params] as const,
  history: (params?: object) => [...dispenseKeys.all, "history", params] as const,
};

export function useDispenseQueue(params?: { warehouse_id?: string; q?: string; page?: number; page_size?: number }) {
  return useQuery({
    queryKey: dispenseKeys.queue(params),
    queryFn: () => getDispenseQueue(params),
    refetchInterval: 10_000,
  });
}

export function useDispenseHistory(params?: {
  patient_id?: string;
  from_date?: string;
  to_date?: string;
  status?: string;
  page?: number;
  page_size?: number;
}) {
  return useQuery({
    queryKey: dispenseKeys.history(params),
    queryFn: () => listDispenseHistory(params),
  });
}

export function useDispensePrescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ prescriptionId, body }: { prescriptionId: string; body: DispenseRequest }) =>
      dispensePrescription(prescriptionId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: dispenseKeys.all });
      qc.invalidateQueries({ queryKey: prescriptionKeys.all });
      qc.invalidateQueries({ queryKey: stockKeys.all });
      toast.success("Phát thuốc thành công");
    },
    onError: () => toast.error("Phát thuốc thất bại"),
  });
}

export function useRejectDispense() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) => rejectDispense(id, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: dispenseKeys.all });
      toast.success("Đã từ chối phát thuốc");
    },
    onError: () => toast.error("Từ chối thất bại"),
  });
}

export function useReturnDispense() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      body,
    }: {
      id: string;
      body: { reason: string; items: Array<{ dispense_item_id: string; quantity: number }> };
    }) => returnDispense(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: dispenseKeys.all });
      qc.invalidateQueries({ queryKey: stockKeys.all });
      toast.success("Hoàn trả thuốc thành công");
    },
    onError: () => toast.error("Hoàn trả thất bại"),
  });
}
