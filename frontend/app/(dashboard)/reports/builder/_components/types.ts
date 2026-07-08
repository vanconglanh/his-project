import type {
  DatasetFieldDataType,
  DatasetFieldRole,
  ReportAggregation,
  ReportChartConfig,
  ReportChartType,
  ReportDataset,
  ReportDefinition,
  ReportDefinitionBody,
  ReportFilterOp,
  ReportVisibility,
} from "@/lib/api/reports";

/** Cột đã chọn trong "Cột đã chọn" — giữ nguyên metadata dataset để hiển thị (agg khả dụng, kiểu dữ liệu). */
export interface ColumnDraft {
  field: string;
  label: string;
  role: DatasetFieldRole;
  dataType: DatasetFieldDataType;
  availableAggs: ReportAggregation[];
  agg: ReportAggregation | null;
  isSubtotal: boolean;
}

export interface FilterDraft {
  id: string;
  field: string;
  op: ReportFilterOp;
  /** Giá trị 1 (hoặc danh sách, phân tách bởi dấu phẩy khi op="in") */
  value: string;
  /** Giá trị 2 — chỉ dùng khi op="between" */
  valueTo: string;
}

export interface SortDraft {
  id: string;
  field: string;
  desc: boolean;
}

export interface KpiDraft {
  id: string;
  label: string;
  field: string;
  agg: ReportAggregation;
}

export interface ChartDraft {
  type: ReportChartType;
  dims: string[];
  measure: string;
}

export type ReportBuilderViewType = "TABLE" | "CHART";

export interface BuilderState {
  datasetKey: string | null;
  columns: ColumnDraft[];
  filters: FilterDraft[];
  groupBy: string[];
  sort: SortDraft[];
  kpis: KpiDraft[];
  viewType: ReportBuilderViewType;
  chart: ChartDraft;
  title: string;
  visibility: ReportVisibility;
}

export function newId(): string {
  return Math.random().toString(36).slice(2, 10);
}

export function createEmptyBuilderState(): BuilderState {
  return {
    datasetKey: null,
    columns: [],
    filters: [],
    groupBy: [],
    sort: [],
    kpis: [],
    viewType: "TABLE",
    chart: { type: "bar", dims: [], measure: "" },
    title: "",
    visibility: "TENANT",
  };
}

/** Op hợp lệ theo kiểu dữ liệu — khớp danh sách BE: =,<>,in,between,like,>,<,>=,<= */
export const OP_LABELS: Record<ReportFilterOp, string> = {
  "=": "Bằng",
  "<>": "Khác",
  in: "Trong danh sách",
  between: "Trong khoảng",
  like: "Chứa",
  ">": "Lớn hơn",
  "<": "Nhỏ hơn",
  ">=": "Lớn hơn hoặc bằng",
  "<=": "Nhỏ hơn hoặc bằng",
};

export function getOpsForDataType(dataType: DatasetFieldDataType): ReportFilterOp[] {
  switch (dataType) {
    case "Text":
      return ["like", "=", "<>", "in"];
    case "Enum":
      return ["=", "<>", "in"];
    case "Number":
    case "Money":
      return ["=", "<>", ">", "<", ">=", "<=", "between", "in"];
    case "Date":
    case "DateTime":
      return ["=", ">", "<", ">=", "<=", "between"];
    default:
      return ["=", "<>"];
  }
}

export function buildDefinitionBody(state: BuilderState): ReportDefinitionBody {
  return {
    columns: state.columns.map((c) => ({
      field: c.field,
      label: c.label,
      agg: c.agg,
      is_subtotal: c.isSubtotal,
    })),
    filters: state.filters
      .filter((f) => f.field && f.value.trim() !== "")
      .map((f) => ({
        field: f.field,
        op: f.op,
        value:
          f.op === "between"
            ? [f.value.trim(), f.valueTo.trim()]
            : f.op === "in"
              ? f.value.split(",").map((v) => v.trim()).filter(Boolean)
              : [f.value.trim()],
      })),
    group_by: state.groupBy,
    sort: state.sort.filter((s) => s.field).map((s) => ({ field: s.field, desc: s.desc })),
    kpis: state.kpis
      .filter((k) => k.label.trim() && k.field)
      .map((k) => ({ label: k.label.trim(), field: k.field, agg: k.agg })),
  };
}

export function buildChartConfig(state: BuilderState): ReportChartConfig | null {
  if (state.viewType !== "CHART") return null;
  if (!state.chart.measure || state.chart.dims.length === 0) return null;
  return { type: state.chart.type, dims: state.chart.dims, measure: state.chart.measure };
}

/** Khôi phục BuilderState từ 1 ReportDefinition đã lưu (chế độ Sửa) — cần dataset để tra role/data_type/agg khả dụng. */
export function builderStateFromDefinition(def: ReportDefinition, dataset: ReportDataset | undefined): BuilderState {
  const fieldMeta = new Map((dataset?.fields ?? []).map((f) => [f.key, f]));
  const columns: ColumnDraft[] = def.definition.columns.map((c) => {
    const meta = fieldMeta.get(c.field);
    return {
      field: c.field,
      label: c.label,
      role: meta?.role ?? "DIMENSION",
      dataType: meta?.data_type ?? "Text",
      availableAggs: meta?.aggregations ?? [],
      agg: c.agg,
      isSubtotal: c.is_subtotal,
    };
  });
  return {
    datasetKey: def.dataset_key,
    columns,
    filters: def.definition.filters.map((f) => ({
      id: newId(),
      field: f.field,
      op: f.op,
      value: f.op === "between" ? (f.value[0] ?? "") : f.value.join(", "),
      valueTo: f.op === "between" ? (f.value[1] ?? "") : "",
    })),
    groupBy: def.definition.group_by,
    sort: def.definition.sort.map((s) => ({ id: newId(), field: s.field, desc: s.desc })),
    kpis: def.definition.kpis.map((k) => ({ id: newId(), label: k.label, field: k.field, agg: k.agg })),
    viewType: def.view_type,
    chart: def.chart ?? { type: "bar", dims: [], measure: "" },
    title: def.title,
    visibility: def.visibility,
  };
}
