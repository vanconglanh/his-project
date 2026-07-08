"use client";

import { BigButton } from "@/components/BigButton";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  loading?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

/** Hộp thoại xác nhận cho hành động có tính phá hủy (hủy lịch hẹn, tắt nhắc thuốc...) */
export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = "Xác nhận",
  cancelLabel = "Đóng",
  loading,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  if (!open) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/40 sm:items-center"
    >
      <div className="w-full max-w-sm rounded-t-3xl bg-white p-6 sm:rounded-3xl">
        <h2 id="confirm-dialog-title" className="mb-2 text-slate-900">
          {title}
        </h2>
        {description && <p className="mb-5 text-base text-slate-600">{description}</p>}
        <div className="flex flex-col gap-3">
          <BigButton variant="danger" onClick={onConfirm} disabled={loading}>
            {loading ? "Đang xử lý..." : confirmLabel}
          </BigButton>
          <BigButton variant="secondary" onClick={onCancel} disabled={loading}>
            {cancelLabel}
          </BigButton>
        </div>
      </div>
    </div>
  );
}
