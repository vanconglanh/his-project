"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { useCdssOverride } from "@/lib/hooks/use-cdss";
import type { CdssAlertResponse } from "@/lib/api/types";

interface Props {
  open: boolean;
  onClose: () => void;
  alert: CdssAlertResponse | null;
  patientId?: string;
  encounterId?: string;
  prescriptionId?: string;
  onOverridden: (alert: CdssAlertResponse) => void;
}

export function CdssOverrideModal({ open, onClose, alert, encounterId, prescriptionId, onOverridden }: Props) {
  const [reason, setReason] = useState("");
  const override = useCdssOverride();

  function handleClose() {
    setReason("");
    onClose();
  }

  function handleSubmit() {
    if (!alert || reason.trim().length === 0) return;
    override.mutate(
      {
        prescription_id: prescriptionId,
        encounter_id: encounterId,
        rule_type: alert.rule_type,
        rule_code: alert.rule_code,
        severity: alert.severity,
        override_reason: reason.trim(),
      },
      {
        onSuccess: () => {
          onOverridden(alert);
          handleClose();
        },
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={(o) => !o && handleClose()}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Bỏ qua cảnh báo CDSS</DialogTitle>
          <DialogDescription>
            Vui lòng nhập lý do bỏ qua cảnh báo &quot;{alert?.title}&quot;. Lý do sẽ được ghi lại trong nhật ký hệ
            thống.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2">
          <Label htmlFor="override-reason">
            Lý do bỏ qua <span className="text-destructive">*</span>
          </Label>
          <Textarea
            id="override-reason"
            rows={4}
            placeholder="Vd: Đã cân nhắc lợi ích/nguy cơ, theo dõi sát bệnh nhân..."
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            aria-required="true"
          />
          {reason.trim().length === 0 && (
            <p className="text-xs text-muted-foreground">Bắt buộc nhập lý do trước khi bỏ qua cảnh báo</p>
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={handleClose} disabled={override.isPending}>
            Huỷ
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={reason.trim().length === 0 || override.isPending}
            variant="destructive"
          >
            {override.isPending ? "Đang lưu..." : "Xác nhận bỏ qua"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
