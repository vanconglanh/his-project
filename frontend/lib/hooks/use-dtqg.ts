import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import {
  submitToDtqg,
  getDtqgStatus,
  retryDtqgSubmission,
  listDtqgSubmissions,
  cancelOnPortal,
  getDtqgCredentials,
  upsertDtqgCredentials,
  testDtqgConnection,
  type DtqgSubmissionListParams,
  type DtqgCredentialsRequest,
} from "../api/dtqg";

export const dtqgKeys = {
  all: ["dtqg"] as const,
  submissions: (params?: DtqgSubmissionListParams) => [...dtqgKeys.all, "submissions", params] as const,
  status: (prescriptionId: string) => [...dtqgKeys.all, "status", prescriptionId] as const,
  credentials: () => [...dtqgKeys.all, "credentials"] as const,
};

export function useDtqgSubmissions(params?: DtqgSubmissionListParams) {
  return useQuery({
    queryKey: dtqgKeys.submissions(params),
    queryFn: () => listDtqgSubmissions(params),
  });
}

export function useDtqgStatus(prescriptionId: string) {
  return useQuery({
    queryKey: dtqgKeys.status(prescriptionId),
    queryFn: () => getDtqgStatus(prescriptionId),
    enabled: !!prescriptionId,
  });
}

export function useDtqgCredentials() {
  return useQuery({
    queryKey: dtqgKeys.credentials(),
    queryFn: getDtqgCredentials,
  });
}

export function useSubmitToDtqg() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (prescriptionId: string) => submitToDtqg(prescriptionId),
    onSuccess: (_, prescriptionId) => {
      qc.invalidateQueries({ queryKey: dtqgKeys.status(prescriptionId) });
      qc.invalidateQueries({ queryKey: dtqgKeys.submissions() });
      toast.success("Đã gửi đơn thuốc lên ĐTQG");
    },
    onError: () => toast.error("Gửi ĐTQG thất bại"),
  });
}

export function useRetryDtqg() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (prescriptionId: string) => retryDtqgSubmission(prescriptionId),
    onSuccess: (_, prescriptionId) => {
      qc.invalidateQueries({ queryKey: dtqgKeys.status(prescriptionId) });
      qc.invalidateQueries({ queryKey: dtqgKeys.submissions() });
      toast.success("Đã gửi lại");
    },
    onError: () => toast.error("Gửi lại thất bại"),
  });
}

export function useCancelOnPortal() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ submissionId, reason }: { submissionId: string; reason: string }) =>
      cancelOnPortal(submissionId, reason),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: dtqgKeys.submissions() });
      toast.success("Đã hủy trên cổng ĐTQG");
    },
    onError: () => toast.error("Hủy thất bại"),
  });
}

export function useUpsertDtqgCredentials() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: DtqgCredentialsRequest) => upsertDtqgCredentials(body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: dtqgKeys.credentials() });
      toast.success("Đã lưu thông tin kết nối ĐTQG");
    },
    onError: () => toast.error("Lưu thất bại"),
  });
}

export function useTestDtqgConnection() {
  return useMutation({
    mutationFn: testDtqgConnection,
    onSuccess: (result) => {
      if (result.ok) {
        toast.success(`Kết nối thành công (${result.latency_ms}ms)`);
      } else {
        toast.error("Kết nối thất bại");
      }
    },
    onError: () => toast.error("Kết nối ĐTQG thất bại"),
  });
}
