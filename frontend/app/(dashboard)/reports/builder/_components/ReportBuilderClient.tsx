"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { RefreshCw } from "lucide-react";
import { Can } from "@/components/auth/Can";
import { EmptyState } from "@/components/ui/EmptyState";
import { Skeleton } from "@/components/ui/skeleton";
import type { ReportDefinition, SaveReportDefinitionRequest, ReportVisibility } from "@/lib/api/reports";
import {
  useReportDatasets,
  useReportDefinitions,
  usePreviewReportDefinition,
  useSaveReportDefinition,
  useUpdateReportDefinition,
} from "@/lib/hooks/use-reports";
import { getReportPresetRange, REPORT_DATE_PRESET_LABELS, type ReportDatePreset } from "@/components/domain/reports-engine/report-date-presets";
import { DatasetPicker } from "./DatasetPicker";
import { FieldTray } from "./FieldTray";
import { FilterBuilder } from "./FilterBuilder";
import { GroupSortConfig } from "./GroupSortConfig";
import { ViewConfig } from "./ViewConfig";
import { PreviewPane } from "./PreviewPane";
import { SaveReportDialog } from "./SaveReportDialog";
import {
  buildChartConfig,
  buildDefinitionBody,
  builderStateFromDefinition,
  createEmptyBuilderState,
  type BuilderState,
} from "./types";

interface ReportBuilderClientProps {
  /** id định nghĩa báo cáo cần sửa — truyền qua ?edit=id, rỗng = tạo mới */
  editId?: string;
}

export function ReportBuilderClient({ editId }: ReportBuilderClientProps) {
  const router = useRouter();
  const { data: datasets = [], isLoading: isLoadingDatasets, isError: isDatasetsError } = useReportDatasets();
  const { data: definitions = [], isLoading: isLoadingDefinitions } = useReportDefinitions(!!editId);

  const [state, setState] = useState<BuilderState>(createEmptyBuilderState);
  const [initializedEdit, setInitializedEdit] = useState(false);
  const [preset, setPreset] = useState<ReportDatePreset>("thisMonth");
  const [range, setRange] = useState(() => getReportPresetRange("thisMonth"));
  const [hasRun, setHasRun] = useState(false);

  const editingDefinition: ReportDefinition | undefined = editId
    ? definitions.find((d) => String(d.id) === String(editId))
    : undefined;

  // Nạp dữ liệu chỉnh sửa 1 lần khi có đủ dataset + definition tương ứng
  useEffect(() => {
    if (!editId || initializedEdit || isLoadingDefinitions || isLoadingDatasets) return;
    if (!editingDefinition) return;
    const dataset = datasets.find((d) => d.key === editingDefinition.dataset_key);
    setState(builderStateFromDefinition(editingDefinition, dataset));
    setInitializedEdit(true);
  }, [editId, initializedEdit, isLoadingDefinitions, isLoadingDatasets, editingDefinition, datasets]);

  const selectedDataset = useMemo(() => datasets.find((d) => d.key === state.datasetKey), [datasets, state.datasetKey]);

  const previewMutation = usePreviewReportDefinition();
  const saveMutation = useSaveReportDefinition();
  const updateMutation = useUpdateReportDefinition();

  function updateState(patch: Partial<BuilderState>) {
    setState((prev) => ({ ...prev, ...patch }));
  }

  function handleSelectDataset(key: string) {
    if (key === state.datasetKey) return;
    setState((prev) => ({ ...createEmptyBuilderState(), datasetKey: key, title: prev.title, visibility: prev.visibility }));
    setHasRun(false);
  }

  function handlePresetChange(value: string | null) {
    if (!value) return;
    const p = value as ReportDatePreset;
    setPreset(p);
    if (p !== "custom") setRange(getReportPresetRange(p));
  }

  function validateBeforeRun(): string | null {
    if (!state.datasetKey) return "Vui lòng chọn nguồn dữ liệu.";
    if (state.columns.length === 0) return "Vui lòng chọn ít nhất 1 cột.";
    if (state.viewType === "CHART" && (!state.chart.measure || state.chart.dims.length === 0)) {
      return "Biểu đồ cần chọn cột phân loại (chiều) và cột số liệu (measure).";
    }
    if (!range.from || !range.to) return "Vui lòng chọn khoảng thời gian.";
    return null;
  }

  function handlePreview() {
    const error = validateBeforeRun();
    if (error) {
      toast.error(error);
      return;
    }
    setHasRun(true);
    previewMutation.mutate({
      from: range.from,
      to: range.to,
      body: {
        dataset_key: state.datasetKey as string,
        definition: buildDefinitionBody(state),
        chart: buildChartConfig(state),
      },
    });
  }

  function handleSave(title: string, visibility: ReportVisibility) {
    const error = validateBeforeRun();
    if (error) {
      toast.error(error);
      return;
    }
    const body: SaveReportDefinitionRequest = {
      title,
      dataset_key: state.datasetKey as string,
      definition: buildDefinitionBody(state),
      chart: buildChartConfig(state),
      view_type: state.viewType,
      visibility,
    };

    if (editId && editingDefinition) {
      updateMutation.mutate(
        { id: editingDefinition.id, body },
        {
          onSuccess: (def) => {
            toast.success("Đã cập nhật báo cáo.");
            router.push(`/reports?report=${def.code}`);
          },
          onError: () => toast.error("Không cập nhật được báo cáo. Vui lòng thử lại."),
        }
      );
    } else {
      saveMutation.mutate(body, {
        onSuccess: (def) => {
          toast.success("Đã lưu báo cáo.");
          router.push(`/reports?report=${def.code}`);
        },
        onError: () => toast.error("Không lưu được báo cáo. Vui lòng thử lại."),
      });
    }
  }

  const isSaving = saveMutation.isPending || updateMutation.isPending;
  const canRun = !!state.datasetKey && state.columns.length > 0;

  if (editId && (isLoadingDefinitions || isLoadingDatasets) && !initializedEdit) {
    return (
      <div className="space-y-4">
        <PageHeader title="Trình tạo báo cáo" description="Đang tải báo cáo để sửa..." />
        <Skeleton className="h-9 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (editId && !isLoadingDefinitions && !editingDefinition) {
    return (
      <div className="space-y-4">
        <PageHeader title="Trình tạo báo cáo" />
        <EmptyState
          variant="generic"
          title="Không tìm thấy báo cáo cần sửa"
          description="Báo cáo có thể đã bị xoá hoặc bạn không có quyền chỉnh sửa."
          action={{ label: "Tạo báo cáo mới", onClick: () => router.replace("/reports/builder") }}
        />
      </div>
    );
  }

  return (
    <Can permission="report.build" fallback={<AccessDenied />}>
      <div className="space-y-4">
        <PageHeader
          title={editId ? "Sửa báo cáo" : "Trình tạo báo cáo"}
          description="Tự thiết kế báo cáo dạng bảng hoặc biểu đồ từ dữ liệu phòng khám của bạn"
        />

        <Card>
          <CardHeader>
            <CardTitle>1. Chọn nguồn dữ liệu</CardTitle>
          </CardHeader>
          <CardContent>
            {isDatasetsError ? (
              <p className="text-sm text-destructive">Không tải được danh sách nguồn dữ liệu. Vui lòng thử lại.</p>
            ) : (
              <DatasetPicker datasets={datasets} isLoading={isLoadingDatasets} selectedKey={state.datasetKey} onSelect={handleSelectDataset} />
            )}
          </CardContent>
        </Card>

        {selectedDataset && (
          <>
            <Card>
              <CardHeader>
                <CardTitle>2. Chọn &amp; sắp xếp cột</CardTitle>
              </CardHeader>
              <CardContent>
                <FieldTray fields={selectedDataset.fields} columns={state.columns} onColumnsChange={(columns) => updateState({ columns })} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>3. Bộ lọc</CardTitle>
              </CardHeader>
              <CardContent>
                <FilterBuilder fields={selectedDataset.fields} filters={state.filters} onChange={(filters) => updateState({ filters })} />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>4. Nhóm &amp; sắp xếp</CardTitle>
              </CardHeader>
              <CardContent>
                <GroupSortConfig
                  columns={state.columns}
                  groupBy={state.groupBy}
                  sort={state.sort}
                  onGroupByChange={(groupBy) => updateState({ groupBy })}
                  onSortChange={(sort) => updateState({ sort })}
                />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>5. Kiểu hiển thị</CardTitle>
              </CardHeader>
              <CardContent>
                <ViewConfig
                  columns={state.columns}
                  viewType={state.viewType}
                  chart={state.chart}
                  onViewTypeChange={(viewType) => updateState({ viewType })}
                  onChartChange={(chart) => updateState({ chart })}
                />
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>6. Xem trước &amp; Lưu</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="flex flex-wrap items-end gap-3">
                  <div className="flex flex-col gap-1">
                    <Label className="text-xs">Khoảng thời gian</Label>
                    <Select items={REPORT_DATE_PRESET_LABELS} value={preset} onValueChange={handlePresetChange}>
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
                      value={range.from}
                      onChange={(e) => {
                        setPreset("custom");
                        setRange((r) => ({ ...r, from: e.target.value }));
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
                        setPreset("custom");
                        setRange((r) => ({ ...r, to: e.target.value }));
                      }}
                      className="h-9 w-36"
                    />
                  </div>

                  <div className="ml-auto flex items-center gap-2">
                    <Button type="button" variant="outline" onClick={handlePreview} disabled={!canRun || previewMutation.isPending} className="gap-2">
                      <RefreshCw className={previewMutation.isPending ? "h-4 w-4 animate-spin" : "h-4 w-4"} />
                      Xem trước
                    </Button>
                    <SaveReportDialog
                      isEditing={!!editId}
                      defaultTitle={state.title}
                      defaultVisibility={state.visibility}
                      disabled={!canRun}
                      isSaving={isSaving}
                      onSave={handleSave}
                    />
                  </div>
                </div>

                <PreviewPane
                  result={previewMutation.data}
                  isPending={previewMutation.isPending}
                  isError={previewMutation.isError}
                  hasRun={hasRun}
                  viewType={state.viewType}
                  chart={state.chart}
                />
              </CardContent>
            </Card>
          </>
        )}
      </div>
    </Can>
  );
}

function AccessDenied() {
  return (
    <EmptyState
      variant="generic"
      title="Không có quyền truy cập"
      description="Bạn không có quyền sử dụng Trình tạo báo cáo. Vui lòng liên hệ quản trị viên."
    />
  );
}
