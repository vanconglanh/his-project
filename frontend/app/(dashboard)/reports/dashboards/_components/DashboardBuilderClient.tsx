"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { ArrowDown, ArrowUp, LayoutDashboard, Plus, X } from "lucide-react";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { Can } from "@/components/auth/Can";
import { getErrorMessage } from "@/lib/utils/errors";
import type { DashboardWidgetType, ReportVisibility } from "@/lib/api/reports";
import {
  useCreateReportDashboard,
  useReportCatalog,
  useReportDashboard,
  useUpdateReportDashboard,
} from "@/lib/hooks/use-reports";
import { getReportGroupLabel } from "@/components/domain/reports-engine/report-icon-map";
import {
  computeWidgetLayout,
  createEmptyDashboardState,
  draftsFromWidgets,
  newWidgetId,
  WIDGET_HEIGHT_LABELS,
  WIDGET_TYPE_LABELS,
  WIDGET_WIDTH_LABELS,
  type DashboardBuilderState,
  type DashboardWidgetDraft,
} from "./types";

interface DashboardBuilderClientProps {
  editId?: string;
}

export function DashboardBuilderClient({ editId }: DashboardBuilderClientProps) {
  const router = useRouter();
  const { data: catalog = [], isLoading: isLoadingCatalog } = useReportCatalog();
  const { data: existingDashboard, isLoading: isLoadingDashboard } = useReportDashboard(editId ?? null);

  const [state, setState] = useState<DashboardBuilderState>(createEmptyDashboardState);
  const [pickedReportCode, setPickedReportCode] = useState<string>("");
  const [initialized, setInitialized] = useState(false);

  useEffect(() => {
    if (!editId || initialized || isLoadingDashboard || !existingDashboard) return;
    setState({
      title: existingDashboard.title,
      visibility: existingDashboard.visibility,
      widgets: draftsFromWidgets(existingDashboard.widgets),
    });
    setInitialized(true);
  }, [editId, initialized, isLoadingDashboard, existingDashboard]);

  const createMutation = useCreateReportDashboard();
  const updateMutation = useUpdateReportDashboard();
  const isSaving = createMutation.isPending || updateMutation.isPending;

  const reportByCode = useMemo(() => new Map(catalog.map((r) => [r.code, r])), [catalog]);
  const availableReports = catalog;

  function updateState(patch: Partial<DashboardBuilderState>) {
    setState((prev) => ({ ...prev, ...patch }));
  }

  function addWidget() {
    if (!pickedReportCode) return;
    const descriptor = reportByCode.get(pickedReportCode);
    if (!descriptor) return;
    const widgetType: DashboardWidgetType = descriptor.view_type === "CHART" ? "CHART" : "TABLE";
    const draft: DashboardWidgetDraft = {
      id: newWidgetId(),
      report_code: descriptor.code,
      title: descriptor.title,
      widget_type: widgetType,
      w: 6,
      h: 1,
    };
    updateState({ widgets: [...state.widgets, draft] });
    setPickedReportCode("");
  }

  function updateWidget(id: string, patch: Partial<DashboardWidgetDraft>) {
    updateState({ widgets: state.widgets.map((w) => (w.id === id ? { ...w, ...patch } : w)) });
  }

  function removeWidget(id: string) {
    updateState({ widgets: state.widgets.filter((w) => w.id !== id) });
  }

  function moveWidget(id: string, direction: -1 | 1) {
    const idx = state.widgets.findIndex((w) => w.id === id);
    const nextIdx = idx + direction;
    if (idx < 0 || nextIdx < 0 || nextIdx >= state.widgets.length) return;
    const next = [...state.widgets];
    [next[idx], next[nextIdx]] = [next[nextIdx], next[idx]];
    updateState({ widgets: next });
  }

  function handleSave() {
    if (!state.title.trim()) {
      toast.error("Vui lòng nhập tên bảng điều khiển.");
      return;
    }
    if (state.widgets.length === 0) {
      toast.error("Vui lòng thêm ít nhất 1 widget.");
      return;
    }
    const body = {
      title: state.title.trim(),
      visibility: state.visibility,
      widgets: computeWidgetLayout(state.widgets),
    };

    if (editId && existingDashboard) {
      updateMutation.mutate(
        { id: editId, body },
        {
          onSuccess: (dashboard) => {
            toast.success("Đã cập nhật bảng điều khiển.");
            router.push(`/reports/dashboards/${dashboard.id}`);
          },
          onError: (err) => toast.error(getErrorMessage(err, "Không cập nhật được bảng điều khiển. Vui lòng thử lại.")),
        }
      );
    } else {
      createMutation.mutate(body, {
        onSuccess: (dashboard) => {
          toast.success("Đã tạo bảng điều khiển.");
          router.push(`/reports/dashboards/${dashboard.id}`);
        },
        onError: (err) => toast.error(getErrorMessage(err, "Không tạo được bảng điều khiển. Vui lòng thử lại.")),
      });
    }
  }

  if (editId && (isLoadingDashboard || isLoadingCatalog) && !initialized) {
    return (
      <div className="space-y-4">
        <PageHeader title="Sửa bảng điều khiển" description="Đang tải..." />
        <Skeleton className="h-9 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  return (
    <Can permission="report.build" fallback={<AccessDenied />}>
      <div className="space-y-4">
        <PageHeader
          title={editId ? "Sửa bảng điều khiển" : "Tạo bảng điều khiển"}
          description="Ghép nhiều báo cáo đã lưu thành 1 màn hình theo dõi tổng quan"
        />

        <Card>
          <CardHeader>
            <CardTitle>1. Thông tin chung</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="dashboard-title">Tên bảng điều khiển</Label>
              <Input
                id="dashboard-title"
                value={state.title}
                onChange={(e) => updateState({ title: e.target.value })}
                placeholder="VD: Tổng quan phòng khám"
                className="max-w-md"
              />
            </div>
            <div className="space-y-1.5">
              <Label>Phạm vi hiển thị</Label>
              <RadioGroup
                value={state.visibility}
                onValueChange={(v) => v && updateState({ visibility: v as ReportVisibility })}
                className="flex flex-wrap gap-4"
              >
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="TENANT" />
                  Cả phòng khám
                </label>
                <label className="flex min-h-9 items-center gap-2 text-sm">
                  <RadioGroupItem value="PRIVATE" />
                  Chỉ mình tôi
                </label>
              </RadioGroup>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle>2. Widget</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex flex-wrap items-end gap-2">
              <div className="flex flex-col gap-1">
                <Label className="text-xs">Chọn báo cáo để thêm</Label>
                <Select
                  items={Object.fromEntries(availableReports.map((r) => [r.code, `${getReportGroupLabel(r.group)} — ${r.title}`]))}
                  value={pickedReportCode}
                  onValueChange={(v) => setPickedReportCode(v ?? "")}
                >
                  <SelectTrigger className="h-9 w-72">
                    <SelectValue placeholder="Chọn báo cáo" />
                  </SelectTrigger>
                  <SelectContent>
                    {availableReports.map((r) => (
                      <SelectItem key={r.code} value={r.code}>
                        {getReportGroupLabel(r.group)} — {r.title}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <Button type="button" variant="outline" onClick={addWidget} disabled={!pickedReportCode} className="gap-1.5">
                <Plus className="h-4 w-4" />
                Thêm widget
              </Button>
            </div>

            {state.widgets.length === 0 ? (
              <p className="text-sm text-muted-foreground">Chưa có widget nào. Chọn báo cáo ở trên để thêm.</p>
            ) : (
              <ul className="space-y-2">
                {state.widgets.map((widget, idx) => {
                  const descriptor = reportByCode.get(widget.report_code);
                  return (
                    <li key={widget.id} className="flex flex-wrap items-center gap-2 rounded-md border p-2.5">
                      <LayoutDashboard className="h-4 w-4 shrink-0 text-muted-foreground" aria-hidden="true" />
                      <div className="min-w-0 flex-1">
                        <p className="truncate text-sm font-medium">{descriptor?.title ?? widget.report_code}</p>
                        <Input
                          value={widget.title}
                          onChange={(e) => updateWidget(widget.id, { title: e.target.value })}
                          placeholder="Tiêu đề hiển thị trên bảng điều khiển"
                          className="mt-1 h-8 text-xs"
                          aria-label="Tiêu đề widget"
                        />
                      </div>

                      <Select
                        items={WIDGET_TYPE_LABELS}
                        value={widget.widget_type}
                        onValueChange={(v) => v && updateWidget(widget.id, { widget_type: v as DashboardWidgetType })}
                      >
                        <SelectTrigger className="h-8 w-32 shrink-0">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {(Object.keys(WIDGET_TYPE_LABELS) as DashboardWidgetType[]).map((t) => (
                            <SelectItem key={t} value={t}>
                              {WIDGET_TYPE_LABELS[t]}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>

                      <Select
                        items={WIDGET_WIDTH_LABELS}
                        value={String(widget.w)}
                        onValueChange={(v) => v && updateWidget(widget.id, { w: Number(v) as 4 | 6 | 12 })}
                      >
                        <SelectTrigger className="h-8 w-28 shrink-0">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {([4, 6, 12] as const).map((w) => (
                            <SelectItem key={w} value={String(w)}>
                              {WIDGET_WIDTH_LABELS[w]}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>

                      <Select
                        items={WIDGET_HEIGHT_LABELS}
                        value={String(widget.h)}
                        onValueChange={(v) => v && updateWidget(widget.id, { h: Number(v) as 1 | 2 })}
                      >
                        <SelectTrigger className="h-8 w-24 shrink-0">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {([1, 2] as const).map((h) => (
                            <SelectItem key={h} value={String(h)}>
                              {WIDGET_HEIGHT_LABELS[h]}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>

                      <div className="flex shrink-0 items-center gap-1">
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon-sm"
                          onClick={() => moveWidget(widget.id, -1)}
                          disabled={idx === 0}
                          aria-label="Di chuyển lên"
                        >
                          <ArrowUp className="h-4 w-4" />
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon-sm"
                          onClick={() => moveWidget(widget.id, 1)}
                          disabled={idx === state.widgets.length - 1}
                          aria-label="Di chuyển xuống"
                        >
                          <ArrowDown className="h-4 w-4" />
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon-sm"
                          onClick={() => removeWidget(widget.id)}
                          aria-label="Xoá widget"
                        >
                          <X className="h-4 w-4" />
                        </Button>
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </CardContent>
        </Card>

        <div className="flex justify-end gap-2">
          <Button type="button" variant="outline" onClick={() => router.push("/reports/dashboards")} disabled={isSaving}>
            Huỷ
          </Button>
          <Button type="button" onClick={handleSave} disabled={isSaving}>
            {isSaving ? "Đang lưu..." : editId ? "Lưu thay đổi" : "Tạo bảng điều khiển"}
          </Button>
        </div>
      </div>
    </Can>
  );
}

function AccessDenied() {
  return (
    <EmptyState
      variant="generic"
      title="Không có quyền truy cập"
      description="Bạn không có quyền tạo/sửa bảng điều khiển. Vui lòng liên hệ quản trị viên."
    />
  );
}
