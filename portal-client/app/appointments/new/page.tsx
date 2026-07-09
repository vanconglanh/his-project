"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { BigButton } from "@/components/BigButton";
import { ChevronLeftIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { ApiRequestError } from "@/lib/api";
import { useCreateAppointmentMutation, useDoctors, useSlots } from "@/lib/hooks";
import { cn, formatDate } from "@/lib/utils";

type Step = 1 | 2 | 3;

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export default function NewAppointmentPage() {
  const router = useRouter();
  const [step, setStep] = useState<Step>(1);
  const [doctorRef, setDoctorRef] = useState<string>("");
  const [doctorName, setDoctorName] = useState<string>("");
  const [date, setDate] = useState<string>(todayIso());
  const [slotAt, setSlotAt] = useState<string>("");
  const [note, setNote] = useState("");
  const [error, setError] = useState<string | null>(null);

  const { data: doctors, isLoading: loadingDoctors, isError: errorDoctors } = useDoctors();
  const { data: slots, isLoading: loadingSlots, isError: errorSlots } = useSlots(doctorRef, date);
  const createMutation = useCreateAppointmentMutation();

  function handleConfirm() {
    setError(null);
    createMutation.mutate(
      { appointmentAt: slotAt, doctorId: doctorRef, note: note || undefined },
      {
        onSuccess: () => router.push("/appointments"),
        onError: (err) => {
          if (err instanceof ApiRequestError && err.code === "APPOINTMENT_SLOT_TAKEN") {
            setError("Khung giờ này vừa có người đặt, vui lòng chọn giờ khác");
            setStep(2);
          } else {
            setError(err instanceof ApiRequestError ? err.message : "Đặt lịch thất bại, vui lòng thử lại");
          }
        },
      },
    );
  }

  return (
    <div className="p-4">
      <div className="mb-5 flex items-center gap-2 pt-4">
        <button
          type="button"
          onClick={() => (step === 1 ? router.push("/appointments") : setStep((s) => (s - 1) as Step))}
          aria-label="Quay lại"
          className="flex h-11 w-11 items-center justify-center rounded-full hover:bg-slate-100"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
        <h1 className="text-slate-900">Đặt lịch khám</h1>
      </div>

      <p className="mb-4 text-base text-slate-500">Bước {step}/3</p>

      {error && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {error}
        </div>
      )}

      {step === 1 && (
        <div className="flex flex-col gap-3">
          <h2 className="mb-1 text-slate-800">Chọn bác sĩ</h2>
          {loadingDoctors && <LoadingBlock label="Đang tải danh sách bác sĩ..." />}
          {errorDoctors && <ErrorBlock error={undefined} />}
          {doctors?.length === 0 && (
            <EmptyState icon={<span className="text-4xl">🩺</span>} title="Chưa có bác sĩ khả dụng" />
          )}
          {doctors?.map((d) => (
            <button
              key={d.doctorRef}
              type="button"
              onClick={() => {
                setDoctorRef(d.doctorRef);
                setDoctorName(d.fullName);
                setStep(2);
              }}
              className="min-h-14 rounded-2xl border border-[var(--border-soft)] bg-white px-4 text-left text-lg font-semibold text-slate-900 shadow-[var(--shadow-card)] transition-all hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)]"
            >
              {d.fullName}
            </button>
          ))}
        </div>
      )}

      {step === 2 && (
        <div className="flex flex-col gap-4">
          <h2 className="text-slate-800">Chọn ngày &amp; giờ khám</h2>
          <p className="text-base text-slate-500">Bác sĩ: {doctorName}</p>
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Ngày khám</span>
            <input
              type="date"
              value={date}
              min={todayIso()}
              onChange={(e) => {
                setDate(e.target.value);
                setSlotAt("");
              }}
              className="min-h-14 rounded-2xl border-2 border-slate-300 px-4 text-lg"
              aria-label="Ngày khám"
            />
          </label>

          {loadingSlots && <LoadingBlock label="Đang tải khung giờ..." />}
          {errorSlots && <ErrorBlock error={undefined} />}
          {slots?.length === 0 && <EmptyState icon={<span className="text-4xl">🗓️</span>} title="Không có khung giờ trống" />}

          <div className="grid grid-cols-3 gap-3">
            {slots?.map((s) => (
              <button
                key={s.slotAt}
                type="button"
                disabled={!s.available}
                onClick={() => setSlotAt(s.slotAt)}
                className={cn(
                  "min-h-14 rounded-2xl border-2 text-base font-semibold",
                  !s.available && "cursor-not-allowed border-slate-100 bg-slate-50 text-slate-300",
                  s.available && slotAt === s.slotAt && "border-teal-700 bg-teal-700 text-white",
                  s.available && slotAt !== s.slotAt && "border-slate-200 bg-white text-slate-900 hover:border-teal-500",
                )}
              >
                {new Intl.DateTimeFormat("vi-VN", { hour: "2-digit", minute: "2-digit" }).format(
                  new Date(s.slotAt),
                )}
              </button>
            ))}
          </div>

          <BigButton disabled={!slotAt} onClick={() => setStep(3)}>
            Tiếp tục
          </BigButton>
        </div>
      )}

      {step === 3 && (
        <div className="flex flex-col gap-4">
          <h2 className="text-slate-800">Xác nhận thông tin</h2>
          <div className="rounded-2xl border border-[var(--border-soft)] bg-white p-4 shadow-[var(--shadow-card)]">
            <p className="mb-1 text-lg font-semibold text-slate-900">{doctorName}</p>
            <p className="text-base text-slate-600">
              {formatDate(slotAt)}{" "}
              {slotAt &&
                new Intl.DateTimeFormat("vi-VN", { hour: "2-digit", minute: "2-digit" }).format(
                  new Date(slotAt),
                )}
            </p>
          </div>
          <label className="flex flex-col gap-2">
            <span className="text-lg font-medium text-slate-700">Ghi chú (không bắt buộc)</span>
            <textarea
              value={note}
              onChange={(e) => setNote(e.target.value)}
              rows={3}
              className="rounded-2xl border-2 border-slate-300 p-4 text-lg"
              aria-label="Ghi chú"
            />
          </label>
          <BigButton onClick={handleConfirm} disabled={createMutation.isPending}>
            {createMutation.isPending ? "Đang đặt lịch..." : "Xác nhận đặt lịch"}
          </BigButton>
        </div>
      )}
    </div>
  );
}
