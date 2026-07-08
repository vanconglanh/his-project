"use client";

import { BigButton } from "@/components/BigButton";

interface ConfirmDialogProps {
  open: boolean;
  title: string;
  description?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  destructive?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({
  open,
  title,
  description,
  confirmLabel = "Xác nhận",
  cancelLabel = "Huỷ bỏ",
  destructive = true,
  onConfirm,
  onCancel,
}: ConfirmDialogProps) {
  if (!open) return null;

  return (
    <div
      role="dialog"
      aria-modal="true"
      aria-labelledby="confirm-dialog-title"
      className="fixed inset-0 z-50 flex items-end justify-center bg-black/50 p-4 sm:items-center"
    >
      <div className="w-full max-w-sm rounded-3xl bg-white p-6 shadow-xl">
        <h2 id="confirm-dialog-title" className="text-xl font-bold text-[--color-text]">
          {title}
        </h2>
        {description ? <p className="mt-2 text-base text-[--color-text-muted]">{description}</p> : null}
        <div className="mt-6 flex flex-col gap-3">
          <BigButton variant={destructive ? "danger" : "primary"} onClick={onConfirm}>
            {confirmLabel}
          </BigButton>
          <BigButton variant="ghost" onClick={onCancel}>
            {cancelLabel}
          </BigButton>
        </div>
      </div>
    </div>
  );
}
