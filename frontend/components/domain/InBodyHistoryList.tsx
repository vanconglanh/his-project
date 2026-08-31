"use client";

import { useState } from "react";
import { Activity, ExternalLink, FileText, Ban } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { useInBodyReports, useCancelInBodyReport } from "@/lib/hooks/use-inbody-reports";
import { formatDateTime } from "@/lib/utils/format";
import { InBodyImportPanel } from "@/components/domain/InBodyImportPanel";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogFooter,
} from "@/components/ui/alert-dialog";
import type { InBodyExtractionStatus, InBodyIndicatorType, InBodyReportResponse } from "@/lib/api/inbody-reports";

const STATUS_LABEL: Record<InBodyExtractionStatus, string> = {
  pending: "Chờ xác nhận",
  success: "Đã xác nhận (đủ)",
  partial: "Đã xác nhận (thiếu)",
  failed: "Không đọc được",
};

const STATUS_VARIANT: Record<InBodyExtractionStatus, "default" | "secondary" | "destructive" | "outline"> = {
  pending: "outline",
  success: "default",
  partial: "secondary",
  failed: "destructive",
};

const INDICATOR_LABEL_SHORT: Record<InBodyIndicatorType, string> = {
  WEIGHT_KG: "Cân nặng",
  BMI: "BMI",
  SMM: "SMM",
  BODY_FAT_MASS: "Khối mỡ",
  PBF: "PBF",
  VISCERAL_FAT: "Mỡ nội tạng",
  TBW: "TBW",
  BMR: "BMR",
  INBODY_SCORE: "Điểm InBody",
};

interface InBodyHistoryListProps {
  patientId: string;
}

export function InBodyHistoryList({ patientId }: InBodyHistoryListProps) {
  const { data, isLoading } = useInBodyReports(patientId);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [cancelTarget, setCancelTarget] = useState<InBodyReportResponse | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const cancelMutation = useCancelInBodyReport(patientId);
  const reports = data?.data ?? [];

  const handleCancelConfirm = () => {
    if (!cancelTarget) return;
    cancelMutation.mutate(
      { id: cancelTarget.id, reason: cancelReason.trim() || undefined },
      {
        onSuccess: () => {
          setCancelTarget(null);
          setCancelReason("");
        },
      }
    );
  };

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-20 w-full" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Dialog open={uploadOpen} onOpenChange={setUploadOpen}>
          <DialogTrigger
            render={
              <Button size="sm" className="gap-1.5">
                <Activity className="h-4 w-4" />
                Nhập kết quả InBody
              </Button>
            }
          />
          <DialogContent className="sm:max-w-xl max-h-[85vh] overflow-y-auto">
            <DialogHeader>
              <DialogTitle>Nhập kết quả máy InBody (PDF)</DialogTitle>
            </DialogHeader>
            <InBodyImportPanel patientId={patientId} onSaved={() => setUploadOpen(false)} />
          </DialogContent>
        </Dialog>
      </div>

      {reports.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground text-sm">
          <FileText className="h-10 w-10 mx-auto mb-2 opacity-30" />
          <p>Chưa có lần đo InBody nào</p>
        </div>
      ) : (
        <div className="space-y-2">
          {reports.map((r) => (
            <div key={r.id} className="border rounded-lg p-3 space-y-2">
              <div className="flex items-center justify-between flex-wrap gap-2">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium">{formatDateTime(r.created_at)}</span>
                  <Badge variant={STATUS_VARIANT[r.extraction_status]}>
                    {STATUS_LABEL[r.extraction_status]}
                  </Badge>
                </div>
                <div className="flex items-center gap-3">
                  {r.file_url && (
                    <a
                      href={r.file_url}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="text-xs text-primary flex items-center gap-1 hover:underline"
                    >
                      Xem file gốc <ExternalLink className="h-3 w-3" />
                    </a>
                  )}
                  <Button
                    variant="ghost"
                    size="sm"
                    className="h-7 px-2 text-xs text-destructive hover:text-destructive"
                    onClick={() => setCancelTarget(r)}
                  >
                    <Ban className="h-3.5 w-3.5 mr-1" />
                    Huỷ
                  </Button>
                </div>
              </div>
              <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-muted-foreground">
                {r.fields
                  .filter((f) => f.value != null)
                  .map((f) => (
                    <span key={f.indicator_type}>
                      {INDICATOR_LABEL_SHORT[f.indicator_type]}: <b className="text-foreground">{f.value}</b> {f.unit}
                    </span>
                  ))}
              </div>
            </div>
          ))}
        </div>
      )}

      <AlertDialog open={!!cancelTarget} onOpenChange={(open) => !open && setCancelTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Huỷ báo cáo InBody nhập nhầm?</AlertDialogTitle>
          </AlertDialogHeader>
          <div className="space-y-1.5">
            <Label htmlFor="inbody-cancel-reason">Lý do huỷ (không bắt buộc)</Label>
            <Textarea
              id="inbody-cancel-reason"
              value={cancelReason}
              onChange={(e) => setCancelReason(e.target.value)}
              placeholder="Vd: nhập nhầm bệnh nhân, đo lại..."
              rows={3}
            />
          </div>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={() => setCancelReason("")}>Đóng</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleCancelConfirm}
              disabled={cancelMutation.isPending}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              {cancelMutation.isPending ? "Đang huỷ..." : "Huỷ báo cáo"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
