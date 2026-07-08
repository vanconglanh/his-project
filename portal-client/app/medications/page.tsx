"use client";

import { useState } from "react";
import { PillIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { usePrescriptions, useMedReminders, useEnableReminderFromPrescription, useToggleReminder } from "@/lib/hooks";
import type { MedReminder } from "@/lib/types";
import { ApiRequestError } from "@/lib/api";

const SLOT_ORDER = ["Sáng", "Trưa", "Chiều", "Tối"];

function groupBySlot(reminders: MedReminder[]): Record<string, MedReminder[]> {
  const groups: Record<string, MedReminder[]> = {};
  for (const slot of SLOT_ORDER) groups[slot] = [];
  for (const r of reminders) {
    const key = SLOT_ORDER.includes(r.timeSlot) ? r.timeSlot : "Khác";
    if (!groups[key]) groups[key] = [];
    groups[key].push(r);
  }
  return groups;
}

export default function MedicationsPage() {
  const { data: reminders, isLoading, isError, refetch } = useMedReminders();
  const { data: prescriptions } = usePrescriptions();
  const enableFromPrescription = useEnableReminderFromPrescription();
  const toggleReminder = useToggleReminder();
  const [error, setError] = useState<string | null>(null);

  const grouped = groupBySlot(reminders ?? []);
  const latestPrescription = prescriptions?.[0];

  function handleToggle(id: string, enabled: boolean) {
    setError(null);
    toggleReminder.mutate(
      { id, enabled },
      {
        onError: (err) =>
          setError(err instanceof ApiRequestError ? err.message : "Không cập nhật được nhắc thuốc"),
      },
    );
  }

  function handleEnableFromLatest() {
    if (!latestPrescription) return;
    setError(null);
    enableFromPrescription.mutate(latestPrescription.id, {
      onError: (err) =>
        setError(err instanceof ApiRequestError ? err.message : "Không bật được nhắc thuốc"),
    });
  }

  return (
    <div className="p-4">
      <div className="mb-5 flex items-center justify-between pt-4">
        <h1 className="text-slate-900">Lịch uống thuốc hôm nay</h1>
      </div>

      {error && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {error}
        </div>
      )}

      {latestPrescription && (
        <button
          type="button"
          onClick={handleEnableFromLatest}
          disabled={enableFromPrescription.isPending}
          className="mb-5 min-h-14 w-full rounded-2xl bg-blue-600 px-4 text-lg font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
        >
          {enableFromPrescription.isPending
            ? "Đang bật nhắc..."
            : `Bật nhắc từ đơn thuốc ${latestPrescription.prescriptionCode}`}
        </button>
      )}

      {isLoading && <LoadingBlock label="Đang tải lịch uống thuốc..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {!isLoading && !isError && (reminders?.length ?? 0) === 0 && (
        <EmptyState
          icon={<PillIcon className="h-16 w-16" />}
          title="Chưa có nhắc uống thuốc"
          description="Bấm nút bên trên để bật nhắc từ đơn thuốc gần nhất"
        />
      )}

      <div className="flex flex-col gap-5">
        {SLOT_ORDER.map((slot) =>
          grouped[slot]?.length ? (
            <section key={slot}>
              <h2 className="mb-2 text-slate-800">{slot}</h2>
              <div className="flex flex-col gap-3">
                {grouped[slot].map((r) => (
                  <div
                    key={r.id}
                    className="flex items-center justify-between rounded-2xl border-2 border-slate-200 bg-white p-4"
                  >
                    <div>
                      <p className="text-lg font-semibold text-slate-900">{r.drugName}</p>
                      <p className="text-base text-slate-600">
                        {r.doseLabel} — {r.remindTime}
                      </p>
                    </div>
                    <label className="relative inline-flex h-8 w-14 cursor-pointer items-center">
                      <input
                        type="checkbox"
                        className="peer sr-only"
                        checked={r.enabled}
                        onChange={(e) => handleToggle(r.id, e.target.checked)}
                        aria-label={`Bật/tắt nhắc uống ${r.drugName}`}
                      />
                      <span className="absolute inset-0 rounded-full bg-slate-300 transition-colors peer-checked:bg-blue-600" />
                      <span className="absolute left-1 h-6 w-6 rounded-full bg-white transition-transform peer-checked:translate-x-6" />
                    </label>
                  </div>
                ))}
              </div>
            </section>
          ) : null,
        )}
      </div>
    </div>
  );
}
