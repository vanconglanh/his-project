"use client";

import { Database } from "lucide-react";
import { cn } from "@/lib/utils";
import type { ReportDataset } from "@/lib/api/reports";

interface DatasetPickerProps {
  datasets: ReportDataset[];
  isLoading: boolean;
  selectedKey: string | null;
  onSelect: (key: string) => void;
}

export function DatasetPicker({ datasets, isLoading, selectedKey, onSelect }: DatasetPickerProps) {
  if (isLoading) {
    return <div className="text-sm text-muted-foreground">Đang tải danh sách nguồn dữ liệu...</div>;
  }

  return (
    <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
      {datasets.map((ds) => (
        <button
          key={ds.key}
          type="button"
          onClick={() => onSelect(ds.key)}
          className={cn(
            "flex min-h-11 items-center gap-2 rounded-lg border px-3 py-2.5 text-left text-sm font-medium transition-colors",
            selectedKey === ds.key
              ? "border-primary bg-primary/10 text-primary"
              : "border-border bg-background hover:bg-muted"
          )}
          aria-pressed={selectedKey === ds.key}
        >
          <Database className="h-4 w-4 shrink-0" aria-hidden="true" />
          <span className="truncate">{ds.label}</span>
        </button>
      ))}
    </div>
  );
}
