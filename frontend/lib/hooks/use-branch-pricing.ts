import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { isAxiosError } from "axios";
import { toast } from "sonner";
import {
  listServicePriceOverrides,
  createServicePriceOverride,
  updateServicePriceOverride,
  deleteServicePriceOverride,
  listDrugPriceOverrides,
  createDrugPriceOverride,
  updateDrugPriceOverride,
  deleteDrugPriceOverride,
  type ServicePriceOverrideListParams,
  type ServicePriceOverrideCreateRequest,
  type DrugPriceOverrideListParams,
  type DrugPriceOverrideCreateRequest,
  type PriceOverrideUpdateRequest,
} from "../api/branch-pricing";

/** Thông báo lỗi thân thiện, ưu tiên message từ envelope { error: { code, message } }. */
function getErrorMessage(error: unknown, fallback: string): string {
  if (isAxiosError(error)) {
    const status = error.response?.status;
    const apiError = error.response?.data?.error as
      | { code?: string; message?: string }
      | undefined;
    if (status === 409 || apiError?.code === "PRICE_OVERLAP") {
      return (
        apiError?.message ??
        "Đã có giá override cho item này trong khoảng thời gian giao nhau. Vui lòng chọn khoảng hiệu lực khác."
      );
    }
    if (apiError?.message) return apiError.message;
  }
  return fallback;
}

export const servicePriceOverrideKeys = {
  all: ["service-price-overrides"] as const,
  list: (params?: ServicePriceOverrideListParams) =>
    [...servicePriceOverrideKeys.all, "list", params] as const,
};

export const drugPriceOverrideKeys = {
  all: ["drug-price-overrides"] as const,
  list: (params?: DrugPriceOverrideListParams) =>
    [...drugPriceOverrideKeys.all, "list", params] as const,
};

// ─── Dịch vụ ────────────────────────────────────────────────────────────────

export function useServicePriceOverrides(params?: ServicePriceOverrideListParams) {
  return useQuery({
    queryKey: servicePriceOverrideKeys.list(params),
    queryFn: () => listServicePriceOverrides(params),
  });
}

export function useCreateServicePriceOverride() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: ServicePriceOverrideCreateRequest) => createServicePriceOverride(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: servicePriceOverrideKeys.all });
      toast.success("Đã thêm override giá dịch vụ");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Thêm override giá dịch vụ thất bại")),
  });
}

export function useUpdateServicePriceOverride(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: PriceOverrideUpdateRequest) => updateServicePriceOverride(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: servicePriceOverrideKeys.all });
      toast.success("Đã cập nhật override giá dịch vụ");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Cập nhật override giá dịch vụ thất bại")),
  });
}

export function useDeleteServicePriceOverride() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteServicePriceOverride(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: servicePriceOverrideKeys.all });
      toast.success("Đã xoá override giá dịch vụ");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Xoá override giá dịch vụ thất bại")),
  });
}

// ─── Thuốc ──────────────────────────────────────────────────────────────────

export function useDrugPriceOverrides(params?: DrugPriceOverrideListParams) {
  return useQuery({
    queryKey: drugPriceOverrideKeys.list(params),
    queryFn: () => listDrugPriceOverrides(params),
  });
}

export function useCreateDrugPriceOverride() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DrugPriceOverrideCreateRequest) => createDrugPriceOverride(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: drugPriceOverrideKeys.all });
      toast.success("Đã thêm override giá thuốc");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Thêm override giá thuốc thất bại")),
  });
}

export function useUpdateDrugPriceOverride(id: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: PriceOverrideUpdateRequest) => updateDrugPriceOverride(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: drugPriceOverrideKeys.all });
      toast.success("Đã cập nhật override giá thuốc");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Cập nhật override giá thuốc thất bại")),
  });
}

export function useDeleteDrugPriceOverride() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteDrugPriceOverride(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: drugPriceOverrideKeys.all });
      toast.success("Đã xoá override giá thuốc");
    },
    onError: (error) => toast.error(getErrorMessage(error, "Xoá override giá thuốc thất bại")),
  });
}
