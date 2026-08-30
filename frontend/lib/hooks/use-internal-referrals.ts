import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  createInternalReferral,
  listIncomingInternalReferrals,
  updateInternalReferralStatus,
  type CreateInternalReferralRequest,
  type UpdateInternalReferralStatusRequest,
} from "../api/internal-referrals";

export const internalReferralKeys = {
  all: ["internal-referrals"] as const,
  incoming: (status?: string) => [...internalReferralKeys.all, "incoming", status] as const,
};

export function useIncomingInternalReferrals(status?: string) {
  return useQuery({
    queryKey: internalReferralKeys.incoming(status),
    queryFn: () => listIncomingInternalReferrals(status),
  });
}

export function useCreateInternalReferral() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateInternalReferralRequest) => createInternalReferral(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: internalReferralKeys.all });
      toast.success("Đã tạo giấy giới thiệu chuyển cơ sở");
    },
    onError: () => toast.error("Tạo giấy giới thiệu thất bại"),
  });
}

export function useUpdateInternalReferralStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      body,
    }: {
      id: number | string;
      body: UpdateInternalReferralStatusRequest;
    }) => updateInternalReferralStatus(id, body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: internalReferralKeys.all });
      toast.success("Đã cập nhật trạng thái");
    },
    onError: () => toast.error("Cập nhật trạng thái thất bại"),
  });
}
