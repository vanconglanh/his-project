"use client";

import { QueueIcon } from "@/components/icons";
import { EmptyState, ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { useQueueInfo } from "@/lib/hooks";

export default function QueuePage() {
  const { data: queue, isLoading, isError, refetch } = useQueueInfo();

  return (
    <div className="p-4">
      <h1 className="mb-5 pt-4 text-slate-900">Hàng đợi của tôi</h1>

      {isLoading && <LoadingBlock label="Đang tải số thứ tự..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {!isLoading && !isError && !queue && (
        <EmptyState
          icon={<QueueIcon className="h-16 w-16" />}
          title="Bạn chưa lấy số"
          description="Vui lòng đến quầy lễ tân để lấy số thứ tự khám bệnh"
        />
      )}

      {queue && (
        <div className="flex flex-col gap-4">
          {queue.waitingAhead <= 3 && (
            <div className="rounded-2xl border-2 border-amber-300 bg-amber-50 p-4 text-center">
              <p className="text-lg font-bold text-amber-800">Sắp tới lượt của bạn!</p>
            </div>
          )}

          <div className="rounded-3xl border border-teal-200 bg-white p-6 text-center shadow-[0_6px_20px_rgba(1,100,90,0.1)]">
            <p className="text-lg text-slate-500">Số thứ tự của bạn</p>
            <p className="my-2 text-[64px] font-extrabold leading-none text-teal-700">
              {queue.ticketNo}
            </p>
            <p className="text-lg text-slate-600">Phòng: {queue.roomName}</p>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div className="rounded-2xl border border-[var(--border-soft)] bg-white p-4 text-center shadow-[var(--shadow-card)]">
              <p className="text-base text-slate-500">Đang gọi số</p>
              <p className="text-3xl font-bold text-slate-900">{queue.currentCalledNo}</p>
            </div>
            <div className="rounded-2xl border border-[var(--border-soft)] bg-white p-4 text-center shadow-[var(--shadow-card)]">
              <p className="text-base text-slate-500">Còn</p>
              <p className="text-3xl font-bold text-slate-900">{queue.waitingAhead} người</p>
            </div>
          </div>

          <div className="rounded-2xl bg-slate-100 p-4 text-center">
            <p className="text-lg text-slate-700">
              Thời gian chờ ước tính: <b>{queue.estWaitMinutes} phút</b>
            </p>
          </div>
        </div>
      )}
    </div>
  );
}
