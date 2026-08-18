"use client";

import { useState } from "react";
import { Loader2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const SECTIONS: Record<string, string> = {
  CLINICAL_NOTE: "Nội dung bệnh án",
  DIAGNOSIS: "Chẩn đoán",
  PRESCRIPTION: "Đơn thuốc",
  VITAL_SIGN: "Sinh hiệu",
  CLS_ORDER: "Chỉ định cận lâm sàng",
  OTHER: "Khác",
};

export interface EncounterAmendDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  isPending?: boolean;
  onSubmit: (body: { section: string; reason: string; content_after?: unknown }) => void;
}

export function EncounterAmendDialog({
  open,
  onOpenChange,
  isPending,
  onSubmit,
}: EncounterAmendDialogProps) {
  const [section, setSection] = useState("CLINICAL_NOTE");
  const [reason, setReason] = useState("");
  const [content, setContent] = useState("");

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-xl">
        <DialogHeader>
          <DialogTitle>Tạo bản đính chính</DialogTitle>
          <DialogDescription>
            Bệnh án đã khoá. Nội dung gốc được giữ nguyên, thay đổi ghi thành bản đính chính có lý do.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1">
            <Label htmlFor="amend-section">Phần cần đính chính</Label>
            <Select items={SECTIONS} value={section} onValueChange={(v) => setSection(String(v))}>
              <SelectTrigger id="amend-section" className="min-h-[44px]">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(SECTIONS).map(([value, label]) => (
                  <SelectItem key={value} value={value}>
                    {label}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1">
            <Label htmlFor="amend-content">Nội dung đính chính</Label>
            <Textarea
              id="amend-content"
              value={content}
              onChange={(e) => setContent(e.target.value)}
              placeholder="Nội dung đúng sau khi đính chính..."
              rows={4}
            />
          </div>

          <div className="space-y-1">
            <Label htmlFor="amend-reason">Lý do đính chính (bắt buộc)</Label>
            <Textarea
              id="amend-reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Ví dụ: nhập nhầm mã ICD-10 khi kết thúc khám."
              rows={2}
              required
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Huỷ
          </Button>
          <Button
            className="gap-2"
            disabled={!reason.trim() || isPending}
            onClick={() => {
              onSubmit({
                section,
                reason: reason.trim(),
                content_after: content.trim() ? { text: content.trim() } : undefined,
              });
              setReason("");
              setContent("");
            }}
          >
            {isPending && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
            {isPending ? "Đang lưu…" : "Lưu bản đính chính"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
