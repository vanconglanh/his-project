"use client";

import { useEffect, useState } from "react";
import { Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import type { ReportVisibility } from "@/lib/api/reports";

interface SaveReportDialogProps {
  isEditing: boolean;
  defaultTitle: string;
  defaultVisibility: ReportVisibility;
  disabled?: boolean;
  isSaving: boolean;
  onSave: (title: string, visibility: ReportVisibility) => void;
}

/** Nút "Lưu báo cáo" + Dialog nhập Tên/Phạm vi — Dialog dùng pattern controlled open/onOpenChange chuẩn của dự án. */
export function SaveReportDialog({
  isEditing,
  defaultTitle,
  defaultVisibility,
  disabled,
  isSaving,
  onSave,
}: SaveReportDialogProps) {
  const [open, setOpen] = useState(false);
  const [title, setTitle] = useState(defaultTitle);
  const [visibility, setVisibility] = useState<ReportVisibility>(defaultVisibility);

  useEffect(() => {
    if (open) {
      setTitle(defaultTitle);
      setVisibility(defaultVisibility);
    }
  }, [open, defaultTitle, defaultVisibility]);

  function handleSubmit() {
    if (!title.trim()) return;
    onSave(title.trim(), visibility);
  }

  return (
    <>
      <Button type="button" disabled={disabled} onClick={() => setOpen(true)} className="gap-1.5">
        <Save className="h-4 w-4" />
        {isEditing ? "Lưu thay đổi" : "Lưu báo cáo"}
      </Button>

      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{isEditing ? "Cập nhật báo cáo" : "Lưu báo cáo mới"}</DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="report-title">Tên báo cáo</Label>
              <Input
                id="report-title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="VD: Doanh thu theo bác sĩ tháng này"
                autoFocus
              />
            </div>

            <div className="space-y-1.5">
              <Label>Phạm vi hiển thị</Label>
              <RadioGroup value={visibility} onValueChange={(v) => v && setVisibility(v as ReportVisibility)} className="space-y-2">
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="TENANT" />
                  Cả phòng khám (mọi người có quyền đều xem được)
                </label>
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="PRIVATE" />
                  Chỉ mình tôi
                </label>
              </RadioGroup>
            </div>
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setOpen(false)} disabled={isSaving}>
              Huỷ
            </Button>
            <Button type="button" onClick={handleSubmit} disabled={!title.trim() || isSaving}>
              {isSaving ? "Đang lưu..." : "Lưu"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
}
