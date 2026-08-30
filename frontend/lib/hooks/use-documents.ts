"use client";

import { useMutation } from "@tanstack/react-query";
import { smartUploadDocument, type SmartUploadParams, type SmartUploadResponse } from "@/lib/api/documents";
import { getErrorMessage } from "@/lib/utils/errors";
import { toast } from "sonner";

export function useSmartUploadDocument() {
  return useMutation<SmartUploadResponse, unknown, SmartUploadParams>({
    mutationFn: (params) => smartUploadDocument(params),
    onError: (e) => toast.error(getErrorMessage(e, "Không phân tích được tài liệu")),
  });
}
