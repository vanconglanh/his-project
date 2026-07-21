"use client";

import Link from "next/link";
import { FileTextIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { StatusBadge, type BadgeTone } from "@/components/StatusBadge";
import { useEncounters } from "@/lib/hooks";
import { formatDate } from "@/lib/utils";

/** Trạng thái lượt khám (API trả UPPERCASE) → nhãn tiếng Việt */
function encounterStatus(status: string | null | undefined): { label: string; tone: BadgeTone } {
  const s = (status ?? "").toUpperCase();
  if (["FINISHED", "COMPLETED", "DONE", "CLOSED"].includes(s)) return { label: "Đã khám", tone: "done" };
  if (["IN_PROGRESS", "OPEN", "EXAMINING"].includes(s)) return { label: "Đang khám", tone: "confirmed" };
  if (["CANCELLED", "CANCELED"].includes(s)) return { label: "Đã hủy", tone: "cancelled" };
  if (["WAITING", "PENDING"].includes(s)) return { label: "Đang chờ", tone: "pending" };
  if (s === "SCHEDULED") return { label: "Đã hẹn", tone: "pending" };
  return { label: status ?? "—", tone: "pending" };
}

export default function EncountersPage() {
  const { data: encounters, isLoading, isError, refetch } = useEncounters();

  return (
    <div className="p-4">
      <h1 className="mb-5 pt-4 text-slate-900">Lịch sử khám bệnh</h1>

      {isLoading && <LoadingBlock label="Đang tải lịch sử khám..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {!isLoading && !isError && (encounters?.length ?? 0) === 0 && (
        <EmptyState
          icon={<FileTextIcon className="h-16 w-16" />}
          title="Chưa có lượt khám nào"
          description="Lịch sử khám bệnh của bạn sẽ hiển thị ở đây"
        />
      )}

      <div className="flex flex-col gap-3">
        {encounters?.map((e) => (
          <Link
            key={e.id}
            href={`/encounters/${e.id}`}
            className="block rounded-2xl border border-[var(--border-soft)] bg-white p-4 shadow-[var(--shadow-card)] transition-all hover:-translate-y-0.5 hover:shadow-[var(--shadow-card-hover)]"
          >
            <div className="mb-1 flex items-center justify-between">
              <span className="text-lg font-semibold text-slate-900">{formatDate(e.visitedAt)}</span>
              {(() => {
                const st = encounterStatus(e.status);
                return <StatusBadge tone={st.tone} label={st.label} />;
              })()}
            </div>
            <p className="text-base text-slate-600">{e.doctorName}</p>
            <p className="text-base text-slate-600">Lý do khám: {e.chiefComplaint}</p>
            {e.diagnosis.length > 0 && (
              <p className="mt-1 text-base font-medium text-slate-800">
                Chẩn đoán: {e.diagnosis.map((d) => d.name).join(", ")}
              </p>
            )}
          </Link>
        ))}
      </div>
    </div>
  );
}
