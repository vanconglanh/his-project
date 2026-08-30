"use client";

import { useState, useEffect } from "react";
import { AlertTriangle } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import type { CccdFieldDiff, CccdComparableField } from "@/lib/api/types";

const FIELD_LABELS: Record<CccdComparableField, string> = {
  full_name: "Họ và tên",
  gender: "Giới tính",
  date_of_birth: "Ngày sinh",
  address: "Địa chỉ",
};

export interface CccdMismatchDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  idNumber: string;
  diffs: CccdFieldDiff[];
  isSaving?: boolean;
  /** Chỉ các field đã tích checkbox */
  onSave: (fields: CccdFieldUpdateSelection[]) => void;
}

export interface CccdFieldUpdateSelection {
  field: CccdComparableField;
  new_value: string;
}

/**
 * US-QR-005 (Case 3 — BR-DUP-004/005): dialog so sánh 4 cột giữa hồ sơ hiện có và dữ liệu
 * quét từ CCCD. Mặc định TẤT CẢ checkbox KHÔNG tích — le tân chủ động chọn field muốn cập nhật.
 */
export function CccdMismatchDialog({
  open,
  onOpenChange,
  idNumber,
  diffs,
  isSaving,
  onSave,
}: CccdMismatchDialogProps) {
  const [checked, setChecked] = useState<Record<string, boolean>>({});

  // Reset lựa chọn mỗi lần dialog mở với dữ liệu mới (mặc định không tích — BR-DUP-004)
  useEffect(() => {
    if (open) setChecked({});
  }, [open, diffs]);

  const toggle = (field: string, value: boolean) => {
    setChecked((prev) => ({ ...prev, [field]: value }));
  };

  const handleSave = () => {
    const selected: CccdFieldUpdateSelection[] = diffs
      .filter((d) => checked[d.field])
      .map((d) => ({ field: d.field, new_value: d.new_value ?? "" }));
    onSave(selected);
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-2xl">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <AlertTriangle className="h-4 w-4 text-amber-500" />
            Phát hiện thông tin có thể đã thay đổi
          </DialogTitle>
          <DialogDescription>
            Số CCCD <span className="font-mono font-medium">{idNumber}</span> đã có hồ sơ trong hệ
            thống. Một số thông tin quét từ CCCD khác với hồ sơ hiện có. Vui lòng kiểm tra và chọn
            trường muốn cập nhật.
          </DialogDescription>
        </DialogHeader>

        <div className="overflow-x-auto rounded-md border">
          <table className="w-full text-sm">
            <thead className="bg-muted/50">
              <tr>
                <th className="px-3 py-2 text-left font-medium">Trường</th>
                <th className="px-3 py-2 text-left font-medium">Dữ liệu hồ sơ hiện có</th>
                <th className="px-3 py-2 text-left font-medium">Dữ liệu quét từ CCCD</th>
                <th className="px-3 py-2 text-center font-medium w-24">Cập nhật</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {diffs.map((d) => (
                <tr key={d.field}>
                  <td className="px-3 py-2 font-medium">{FIELD_LABELS[d.field] ?? d.field}</td>
                  <td className="px-3 py-2 text-muted-foreground">{d.old_value || "—"}</td>
                  <td className="px-3 py-2">{d.new_value || "—"}</td>
                  <td className="px-3 py-2 text-center">
                    <Checkbox
                      aria-label={`Cập nhật trường ${FIELD_LABELS[d.field] ?? d.field}`}
                      checked={!!checked[d.field]}
                      onCheckedChange={(v) => toggle(d.field, v === true)}
                    />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>

        <p className="text-xs text-muted-foreground">
          Mặc định: tất cả checkbox KHÔNG tích — dữ liệu cũ được giữ nguyên.
        </p>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSaving}>
            Thoát — không thay đổi
          </Button>
          <Button type="button" onClick={handleSave} disabled={isSaving}>
            {isSaving ? "Đang lưu..." : "Lưu thay đổi đã chọn"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
