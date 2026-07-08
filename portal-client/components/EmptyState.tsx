import type { ReactNode } from "react";

interface EmptyStateProps {
  icon?: string;
  title: string;
  description?: string;
  action?: ReactNode;
}

export function EmptyState({ icon = "📭", title, description, action }: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center gap-3 rounded-2xl border border-dashed border-[--color-border] bg-white/60 px-6 py-10 text-center">
      <span className="text-5xl" aria-hidden="true">
        {icon}
      </span>
      <p className="text-lg font-bold text-[--color-text]">{title}</p>
      {description ? <p className="text-base text-[--color-text-muted]">{description}</p> : null}
      {action}
    </div>
  );
}
