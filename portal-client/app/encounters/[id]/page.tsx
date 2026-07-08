"use client";

import { useParams, useRouter } from "next/navigation";
import { BigButton } from "@/components/BigButton";
import { ChevronLeftIcon } from "@/components/icons";
import { ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { useEncounterDetail } from "@/lib/hooks";
import { formatDateTime } from "@/lib/utils";

export default function EncounterDetailPage() {
  const params = useParams<{ id: string }>();
  const router = useRouter();
  const id = params.id;

  const { data: encounter, isLoading, isError, refetch } = useEncounterDetail(id);

  return (
    <div className="p-4">
      <div className="mb-5 flex items-center gap-2 pt-4">
        <button
          type="button"
          onClick={() => router.push("/encounters")}
          aria-label="Quay lại"
          className="flex h-11 w-11 items-center justify-center rounded-full hover:bg-slate-100"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
        <h1 className="text-slate-900">Kết quả khám</h1>
      </div>

      {isLoading && <LoadingBlock label="Đang tải kết quả khám..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {encounter && (
        <div className="flex flex-col gap-4">
          <div className="rounded-2xl border-2 border-slate-200 bg-white p-4">
            <p className="text-lg font-semibold text-slate-900">
              {formatDateTime(encounter.visitedAt)}
            </p>
            <p className="text-base text-slate-600">BS. {encounter.doctorName}</p>
            <p className="mt-2 text-base text-slate-700">
              <b>Lý do khám:</b> {encounter.chiefComplaint}
            </p>
          </div>

          {encounter.diagnosis.length > 0 && (
            <div className="rounded-2xl border-2 border-slate-200 bg-white p-4">
              <h2 className="mb-2 text-slate-800">Chẩn đoán</h2>
              <ul className="list-disc pl-5 text-base text-slate-700">
                {encounter.diagnosis.map((d) => (
                  <li key={d.icd10}>
                    {d.name} <span className="text-slate-400">({d.icd10})</span>
                  </li>
                ))}
              </ul>
            </div>
          )}

          {encounter.conclusion && (
            <div className="rounded-2xl border-2 border-slate-200 bg-white p-4">
              <h2 className="mb-2 text-slate-800">Kết luận</h2>
              <p className="text-base text-slate-700">{encounter.conclusion}</p>
            </div>
          )}

          {encounter.doctorAdvice && (
            <div className="rounded-2xl border-2 border-amber-300 bg-amber-50 p-4">
              <h2 className="mb-2 text-amber-900">Lời dặn của bác sĩ</h2>
              <p className="text-lg font-medium text-amber-900">{encounter.doctorAdvice}</p>
            </div>
          )}

          {encounter.prescriptionItems.length > 0 && (
            <div className="rounded-2xl border-2 border-slate-200 bg-white p-4">
              <div className="mb-2 flex items-center justify-between">
                <h2 className="text-slate-800">Đơn thuốc</h2>
              </div>
              <ul className="flex flex-col gap-3">
                {encounter.prescriptionItems.map((item, idx) => (
                  <li key={idx} className="border-b border-slate-100 pb-2 last:border-0">
                    <p className="text-base font-semibold text-slate-900">{item.drugName}</p>
                    <p className="text-base text-slate-600">
                      {item.dosage} — {item.frequency} — {item.durationDays} ngày
                    </p>
                    {item.instructions && (
                      <p className="text-base text-slate-500">{item.instructions}</p>
                    )}
                  </li>
                ))}
              </ul>
              <BigButton
                className="mt-3"
                variant="outline"
                onClick={() => router.push("/prescriptions")}
              >
                Xem &amp; tải đơn thuốc (PDF)
              </BigButton>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
