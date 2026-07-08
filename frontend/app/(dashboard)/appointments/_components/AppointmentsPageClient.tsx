"use client";

import { useMemo, useState } from "react";
import { toast } from "sonner";
import { format, startOfWeek, endOfWeek, startOfMonth, endOfMonth } from "date-fns";
import {
  Plus,
  MoreHorizontal,
  Pencil,
  Printer,
  CalendarCheck,
  LogIn,
  Ban,
  UserX,
} from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Label } from "@/components/ui/label";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { DataTable, type Column } from "@/components/ui/DataTable";
import { EmptyState } from "@/components/ui/EmptyState";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { formatDateTime } from "@/lib/utils/format";
import { useDebounce } from "@/lib/hooks/use-debounce";
import {
  useAppointments,
  useDoctorOptions,
  useChangeAppointmentStatus,
} from "@/lib/hooks/use-appointments";
import { printAppointmentSlipPdf } from "@/lib/api/appointments";
import type {
  AppointmentResponse,
  AppointmentStatus,
} from "@/lib/api/appointments";
import {
  AppointmentStatusBadge,
  APPOINTMENT_SOURCE_LABEL,
  APPOINTMENT_STATUS_LABEL,
} from "./AppointmentStatusBadge";
import { AppointmentFormDialog } from "./AppointmentFormDialog";

type DatePreset = "today" | "thisWeek" | "thisMonth" | "custom";

const DATE_PRESET_LABEL: Record<DatePreset, string> = {
  today: "Hôm nay",
  thisWeek: "Tuần này",
  thisMonth: "Tháng này",
  custom: "Tuỳ chọn",
};

const fmt = (d: Date) => format(d, "yyyy-MM-dd");

function getPresetRange(preset: DatePreset): { from: string; to: string } {
  const today = new Date();
  switch (preset) {
    case "today":
      return { from: fmt(today), to: fmt(today) };
    case "thisWeek":
      return {
        from: fmt(startOfWeek(today, { weekStartsOn: 1 })),
        to: fmt(endOfWeek(today, { weekStartsOn: 1 })),
      };
    case "thisMonth":
      return { from: fmt(startOfMonth(today)), to: fmt(endOfMonth(today)) };
    default:
      return { from: fmt(startOfWeek(today, { weekStartsOn: 1 })), to: fmt(endOfWeek(today, { weekStartsOn: 1 })) };
  }
}

const STATUS_OPTIONS: AppointmentStatus[] = [
  "PENDING",
  "CONFIRMED",
  "CHECKED_IN",
  "CANCELLED",
  "NO_SHOW",
];

export function AppointmentsPageClient() {
  const [preset, setPreset] = useState<DatePreset>("thisWeek");
  const [range, setRange] = useState(() => getPresetRange("thisWeek"));
  const [doctorRef, setDoctorRef] = useState<string>("all");
  const [status, setStatus] = useState<string>("all");
  const [q, setQ] = useState("");
  const [page, setPage] = useState(1);

  const [formOpen, setFormOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<AppointmentResponse | null>(null);
  const [statusTarget, setStatusTarget] = useState<{
    appointment: AppointmentResponse;
    next: AppointmentStatus;
  } | null>(null);

  const debouncedQ = useDebounce(q, 300);

  const { data: doctorOptions = [] } = useDoctorOptions();

  const params = useMemo(
    () => ({
      from: range.from,
      to: range.to,
      doctor_ref: doctorRef !== "all" ? doctorRef : undefined,
      status: status !== "all" ? (status as AppointmentStatus) : undefined,
      q: debouncedQ || undefined,
      page,
      page_size: 20,
    }),
    [range, doctorRef, status, debouncedQ, page]
  );

  const { data, isLoading, isError } = useAppointments(params);
  const changeStatus = useChangeAppointmentStatus();

  const rows = data?.data ?? [];

  function handlePresetChange(val: string | null) {
    if (!val) return;
    const p = val as DatePreset;
    setPreset(p);
    setPage(1);
    if (p !== "custom") {
      setRange(getPresetRange(p));
    }
  }

  function handleCreate() {
    setEditTarget(null);
    setFormOpen(true);
  }

  function handleEdit(appt: AppointmentResponse) {
    setEditTarget(appt);
    setFormOpen(true);
  }

  function requestStatusChange(appt: AppointmentResponse, next: AppointmentStatus) {
    if (next === "CANCELLED" || next === "NO_SHOW") {
      setStatusTarget({ appointment: appt, next });
    } else {
      changeStatus.mutate({ id: appt.id, status: next });
    }
  }

  function confirmStatusChange() {
    if (!statusTarget) return;
    changeStatus.mutate(
      { id: statusTarget.appointment.id, status: statusTarget.next },
      { onSuccess: () => setStatusTarget(null) }
    );
  }

  async function handlePrint(appt: AppointmentResponse) {
    try {
      await printAppointmentSlipPdf(appt.id);
    } catch {
      toast.error("Không in được giấy hẹn");
    }
  }

  const columns: Column<AppointmentResponse>[] = [
    {
      key: "appointment_at",
      header: "Giờ hẹn",
      cell: (row) => (
        <span className="font-medium whitespace-nowrap">
          {formatDateTime(row.appointment_at)}
        </span>
      ),
    },
    {
      key: "patient",
      header: "Bệnh nhân",
      cell: (row) => (
        <div>
          <p className="font-medium">{row.patient_name}</p>
          {row.patient_phone && (
            <p className="text-xs text-muted-foreground">{row.patient_phone}</p>
          )}
        </div>
      ),
    },
    {
      key: "doctor",
      header: "Bác sĩ",
      cell: (row) => row.doctor_name ?? <span className="text-muted-foreground">—</span>,
    },
    {
      key: "duration",
      header: "Thời lượng",
      cell: (row) => `${row.duration_minutes} phút`,
    },
    {
      key: "source",
      header: "Nguồn",
      cell: (row) => APPOINTMENT_SOURCE_LABEL[row.source] ?? row.source,
    },
    {
      key: "status",
      header: "Trạng thái",
      cell: (row) => <AppointmentStatusBadge status={row.status} />,
    },
    {
      key: "actions",
      header: "Thao tác",
      className: "text-right",
      cell: (row) => (
        <div className="text-right">
          <DropdownMenu>
            <DropdownMenuTrigger
              className="inline-flex h-9 w-9 items-center justify-center rounded-lg hover:bg-muted"
              onClick={(e) => e.stopPropagation()}
              onDoubleClick={(e) => e.stopPropagation()}
            >
              <MoreHorizontal className="h-4 w-4" />
              <span className="sr-only">Thao tác</span>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuItem onClick={() => handleEdit(row)}>
                <Pencil className="mr-2 h-4 w-4" /> Sửa
              </DropdownMenuItem>
              <DropdownMenuItem onClick={() => handlePrint(row)}>
                <Printer className="mr-2 h-4 w-4" /> In giấy hẹn
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              {row.status !== "CONFIRMED" && (
                <DropdownMenuItem onClick={() => requestStatusChange(row, "CONFIRMED")}>
                  <CalendarCheck className="mr-2 h-4 w-4" /> Xác nhận
                </DropdownMenuItem>
              )}
              {row.status !== "CHECKED_IN" && (
                <DropdownMenuItem onClick={() => requestStatusChange(row, "CHECKED_IN")}>
                  <LogIn className="mr-2 h-4 w-4" /> Check-in
                </DropdownMenuItem>
              )}
              {row.status !== "NO_SHOW" && (
                <DropdownMenuItem onClick={() => requestStatusChange(row, "NO_SHOW")}>
                  <UserX className="mr-2 h-4 w-4" /> Không đến
                </DropdownMenuItem>
              )}
              {row.status !== "CANCELLED" && (
                <DropdownMenuItem
                  className="text-destructive"
                  onClick={() => requestStatusChange(row, "CANCELLED")}
                >
                  <Ban className="mr-2 h-4 w-4" /> Huỷ lịch hẹn
                </DropdownMenuItem>
              )}
            </DropdownMenuContent>
          </DropdownMenu>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-4">
      <PageHeader
        title="Lịch hẹn"
        description="Quản lý lịch hẹn khám của bệnh nhân"
        actions={
          <Button onClick={handleCreate}>
            <Plus className="mr-2 h-4 w-4" /> Tạo lịch hẹn
          </Button>
        }
      />

      {/* Filter bar */}
      <div className="flex flex-wrap items-end gap-3 rounded-lg border bg-card p-3">
        <div className="flex flex-col gap-1">
          <Label className="text-xs">Khoảng thời gian</Label>
          <Select items={DATE_PRESET_LABEL} value={preset} onValueChange={handlePresetChange}>
            <SelectTrigger className="w-36 h-9">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {(Object.keys(DATE_PRESET_LABEL) as DatePreset[]).map((p) => (
                <SelectItem key={p} value={p}>
                  {DATE_PRESET_LABEL[p]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        {preset === "custom" && (
          <>
            <div className="flex flex-col gap-1">
              <Label className="text-xs">Từ ngày</Label>
              <Input
                type="date"
                value={range.from}
                onChange={(e) => {
                  setRange((r) => ({ ...r, from: e.target.value }));
                  setPage(1);
                }}
                className="h-9 w-36"
              />
            </div>
            <div className="flex flex-col gap-1">
              <Label className="text-xs">Đến ngày</Label>
              <Input
                type="date"
                value={range.to}
                onChange={(e) => {
                  setRange((r) => ({ ...r, to: e.target.value }));
                  setPage(1);
                }}
                className="h-9 w-36"
              />
            </div>
          </>
        )}

        <div className="flex flex-col gap-1">
          <Label className="text-xs">Bác sĩ</Label>
          <Select
            items={{ all: "Tất cả bác sĩ", ...Object.fromEntries(doctorOptions.map((d) => [d.value, d.label])) }}
            value={doctorRef}
            onValueChange={(v) => {
              setDoctorRef(v ?? "all");
              setPage(1);
            }}
          >
            <SelectTrigger className="w-44 h-9">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả bác sĩ</SelectItem>
              {doctorOptions.map((d) => (
                <SelectItem key={d.value} value={d.value}>
                  {d.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1">
          <Label className="text-xs">Trạng thái</Label>
          <Select
            items={{ all: "Tất cả trạng thái", ...APPOINTMENT_STATUS_LABEL }}
            value={status}
            onValueChange={(v) => {
              setStatus(v ?? "all");
              setPage(1);
            }}
          >
            <SelectTrigger className="w-44 h-9">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả trạng thái</SelectItem>
              {STATUS_OPTIONS.map((s) => (
                <SelectItem key={s} value={s}>
                  {APPOINTMENT_STATUS_LABEL[s]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="flex flex-col gap-1 flex-1 min-w-[200px]">
          <Label className="text-xs">Tìm bệnh nhân</Label>
          <Input
            placeholder="Tên hoặc SĐT bệnh nhân..."
            value={q}
            onChange={(e) => {
              setQ(e.target.value);
              setPage(1);
            }}
            className="h-9"
          />
        </div>
      </div>

      {isError ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được danh sách lịch hẹn. Vui lòng thử lại.
        </div>
      ) : (
        <DataTable
          columns={columns}
          data={rows}
          isLoading={isLoading}
          meta={data?.meta}
          onPageChange={setPage}
          onRowDoubleClick={(row) => {
            if (row.status !== "CHECKED_IN" && row.status !== "CANCELLED" && row.status !== "NO_SHOW") {
              requestStatusChange(row, "CHECKED_IN");
            }
          }}
          emptyState={
            <EmptyState
              variant="generic"
              title="Chưa có lịch hẹn"
              description="Chưa có lịch hẹn nào trong khoảng thời gian đã chọn."
              action={{ label: "Tạo lịch hẹn", onClick: handleCreate }}
            />
          }
        />
      )}

      <AppointmentFormDialog open={formOpen} onOpenChange={setFormOpen} editTarget={editTarget} />

      <ConfirmDialog
        open={Boolean(statusTarget)}
        onOpenChange={(o) => {
          if (!o) setStatusTarget(null);
        }}
        title={
          statusTarget?.next === "CANCELLED" ? "Huỷ lịch hẹn" : "Đánh dấu không đến"
        }
        description={
          statusTarget?.next === "CANCELLED" ? (
            <>
              Bạn có chắc muốn huỷ lịch hẹn của{" "}
              <strong>{statusTarget?.appointment.patient_name}</strong>?
            </>
          ) : (
            <>
              Đánh dấu <strong>{statusTarget?.appointment.patient_name}</strong> không đến
              lịch hẹn này?
            </>
          )
        }
        confirmLabel={statusTarget?.next === "CANCELLED" ? "Huỷ lịch hẹn" : "Xác nhận"}
        variant="destructive"
        isLoading={changeStatus.isPending}
        onConfirm={confirmStatusChange}
      />
    </div>
  );
}
