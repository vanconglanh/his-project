import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  listPrescriptions,
  getPrescription,
  createPrescription,
  updatePrescription,
  deletePrescription,
  addPrescriptionItems,
  removePrescriptionItem,
  signPrescription,
  cancelPrescription,
  getDdiCheck,
  type PrescriptionListParams,
  type PrescriptionCreateRequest,
  type PrescriptionItemRequest,
} from "../api/prescriptions";

export const prescriptionKeys = {
  all: ["prescriptions"] as const,
  list: (params?: PrescriptionListParams) => [...prescriptionKeys.all, "list", params] as const,
  detail: (id: string) => [...prescriptionKeys.all, "detail", id] as const,
  ddi: (id: string) => [...prescriptionKeys.all, "ddi", id] as const,
};

export function usePrescriptions(params?: PrescriptionListParams) {
  return useQuery({
    queryKey: prescriptionKeys.list(params),
    queryFn: () => listPrescriptions(params),
  });
}

export function usePrescription(id: string) {
  return useQuery({
    queryKey: prescriptionKeys.detail(id),
    queryFn: () => getPrescription(id),
    enabled: !!id,
  });
}

export function useDdiCheck(id: string) {
  return useQuery({
    queryKey: prescriptionKeys.ddi(id),
    queryFn: () => getDdiCheck(id),
    enabled: !!id,
  });
}

export function useCreatePrescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: PrescriptionCreateRequest) => createPrescription(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.all });
      toast.success("Đã tạo đơn thuốc nháp");
    },
    onError: () => toast.error("Tạo đơn thuốc thất bại"),
  });
}

export function useUpdatePrescription(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { note?: string }) => updatePrescription(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.detail(id) });
      toast.success("Đã cập nhật đơn thuốc");
    },
    onError: () => toast.error("Cập nhật thất bại"),
  });
}

export function useDeletePrescription() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deletePrescription(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.all });
      toast.success("Đã xóa đơn thuốc");
    },
    onError: () => toast.error("Xóa thất bại"),
  });
}

export function useAddPrescriptionItems(prescriptionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (items: PrescriptionItemRequest[]) => addPrescriptionItems(prescriptionId, items),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.detail(prescriptionId) });
      qc.invalidateQueries({ queryKey: prescriptionKeys.ddi(prescriptionId) });
    },
    onError: () => toast.error("Thêm thuốc thất bại"),
  });
}

export function useRemovePrescriptionItem(prescriptionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (itemId: string) => removePrescriptionItem(prescriptionId, itemId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.detail(prescriptionId) });
      qc.invalidateQueries({ queryKey: prescriptionKeys.ddi(prescriptionId) });
    },
    onError: () => toast.error("Xóa thuốc thất bại"),
  });
}

export function useSignPrescription(prescriptionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: { signature_data: string; certificate_thumbprint: string; signing_time?: string }) =>
      signPrescription(prescriptionId, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.detail(prescriptionId) });
      qc.invalidateQueries({ queryKey: prescriptionKeys.all });
      toast.success("Ký số đơn thuốc thành công");
    },
    onError: () => toast.error("Ký số thất bại"),
  });
}

export function useCancelPrescription(prescriptionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (reason: string) => cancelPrescription(prescriptionId, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: prescriptionKeys.detail(prescriptionId) });
      qc.invalidateQueries({ queryKey: prescriptionKeys.all });
      toast.success("Đã hủy đơn thuốc");
    },
    onError: () => toast.error("Hủy đơn thất bại"),
  });
}
