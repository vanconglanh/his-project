"use client";

import { useEffect, useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Switch } from "@/components/ui/switch";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { getErrorMessage } from "@/lib/utils/errors";
import type {
  ReportCatalogItem,
  ReportSchedule,
  ReportScheduleFormat,
  ReportScheduleFrequency,
  ReportSchedulePeriod,
  SaveReportScheduleRequest,
} from "@/lib/api/reports";
import { useCreateReportSchedule, useUpdateReportSchedule } from "@/lib/hooks/use-reports";

const FREQUENCY_LABELS: Record<ReportScheduleFrequency, string> = {
  DAILY: "Hằng ngày",
  WEEKLY: "Hằng tuần",
  MONTHLY: "Hằng tháng",
};

const PERIOD_LABELS: Record<ReportSchedulePeriod, string> = {
  TODAY: "Hôm nay",
  THIS_WEEK: "Tuần này",
  THIS_MONTH: "Tháng này",
  LAST_MONTH: "Tháng trước",
};

const FORMAT_LABELS: Record<ReportScheduleFormat, string> = {
  PDF: "PDF",
  EXCEL: "Excel",
};

const DAY_OF_WEEK_LABELS: Record<number, string> = {
  0: "Chủ nhật",
  1: "Thứ Hai",
  2: "Thứ Ba",
  3: "Thứ Tư",
  4: "Thứ Năm",
  5: "Thứ Sáu",
  6: "Thứ Bảy",
};

const HOUR_ITEMS = Object.fromEntries(Array.from({ length: 24 }, (_, h) => [String(h), `${String(h).padStart(2, "0")}:00`]));

const EMAIL_RE = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface ScheduleFormDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  catalog: ReportCatalogItem[];
  editing: ReportSchedule | null;
}

interface FormState {
  report_code: string;
  title: string;
  frequency: ReportScheduleFrequency;
  hour: number;
  day_of_week: number;
  day_of_month: number;
  period: ReportSchedulePeriod;
  format: ReportScheduleFormat;
  recipientsText: string;
  enabled: boolean;
}

function emptyForm(): FormState {
  return {
    report_code: "",
    title: "",
    frequency: "DAILY",
    hour: 7,
    day_of_week: 1,
    day_of_month: 1,
    period: "TODAY",
    format: "PDF",
    recipientsText: "",
    enabled: true,
  };
}

function formFromSchedule(s: ReportSchedule): FormState {
  return {
    report_code: s.report_code,
    title: s.title,
    frequency: s.frequency,
    hour: s.hour,
    day_of_week: s.day_of_week ?? 1,
    day_of_month: s.day_of_month ?? 1,
    period: s.period,
    format: s.format,
    recipientsText: s.recipients.join(", "),
    enabled: s.enabled,
  };
}

/** Dialog Tạo/Sửa lịch gửi báo cáo qua email — dùng chung 1 form cho cả 2 chế độ. */
export function ScheduleFormDialog({ open, onOpenChange, catalog, editing }: ScheduleFormDialogProps) {
  const [form, setForm] = useState<FormState>(emptyForm);
  const createMutation = useCreateReportSchedule();
  const updateMutation = useUpdateReportSchedule();
  const isSaving = createMutation.isPending || updateMutation.isPending;

  useEffect(() => {
    if (open) {
      setForm(editing ? formFromSchedule(editing) : emptyForm());
    }
  }, [open, editing]);

  function patch(p: Partial<FormState>) {
    setForm((prev) => ({ ...prev, ...p }));
  }

  function parseRecipients(): string[] | null {
    const list = form.recipientsText
      .split(/[,;\n]/)
      .map((e) => e.trim())
      .filter(Boolean);
    if (list.length === 0) return null;
    if (list.some((e) => !EMAIL_RE.test(e))) return null;
    return list;
  }

  function handleSubmit() {
    if (!form.report_code) {
      toast.error("Vui lòng chọn báo cáo.");
      return;
    }
    if (!form.title.trim()) {
      toast.error("Vui lòng nhập tên lịch gửi.");
      return;
    }
    const recipients = parseRecipients();
    if (!recipients) {
      toast.error("Danh sách email không hợp lệ. Nhập email hợp lệ, phân tách bởi dấu phẩy.");
      return;
    }

    const body: SaveReportScheduleRequest = {
      report_code: form.report_code,
      title: form.title.trim(),
      frequency: form.frequency,
      hour: form.hour,
      day_of_week: form.frequency === "WEEKLY" ? form.day_of_week : undefined,
      day_of_month: form.frequency === "MONTHLY" ? form.day_of_month : undefined,
      period: form.period,
      format: form.format,
      recipients,
      enabled: form.enabled,
    };

    if (editing) {
      updateMutation.mutate(
        { id: String(editing.id), body },
        {
          onSuccess: () => {
            toast.success("Đã cập nhật lịch gửi báo cáo.");
            onOpenChange(false);
          },
          onError: (err) => toast.error(getErrorMessage(err, "Không cập nhật được lịch gửi. Vui lòng thử lại.")),
        }
      );
    } else {
      createMutation.mutate(body, {
        onSuccess: () => {
          toast.success("Đã tạo lịch gửi báo cáo.");
          onOpenChange(false);
        },
        onError: (err) => toast.error(getErrorMessage(err, "Không tạo được lịch gửi. Vui lòng thử lại.")),
      });
    }
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{editing ? "Sửa lịch gửi báo cáo" : "Tạo lịch gửi báo cáo"}</DialogTitle>
        </DialogHeader>

        <div className="space-y-4 max-h-[70vh] overflow-y-auto pr-1">
          <div className="space-y-1.5">
            <Label>Báo cáo</Label>
            <Select
              items={Object.fromEntries(catalog.map((r) => [r.code, r.title]))}
              value={form.report_code}
              onValueChange={(v) => v && patch({ report_code: v })}
            >
              <SelectTrigger className="h-9 w-full">
                <SelectValue placeholder="Chọn báo cáo" />
              </SelectTrigger>
              <SelectContent>
                {catalog.map((r) => (
                  <SelectItem key={r.code} value={r.code}>
                    {r.title}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="schedule-title">Tên lịch gửi</Label>
            <Input
              id="schedule-title"
              value={form.title}
              onChange={(e) => patch({ title: e.target.value })}
              placeholder="VD: Doanh thu hằng ngày gửi kế toán"
            />
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1.5">
              <Label>Tần suất</Label>
              <Select
                items={FREQUENCY_LABELS}
                value={form.frequency}
                onValueChange={(v) => v && patch({ frequency: v as ReportScheduleFrequency })}
              >
                <SelectTrigger className="h-9 w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(Object.keys(FREQUENCY_LABELS) as ReportScheduleFrequency[]).map((f) => (
                    <SelectItem key={f} value={f}>
                      {FREQUENCY_LABELS[f]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label>Giờ gửi</Label>
              <Select items={HOUR_ITEMS} value={String(form.hour)} onValueChange={(v) => v && patch({ hour: Number(v) })}>
                <SelectTrigger className="h-9 w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Array.from({ length: 24 }, (_, h) => (
                    <SelectItem key={h} value={String(h)}>
                      {String(h).padStart(2, "0")}:00
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            {form.frequency === "WEEKLY" && (
              <div className="space-y-1.5">
                <Label>Vào thứ</Label>
                <Select
                  items={DAY_OF_WEEK_LABELS}
                  value={String(form.day_of_week)}
                  onValueChange={(v) => v && patch({ day_of_week: Number(v) })}
                >
                  <SelectTrigger className="h-9 w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Object.entries(DAY_OF_WEEK_LABELS).map(([v, label]) => (
                      <SelectItem key={v} value={v}>
                        {label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            {form.frequency === "MONTHLY" && (
              <div className="space-y-1.5">
                <Label>Vào ngày</Label>
                <Select
                  items={Object.fromEntries(Array.from({ length: 31 }, (_, i) => [String(i + 1), `Ngày ${i + 1}`]))}
                  value={String(form.day_of_month)}
                  onValueChange={(v) => v && patch({ day_of_month: Number(v) })}
                >
                  <SelectTrigger className="h-9 w-full">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    {Array.from({ length: 31 }, (_, i) => (
                      <SelectItem key={i + 1} value={String(i + 1)}>
                        Ngày {i + 1}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
            )}

            <div className="space-y-1.5">
              <Label>Kỳ dữ liệu</Label>
              <Select items={PERIOD_LABELS} value={form.period} onValueChange={(v) => v && patch({ period: v as ReportSchedulePeriod })}>
                <SelectTrigger className="h-9 w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(Object.keys(PERIOD_LABELS) as ReportSchedulePeriod[]).map((p) => (
                    <SelectItem key={p} value={p}>
                      {PERIOD_LABELS[p]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1.5">
              <Label>Định dạng</Label>
              <Select items={FORMAT_LABELS} value={form.format} onValueChange={(v) => v && patch({ format: v as ReportScheduleFormat })}>
                <SelectTrigger className="h-9 w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {(Object.keys(FORMAT_LABELS) as ReportScheduleFormat[]).map((f) => (
                    <SelectItem key={f} value={f}>
                      {FORMAT_LABELS[f]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="schedule-recipients">Danh sách email nhận</Label>
            <Textarea
              id="schedule-recipients"
              value={form.recipientsText}
              onChange={(e) => patch({ recipientsText: e.target.value })}
              placeholder="email1@phongkham.vn, email2@phongkham.vn"
              className="min-h-[70px]"
            />
            <p className="text-xs text-muted-foreground">Phân tách nhiều email bởi dấu phẩy hoặc xuống dòng.</p>
          </div>

          <label className="flex min-h-9 items-center gap-2.5 text-sm">
            <Switch checked={form.enabled} onCheckedChange={(v) => patch({ enabled: v })} />
            Kích hoạt lịch gửi
          </label>
        </div>

        <DialogFooter>
          <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isSaving}>
            Huỷ
          </Button>
          <Button type="button" onClick={handleSubmit} disabled={isSaving}>
            {isSaving ? "Đang lưu..." : "Lưu"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
