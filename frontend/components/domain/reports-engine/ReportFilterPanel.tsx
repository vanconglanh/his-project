"use client";

import { useState } from "react";
import { Download, Printer, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { ReportCatalogItem, ReportFilterDescriptor } from "@/lib/api/reports";
import { useReportOptions } from "@/lib/hooks/use-reports";
import { REPORT_DATE_PRESET_LABELS, getReportPresetRange, type ReportDatePreset } from "./report-date-presets";

export interface ReportFilterDraft {
  from: string;
  to: string;
  [key: string]: string | undefined;
}

interface ReportFilterPanelProps {
  descriptor: ReportCatalogItem;
  draft: ReportFilterDraft;
  onDraftChange: (patch: Partial<ReportFilterDraft>) => void;
  onApply: () => void;
  onExportPdf: () => void;
  onExportExcel: () => void;
  canExport: boolean;
  isExportingPdf: boolean;
  isExportingExcel: boolean;
  isFetching: boolean;
}

/** BE trả options_source=null cho variance — hard-code theo PRD mục 5.3 (ALL/DIFF/NODIFF). */
const VARIANCE_OPTIONS = [
  { value: "DIFF", label: "Có chênh lệch" },
  { value: "NODIFF", label: "Không chênh lệch" },
];

export function ReportFilterPanel({
  descriptor,
  draft,
  onDraftChange,
  onApply,
  onExportPdf,
  onExportExcel,
  canExport,
  isExportingPdf,
  isExportingExcel,
  isFetching,
}: ReportFilterPanelProps) {
  const [preset, setPreset] = useState<ReportDatePreset>("thisMonth");

  function handlePresetChange(value: string | null) {
    if (!value) return;
    const p = value as ReportDatePreset;
    setPreset(p);
    if (p !== "custom") {
      const range = getReportPresetRange(p);
      onDraftChange({ from: range.from, to: range.to });
    }
  }

  return (
    <div className="sticky top-0 z-10 bg-background border-b pb-3 mb-4 pt-0.5">
      <div className="flex flex-wrap items-end gap-3">
        <div className="flex flex-col gap-1">
          <Label className="text-xs">Khoảng thời gian</Label>
          <Select value={preset} onValueChange={handlePresetChange}>
            <SelectTrigger className="w-36 h-9">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {(Object.keys(REPORT_DATE_PRESET_LABELS) as ReportDatePreset[]).map((key) => (
                <SelectItem key={key} value={key}>
                  {REPORT_DATE_PRESET_LABELS[key]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1">
          <Label className="text-xs">Từ ngày</Label>
          <Input
            type="date"
            value={draft.from}
            onChange={(e) => {
              setPreset("custom");
              onDraftChange({ from: e.target.value });
            }}
            className="h-9 w-36"
            aria-label="Từ ngày"
          />
        </div>
        <div className="flex flex-col gap-1">
          <Label className="text-xs">Đến ngày</Label>
          <Input
            type="date"
            value={draft.to}
            onChange={(e) => {
              setPreset("custom");
              onDraftChange({ to: e.target.value });
            }}
            className="h-9 w-36"
            aria-label="Đến ngày"
          />
        </div>

        {descriptor.filters.map((filter) => (
          <ReportDynamicFilterField
            key={filter.key}
            filter={filter}
            value={draft[filter.key]}
            onChange={(value) => onDraftChange({ [filter.key]: value })}
          />
        ))}

        <div className="ml-auto flex items-center gap-2">
          <Button onClick={onApply} className="h-9 gap-2 min-w-[44px]" disabled={isFetching}>
            <RefreshCw className={isFetching ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
            Lấy dữ liệu
          </Button>
          <Button
            variant="outline"
            onClick={onExportPdf}
            disabled={!canExport || isExportingPdf}
            className="h-9 gap-2 min-w-[44px]"
          >
            <Printer className="h-4 w-4" />
            {isExportingPdf ? "Đang tạo..." : "In Phiếu"}
          </Button>
          <Button
            variant="outline"
            onClick={onExportExcel}
            disabled={!canExport || isExportingExcel}
            className="h-9 gap-2 min-w-[44px]"
          >
            <Download className="h-4 w-4" />
            {isExportingExcel ? "Đang xuất..." : "Xuất Excel"}
          </Button>
        </div>
      </div>
    </div>
  );
}

interface ReportDynamicFilterFieldProps {
  filter: ReportFilterDescriptor;
  value: string | undefined;
  onChange: (value: string | undefined) => void;
}

function ReportDynamicFilterField({ filter, value, onChange }: ReportDynamicFilterFieldProps) {
  const hasOptionsSource = filter.type === "Select" && !!filter.options_source;
  const { data: options, isLoading } = useReportOptions(hasOptionsSource ? filter.options_source : null);

  const isVarianceEnum = filter.type === "Enum" && !filter.options_source && filter.key === "variance";
  const list = hasOptionsSource ? options ?? [] : isVarianceEnum ? VARIANCE_OPTIONS : [];

  return (
    <div className="flex flex-col gap-1">
      <Label className="text-xs">{filter.label}</Label>
      <Select value={value ?? "ALL"} onValueChange={(v) => onChange(!v || v === "ALL" ? undefined : v)}>
        <SelectTrigger className="w-40 h-9">
          <SelectValue placeholder={isLoading ? "Đang tải..." : "Tất cả"} />
        </SelectTrigger>
        <SelectContent>
          <SelectItem value="ALL">Tất cả</SelectItem>
          {list.map((opt) => (
            <SelectItem key={opt.value} value={opt.value}>
              {opt.label}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
}
