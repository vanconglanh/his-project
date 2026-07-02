"use client";

import { useState } from "react";
import { CalendarIcon, Download } from "lucide-react";
import { format, subDays, startOfMonth, endOfMonth } from "date-fns";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";

type Preset = "7d" | "30d" | "thisMonth" | "lastMonth" | "custom";

export interface DateRange {
  from: string;
  to: string;
}

interface Props {
  onRangeChange: (range: DateRange) => void;
  onExport?: () => void;
  showExport?: boolean;
}

function getPresetRange(preset: Preset): DateRange {
  const today = new Date();
  const fmt = (d: Date) => format(d, "yyyy-MM-dd");
  switch (preset) {
    case "7d":
      return { from: fmt(subDays(today, 6)), to: fmt(today) };
    case "30d":
      return { from: fmt(subDays(today, 29)), to: fmt(today) };
    case "thisMonth":
      return { from: fmt(startOfMonth(today)), to: fmt(endOfMonth(today)) };
    case "lastMonth": {
      const lastM = new Date(today.getFullYear(), today.getMonth() - 1, 1);
      return { from: fmt(startOfMonth(lastM)), to: fmt(endOfMonth(lastM)) };
    }
    default:
      return { from: fmt(subDays(today, 29)), to: fmt(today) };
  }
}

export function ReportFilterBar({ onRangeChange, onExport, showExport = true }: Props) {
  const [preset, setPreset] = useState<Preset>("30d");
  const [customFrom, setCustomFrom] = useState("");
  const [customTo, setCustomTo] = useState("");

  function handlePresetChange(val: string | null) {
    if (!val) return;
    const p = val as Preset;
    setPreset(p);
    if (p !== "custom") {
      onRangeChange(getPresetRange(p));
    }
  }

  function handleApplyCustom() {
    if (customFrom && customTo) {
      onRangeChange({ from: customFrom, to: customTo });
    }
  }

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className="flex flex-col gap-1">
        <Label className="text-xs">Khoảng thời gian</Label>
        <Select value={preset} onValueChange={handlePresetChange}>
          <SelectTrigger className="w-40 h-9">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="7d">7 ngày qua</SelectItem>
            <SelectItem value="30d">30 ngày qua</SelectItem>
            <SelectItem value="thisMonth">Tháng này</SelectItem>
            <SelectItem value="lastMonth">Tháng trước</SelectItem>
            <SelectItem value="custom">Tùy chọn</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {preset === "custom" && (
        <>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">Từ ngày</Label>
            <div className="relative">
              <CalendarIcon className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                type="date"
                value={customFrom}
                onChange={(e) => setCustomFrom(e.target.value)}
                className="pl-8 h-9 w-36 text-sm"
              />
            </div>
          </div>
          <div className="flex flex-col gap-1">
            <Label className="text-xs">Đến ngày</Label>
            <div className="relative">
              <CalendarIcon className="absolute left-2.5 top-2.5 h-3.5 w-3.5 text-muted-foreground" />
              <Input
                type="date"
                value={customTo}
                onChange={(e) => setCustomTo(e.target.value)}
                className="pl-8 h-9 w-36 text-sm"
              />
            </div>
          </div>
          <Button size="sm" onClick={handleApplyCustom} className="h-9">
            Áp dụng
          </Button>
        </>
      )}

      {showExport && onExport && (
        <Button variant="outline" size="sm" onClick={onExport} className="ml-auto gap-2 h-9">
          <Download className="h-4 w-4" />
          Xuất báo cáo
        </Button>
      )}
    </div>
  );
}
