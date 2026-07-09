import { ApiRequestError } from "@/lib/api";

export function LoadingBlock({ label = "Đang tải..." }: { label?: string }) {
  return (
    <div className="flex flex-col items-center justify-center gap-3 py-12 text-slate-500">
      <div
        className="h-10 w-10 animate-spin rounded-full border-4 border-slate-200 border-t-teal-700"
        role="status"
        aria-label={label}
      />
      <p className="text-base">{label}</p>
    </div>
  );
}

export function ErrorBlock({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const message =
    error instanceof ApiRequestError ? error.message : "Đã có lỗi xảy ra, vui lòng thử lại";

  return (
    <div className="flex flex-col items-center gap-3 rounded-2xl border-2 border-red-200 bg-red-50 p-6 text-center">
      <p className="text-lg font-semibold text-red-700">{message}</p>
      {onRetry && (
        <button
          type="button"
          onClick={onRetry}
          className="min-h-12 rounded-xl bg-red-600 px-5 text-base font-semibold text-white hover:bg-red-700"
        >
          Thử lại
        </button>
      )}
    </div>
  );
}

export function EmptyState({
  icon,
  title,
  description,
  action,
}: {
  icon: React.ReactNode;
  title: string;
  description?: string;
  action?: React.ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-2xl border-2 border-dashed border-slate-200 p-8 text-center text-slate-500">
      <div className="text-slate-300">{icon}</div>
      <p className="text-lg font-semibold text-slate-700">{title}</p>
      {description && <p className="text-base">{description}</p>}
      {action}
    </div>
  );
}
