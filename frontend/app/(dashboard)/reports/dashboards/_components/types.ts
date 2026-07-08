import type { DashboardWidgetType, ReportDashboardWidget, ReportVisibility } from "@/lib/api/reports";

export function newWidgetId(): string {
  return Math.random().toString(36).slice(2, 10);
}

/** Draft widget trong builder — có id nội bộ để reorder/sửa, x/y tính lại tự động khi lưu. */
export interface DashboardWidgetDraft {
  id: string;
  report_code: string;
  title: string;
  widget_type: DashboardWidgetType;
  w: 4 | 6 | 12;
  h: 1 | 2;
}

export interface DashboardBuilderState {
  title: string;
  visibility: ReportVisibility;
  widgets: DashboardWidgetDraft[];
}

export function createEmptyDashboardState(): DashboardBuilderState {
  return { title: "", visibility: "TENANT", widgets: [] };
}

/**
 * Sắp cột (x) và hàng (y) theo lưới 12 cột — đóng gói tuần tự trái→phải, wrap khi vượt 12.
 * Đơn giản hoá theo yêu cầu ("ưu tiên đơn giản chạy được") — không cho kéo thả vị trí thủ công.
 */
export function computeWidgetLayout(widgets: DashboardWidgetDraft[]): ReportDashboardWidget[] {
  let cursorX = 0;
  let cursorY = 0;
  let rowHeight = 0;
  const result: ReportDashboardWidget[] = [];
  for (const widget of widgets) {
    if (cursorX + widget.w > 12) {
      cursorX = 0;
      cursorY += rowHeight;
      rowHeight = 0;
    }
    result.push({
      report_code: widget.report_code,
      title: widget.title,
      widget_type: widget.widget_type,
      w: widget.w,
      h: widget.h,
      x: cursorX,
      y: cursorY,
    });
    cursorX += widget.w;
    rowHeight = Math.max(rowHeight, widget.h);
  }
  return result;
}

export function draftsFromWidgets(widgets: ReportDashboardWidget[]): DashboardWidgetDraft[] {
  return widgets.map((w) => ({
    id: newWidgetId(),
    report_code: w.report_code,
    title: w.title,
    widget_type: w.widget_type,
    w: (w.w === 4 || w.w === 12 ? w.w : 6) as 4 | 6 | 12,
    h: (w.h === 2 ? 2 : 1) as 1 | 2,
  }));
}

export const WIDGET_TYPE_LABELS: Record<DashboardWidgetType, string> = {
  TABLE: "Bảng",
  CHART: "Biểu đồ",
  KPI: "Chỉ số KPI",
};

export const WIDGET_WIDTH_LABELS: Record<4 | 6 | 12, string> = {
  4: "1/3 hàng",
  6: "1/2 hàng",
  12: "Toàn hàng",
};

export const WIDGET_HEIGHT_LABELS: Record<1 | 2, string> = {
  1: "Thường",
  2: "Cao",
};
