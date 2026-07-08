"use client";

import Link from "next/link";
import { useState } from "react";
import { BigButton } from "@/components/BigButton";
import { CalendarIcon } from "@/components/icons";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { ApiRequestError } from "@/lib/api";
import { useAppointments, useCancelAppointmentMutation } from "@/lib/hooks";
import { formatDateTime } from "@/lib/utils";

const STATUS_LABEL: Record<string, string> = {
  scheduled: "Đã đặt",
  confirmed: "Đã xác nhận",
  done: "Đã khám",
  cancelled: "Đã hủy",
};

export default function AppointmentsPage() {
  const { data: appointments, isLoading, isError, refetch } = useAppointments();
  const cancelMutation = useCancelAppointmentMutation();
  const [cancelTarget, setCancelTarget] = useState<string | null>(null);
  const [cancelError, setCancelError] = useState<string | null>(null);

  function handleConfirmCancel() {
    if (!cancelTarget) return;
    setCancelError(null);
    cancelMutation.mutate(cancelTarget, {
      onSuccess: () => setCancelTarget(null),
      onError: (err) => {
        setCancelError(
          err instanceof ApiRequestError ? err.message : "Không hủy được lịch hẹn, vui lòng thử lại",
        );
      },
    });
  }

  return (
    <div className="p-4">
      <div className="mb-5 flex items-center justify-between pt-4">
        <h1 className="text-slate-900">Lịch hẹn của tôi</h1>
      </div>

      <Link href="/appointments/new" className="mb-5 block">
        <BigButton>+ Đặt lịch khám mới</BigButton>
      </Link>

      {cancelError && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {cancelError}
        </div>
      )}

      {isLoading && <LoadingBlock label="Đang tải lịch hẹn..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {!isLoading && !isError && (appointments?.length ?? 0) === 0 && (
        <EmptyState
          icon={<CalendarIcon className="h-16 w-16" />}
          title="Chưa có lịch hẹn nào"
          description="Đặt lịch khám để được phục vụ nhanh hơn"
        />
      )}

      <div className="flex flex-col gap-3">
        {appointments?.map((a, idx) => (
          <div key={`${a.id}-${idx}`} className="rounded-2xl border-2 border-slate-200 bg-white p-4">
            <div className="mb-1 flex items-center justify-between">
              <span className="text-lg font-semibold text-slate-900">
                {formatDateTime(a.appointmentAt)}
              </span>
              <span className="rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-600">
                {STATUS_LABEL[a.status] ?? a.status}
              </span>
            </div>
            <p className="mb-3 text-base text-slate-600">{a.doctorName}</p>
            {a.status !== "cancelled" && a.status !== "done" && (
              <button
                type="button"
                onClick={() => setCancelTarget(a.id)}
                className="min-h-11 rounded-xl border-2 border-red-200 px-4 text-base font-semibold text-red-600 hover:bg-red-50"
              >
                Hủy lịch hẹn
              </button>
            )}
          </div>
        ))}
      </div>

      <ConfirmDialog
        open={Boolean(cancelTarget)}
        title="Hủy lịch hẹn?"
        description="Bạn có chắc muốn hủy lịch hẹn này không?"
        confirmLabel="Hủy lịch hẹn"
        cancelLabel="Không, giữ lại"
        loading={cancelMutation.isPending}
        onConfirm={handleConfirmCancel}
        onCancel={() => setCancelTarget(null)}
      />
    </div>
  );
}
