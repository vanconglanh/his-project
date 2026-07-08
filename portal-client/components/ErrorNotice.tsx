interface ErrorNoticeProps {
  message: string;
  onRetry?: () => void;
}

export function ErrorNotice({ message, onRetry }: ErrorNoticeProps) {
  return (
    <div
      role="alert"
      className="flex flex-col items-start gap-3 rounded-2xl border border-[--color-danger] bg-[--color-danger-soft] p-4 text-[--color-danger]"
    >
      <p className="text-base font-semibold">{message}</p>
      {onRetry ? (
        <button
          type="button"
          onClick={onRetry}
          className="min-h-[44px] rounded-xl border-2 border-[--color-danger] px-4 text-sm font-bold focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-[--color-focus]"
        >
          Thử lại
        </button>
      ) : null}
    </div>
  );
}
