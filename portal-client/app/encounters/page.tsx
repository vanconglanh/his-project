"use client";

import Link from "next/link";
import { FileTextIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { useEncounters } from "@/lib/hooks";
import { formatDate } from "@/lib/utils";

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
            className="block rounded-2xl border-2 border-slate-200 bg-white p-4 hover:border-blue-400 hover:bg-blue-50"
          >
            <div className="mb-1 flex items-center justify-between">
              <span className="text-lg font-semibold text-slate-900">{formatDate(e.visitedAt)}</span>
              <span className="rounded-full bg-slate-100 px-3 py-1 text-sm font-medium text-slate-600">
                {e.status}
              </span>
            </div>
            <p className="text-base text-slate-600">BS. {e.doctorName}</p>
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
