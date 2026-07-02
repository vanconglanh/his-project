"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { BhytExportResponse, BhytValidationResult } from "@/lib/api/bhyt-export";
import type { BhytExportStatus } from "@/lib/api/bhyt-export";
import {
  useGenerateBhytXml,
  useValidateBhytXml,
  useSignBhytXml,
  useSubmitBhyt,
} from "@/lib/hooks/use-bhyt-export";
import { BhytSignDialog } from "./BhytSignDialog";
import { toast } from "sonner";
import { CheckCircle2, Circle, Loader2, AlertTriangle } from "lucide-react";

const STATUS_ORDER: BhytExportStatus[] = [
  "DRAFT",
  "GENERATED",
  "VALIDATED",
  "SIGNED",
  "SUBMITTED",
];

function getStatusIndex(status: BhytExportStatus): number {
  const idx = STATUS_ORDER.indexOf(status);
  if (idx === -1) return STATUS_ORDER.length; // APPROVED / REJECTED / PARTIALLY_REJECTED
  return idx;
}

interface StepInfo {
  label: string;
  description: string;
  targetStatus: BhytExportStatus;
}

const STEPS: StepInfo[] = [
  { label: "Tạo nháp", description: "Kỳ export đã được tạo", targetStatus: "DRAFT" },
  { label: "Sinh XML", description: "Sinh Bảng 1-5 từ encounters trong kỳ", targetStatus: "GENERATED" },
  { label: "Validate XSD", description: "Kiểm tra chuẩn QĐ 4750", targetStatus: "VALIDATED" },
  { label: "Ký số", description: "Ký số file XML bằng USB token / chứng thư số", targetStatus: "SIGNED" },
  { label: "Gửi cổng BHYT", description: "Submit lên cổng giám định BHYT", targetStatus: "SUBMITTED" },
];

interface Props {
  exportData: BhytExportResponse;
}

export function BhytExportStepper({ exportData }: Props) {
  const [signOpen, setSignOpen] = useState(false);
  const [validationResult, setValidationResult] = useState<BhytValidationResult | null>(null);

  const generate = useGenerateBhytXml();
  const validate = useValidateBhytXml();
  const sign = useSignBhytXml();
  const submit = useSubmitBhyt();

  const currentIdx = getStatusIndex(exportData.status);
  const isLocked = ["SUBMITTED", "APPROVED", "PARTIALLY_REJECTED", "REJECTED"].includes(exportData.status);

  function handleGenerate() {
    generate.mutate(exportData.id, {
      onSuccess: () => toast.success("Sinh XML thành công"),
      onError: () => toast.error("Sinh XML thất bại"),
    });
  }

  function handleValidate() {
    validate.mutate(exportData.id, {
      onSuccess: (result) => {
        setValidationResult(result);
        if (result.valid) {
          toast.success("Validate XSD thành công");
        } else {
          toast.warning(`Có ${result.errors.length} lỗi XSD`);
        }
      },
      onError: () => toast.error("Validate thất bại"),
    });
  }

  function handleSign(certThumbprint: string, pin: string) {
    sign.mutate(
      { id: exportData.id, cert_thumbprint: certThumbprint, pin },
      {
        onSuccess: () => {
          toast.success("Ký số thành công");
          setSignOpen(false);
        },
        onError: () => toast.error("Ký số thất bại"),
      }
    );
  }

  function handleSubmit() {
    submit.mutate(exportData.id, {
      onSuccess: () => toast.success("Đã gửi lên cổng giám định BHYT"),
      onError: () => toast.error("Gửi cổng BHYT thất bại"),
    });
  }

  return (
    <div className="space-y-4">
      <div className="relative">
        {STEPS.map((step, idx) => {
          const done = currentIdx > idx;
          const active = currentIdx === idx;

          return (
            <div key={step.targetStatus} className="flex gap-4 pb-6 last:pb-0">
              {/* Line */}
              <div className="flex flex-col items-center">
                <div
                  className={cn(
                    "flex h-8 w-8 items-center justify-center rounded-full border-2 shrink-0 text-sm font-medium",
                    done
                      ? "border-green-500 bg-green-500 text-white"
                      : active
                      ? "border-primary bg-primary text-primary-foreground"
                      : "border-muted-foreground/30 text-muted-foreground"
                  )}
                >
                  {done ? <CheckCircle2 className="h-4 w-4" /> : <Circle className="h-4 w-4" />}
                </div>
                {idx < STEPS.length - 1 && (
                  <div className={cn("mt-1 w-0.5 flex-1 min-h-6", done ? "bg-green-500" : "bg-border")} />
                )}
              </div>

              {/* Content */}
              <div className="flex-1 pb-2">
                <p className={cn("text-sm font-medium", active ? "text-foreground" : done ? "text-green-700" : "text-muted-foreground")}>
                  {step.label}
                </p>
                <p className="text-xs text-muted-foreground">{step.description}</p>

                {/* Action buttons */}
                {active && !isLocked && (
                  <div className="mt-3">
                    {step.targetStatus === "GENERATED" && (
                      <Button
                        size="sm"
                        onClick={handleGenerate}
                        disabled={generate.isPending}
                      >
                        {generate.isPending && <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" />}
                        Sinh XML
                      </Button>
                    )}

                    {step.targetStatus === "VALIDATED" && (
                      <div className="space-y-3">
                        <Button
                          size="sm"
                          onClick={handleValidate}
                          disabled={validate.isPending}
                        >
                          {validate.isPending && <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" />}
                          Validate XSD
                        </Button>
                        {validationResult && !validationResult.valid && (
                          <div className="rounded-md border border-destructive/50 bg-destructive/5 p-3 space-y-1 max-h-40 overflow-y-auto">
                            {validationResult.errors.map((err, i) => (
                              <div key={i} className="flex gap-2 text-xs text-destructive">
                                <AlertTriangle className="h-3.5 w-3.5 shrink-0 mt-0.5" />
                                <span>Bảng {err.table_no} dòng {err.row_index} — {err.field}: {err.message}</span>
                              </div>
                            ))}
                          </div>
                        )}
                      </div>
                    )}

                    {step.targetStatus === "SIGNED" && (
                      <>
                        <Button
                          size="sm"
                          className="bg-violet-600 hover:bg-violet-700"
                          onClick={() => setSignOpen(true)}
                        >
                          Ký số
                        </Button>
                        <BhytSignDialog
                          open={signOpen}
                          onOpenChange={setSignOpen}
                          onSign={handleSign}
                          isPending={sign.isPending}
                        />
                      </>
                    )}

                    {step.targetStatus === "SUBMITTED" && (
                      <div className="space-y-2">
                        <Button
                          size="sm"
                          onClick={handleSubmit}
                          disabled={submit.isPending}
                        >
                          {submit.isPending && <Loader2 className="mr-2 h-3.5 w-3.5 animate-spin" />}
                          Gửi cổng BHYT
                        </Button>
                        {exportData.response_message && (
                          <p className="text-xs text-muted-foreground bg-muted rounded p-2">{exportData.response_message}</p>
                        )}
                      </div>
                    )}
                  </div>
                )}
              </div>
            </div>
          );
        })}
      </div>

      {/* Final state */}
      {["APPROVED", "PARTIALLY_REJECTED", "REJECTED"].includes(exportData.status) && (
        <div className={cn(
          "rounded-lg border p-4 text-sm",
          exportData.status === "APPROVED" && "border-green-200 bg-green-50 text-green-800",
          exportData.status === "PARTIALLY_REJECTED" && "border-yellow-200 bg-yellow-50 text-yellow-800",
          exportData.status === "REJECTED" && "border-red-200 bg-red-50 text-red-800",
        )}>
          <p className="font-medium">
            {exportData.status === "APPROVED" && "Kỳ đã được duyệt toàn bộ"}
            {exportData.status === "PARTIALLY_REJECTED" && "Kỳ được duyệt 1 phần — xem tab Đối soát"}
            {exportData.status === "REJECTED" && "Kỳ bị từ chối — xem tab Đối soát để khiếu nại"}
          </p>
          {exportData.response_message && (
            <p className="mt-1 text-xs">{exportData.response_message}</p>
          )}
        </div>
      )}
    </div>
  );
}
