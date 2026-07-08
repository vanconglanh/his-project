"use client";

import { useState } from "react";
import { toast } from "sonner";
import { Pencil, Plus, Trash2 } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/EmptyState";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { Can } from "@/components/auth/Can";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import type { ReportSchedule } from "@/lib/api/reports";
import { useDeleteReportSchedule, useReportCatalog, useReportSchedules } from "@/lib/hooks/use-reports";
import { ScheduleFormDialog } from "./ScheduleFormDialog";

const FREQUENCY_LABELS: Record<string, string> = { DAILY: "Hằng ngày", WEEKLY: "Hằng tuần", MONTHLY: "Hằng tháng" };
const FORMAT_LABELS: Record<string, string> = { PDF: "PDF", EXCEL: "Excel" };

export function SchedulesPageClient() {
  const { data: catalog = [] } = useReportCatalog();
  const { data: schedules = [], isLoading } = useReportSchedules();
  const deleteMutation = useDeleteReportSchedule();

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<ReportSchedule | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ReportSchedule | null>(null);

  const reportTitleByCode = new Map(catalog.map((r) => [r.code, r.title]));

  function handleCreate() {
    setEditing(null);
    setFormOpen(true);
  }

  function handleEdit(schedule: ReportSchedule) {
    setEditing(schedule);
    setFormOpen(true);
  }

  function handleDelete() {
    if (!deleteTarget) return;
    deleteMutation.mutate(String(deleteTarget.id), {
      onSuccess: () => {
        toast.success("Đã xoá lịch gửi báo cáo.");
        setDeleteTarget(null);
      },
      onError: () => toast.error("Không xoá được lịch gửi. Vui lòng thử lại."),
    });
  }

  const columns: Column<ReportSchedule>[] = [
    {
      key: "title",
      header: "Tên lịch gửi",
      cell: (s) => (
        <div>
          <p className="font-medium">{s.title}</p>
          <p className="text-xs text-muted-foreground">{reportTitleByCode.get(s.report_code) ?? s.report_code}</p>
        </div>
      ),
    },
    {
      key: "frequency",
      header: "Tần suất",
      cell: (s) => (
        <span>
          {FREQUENCY_LABELS[s.frequency] ?? s.frequency} · {String(s.hour).padStart(2, "0")}:00
        </span>
      ),
    },
    { key: "format", header: "Định dạng", cell: (s) => FORMAT_LABELS[s.format] ?? s.format },
    { key: "recipients", header: "Người nhận", cell: (s) => <span className="text-xs">{s.recipients.join(", ")}</span> },
    {
      key: "enabled",
      header: "Trạng thái",
      cell: (s) => (
        <Badge variant={s.enabled ? "default" : "outline"}>{s.enabled ? "Đang bật" : "Đã tắt"}</Badge>
      ),
    },
    {
      key: "actions",
      header: "",
      className: "text-right",
      cell: (s) => (
        <Can permission="report.build">
          <div className="flex justify-end gap-2">
            <Button type="button" variant="ghost" size="icon-sm" onClick={() => handleEdit(s)} aria-label={`Sửa lịch gửi ${s.title}`}>
              <Pencil className="h-4 w-4" />
            </Button>
            <Button
              type="button"
              variant="ghost"
              size="icon-sm"
              className="text-destructive"
              onClick={() => setDeleteTarget(s)}
              aria-label={`Xoá lịch gửi ${s.title}`}
            >
              <Trash2 className="h-4 w-4" />
            </Button>
          </div>
        </Can>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <PageHeader
        title="Lịch báo cáo"
        description="Tự động gửi báo cáo qua email theo lịch định kỳ"
        actions={
          <Can permission="report.build">
            <Button onClick={handleCreate} className="gap-1.5">
              <Plus className="h-4 w-4" />
              Tạo lịch gửi
            </Button>
          </Can>
        }
      />

      <DataTable
        columns={columns}
        data={schedules}
        isLoading={isLoading}
        emptyState={
          <EmptyState
            variant="generic"
            title="Chưa có lịch gửi báo cáo"
            description="Tạo lịch gửi để tự động nhận báo cáo qua email."
          />
        }
      />

      <Can permission="report.build">
        <ScheduleFormDialog open={formOpen} onOpenChange={setFormOpen} catalog={catalog} editing={editing} />
      </Can>

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Xoá lịch gửi báo cáo"
        description={`Bạn có chắc muốn xoá lịch gửi "${deleteTarget?.title}"? Hành động này không thể hoàn tác.`}
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={handleDelete}
      />
    </div>
  );
}
