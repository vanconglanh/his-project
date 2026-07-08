"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import * as aiSuggestionApi from "@/lib/api/ai-suggestion";
import type { TreatmentSuggestionRequest, UpdateAiSuggestionStatusRequest } from "@/lib/api/types";

export function useGenerateTreatmentSuggestion(patientId: string) {
  return useMutation({
    mutationFn: (body?: TreatmentSuggestionRequest) =>
      aiSuggestionApi.generateTreatmentSuggestion(patientId, body),
    onError: () => toast.error("Không thể tạo gợi ý điều trị"),
  });
}

export function useUpdateAiSuggestionStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ logId, body }: { logId: string; body: UpdateAiSuggestionStatusRequest }) =>
      aiSuggestionApi.updateAiSuggestionStatus(logId, body),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ["ai-suggestion", variables.logId] });
      toast.success("Đã cập nhật trạng thái gợi ý");
    },
    onError: () => toast.error("Cập nhật trạng thái gợi ý thất bại"),
  });
}
