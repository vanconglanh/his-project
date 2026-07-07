"use client";

import { useEffect, useMemo, useState } from "react";
import { LayoutDashboard, Search, Star, X } from "lucide-react";
import { cn } from "@/lib/utils";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import type { ReportCatalogItem } from "@/lib/api/reports";
import { getReportGroupLabel, getReportIcon } from "./report-icon-map";
import { useReportRecentFavorites } from "@/lib/hooks/use-report-recent-favorites";

const DASHBOARD_CODE = "__dashboard__";

interface ReportSidebarProps {
  catalog: ReportCatalogItem[];
  isLoading: boolean;
  isError: boolean;
  selectedCode: string;
  onSelectDashboard: () => void;
  onSelectReport: (code: string) => void;
}

function useDebouncedValue(value: string, delayMs: number): string {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}

export function ReportSidebar({
  catalog,
  isLoading,
  isError,
  selectedCode,
  onSelectDashboard,
  onSelectReport,
}: ReportSidebarProps) {
  const [search, setSearch] = useState("");
  const debouncedSearch = useDebouncedValue(search, 200);
  const { items: recentFavoriteItems, toggleFavorite, isFavorite } = useReportRecentFavorites();

  const byCode = useMemo(() => {
    const map = new Map<string, ReportCatalogItem>();
    for (const item of catalog) map.set(item.code, item);
    return map;
  }, [catalog]);

  const recentFavoriteReports = recentFavoriteItems
    .map((entry) => ({ entry, report: byCode.get(entry.code) }))
    .filter((x): x is { entry: (typeof recentFavoriteItems)[number]; report: ReportCatalogItem } => !!x.report);

  const normalizedSearch = debouncedSearch.trim().toLocaleLowerCase("vi");

  const groups = useMemo(() => {
    const filtered = normalizedSearch
      ? catalog.filter((item) => item.title.toLocaleLowerCase("vi").includes(normalizedSearch))
      : catalog;

    const byGroup = new Map<string, ReportCatalogItem[]>();
    for (const item of filtered) {
      const list = byGroup.get(item.group) ?? [];
      list.push(item);
      byGroup.set(item.group, list);
    }
    for (const list of byGroup.values()) {
      list.sort((a, b) => a.group_order - b.group_order || a.title.localeCompare(b.title, "vi"));
    }
    return Array.from(byGroup.entries())
      .map(([group, items]) => ({
        group,
        order: Math.min(...items.map((i) => i.group_order)),
        items,
      }))
      .sort((a, b) => a.order - b.order);
  }, [catalog, normalizedSearch]);

  return (
    <aside className="flex w-72 shrink-0 flex-col gap-3 rounded-lg border bg-card p-3 sticky top-0 max-h-[calc(100vh-140px)]">
      <div className="relative shrink-0">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-muted-foreground" />
        <Input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Tìm báo cáo..."
          className="pl-8 pr-7 h-9"
          aria-label="Tìm báo cáo"
        />
        {search && (
          <button
            type="button"
            onClick={() => setSearch("")}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-muted-foreground hover:text-foreground"
            aria-label="Xóa tìm kiếm"
          >
            <X className="h-3.5 w-3.5" />
          </button>
        )}
      </div>

      <nav className="flex-1 min-h-0 overflow-y-auto space-y-4 pr-1" aria-label="Danh mục báo cáo">
        {/* Tổng quan (Dashboard) */}
        <button
          type="button"
          onClick={onSelectDashboard}
          className={cn(
            "flex w-full items-center gap-2 rounded-md px-2.5 py-2 text-sm font-medium transition-colors min-h-11",
            selectedCode === DASHBOARD_CODE
              ? "bg-primary text-primary-foreground"
              : "text-foreground hover:bg-muted"
          )}
          aria-current={selectedCode === DASHBOARD_CODE ? "page" : undefined}
        >
          <LayoutDashboard className="h-4 w-4 shrink-0" aria-hidden="true" />
          <span className="truncate">Tổng quan (Dashboard)</span>
        </button>

        {/* Yêu thích / Gần dùng */}
        {recentFavoriteReports.length > 0 && !normalizedSearch && (
          <div>
            <p className="px-2.5 mb-1 text-xs font-medium text-muted-foreground uppercase tracking-wider">
              ⭐ Yêu thích / Gần dùng
            </p>
            <ul className="space-y-0.5">
              {recentFavoriteReports.map(({ entry, report }) => (
                <ReportSidebarItem
                  key={`recent-${report.code}`}
                  report={report}
                  active={selectedCode === report.code}
                  pinned={entry.pinned}
                  onSelect={() => onSelectReport(report.code)}
                  onToggleFavorite={() => toggleFavorite(report.code)}
                />
              ))}
            </ul>
          </div>
        )}

        {isLoading && (
          <div className="space-y-2 px-1">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-9 w-full" />
            ))}
          </div>
        )}

        {isError && (
          <p className="px-2.5 text-sm text-destructive">
            Không tải được danh mục báo cáo. Vui lòng thử lại.
          </p>
        )}

        {!isLoading &&
          !isError &&
          groups.map((groupEntry) => (
            <div key={groupEntry.group}>
              <p className="px-2.5 mb-1 text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {getReportGroupLabel(groupEntry.group)}
              </p>
              <ul className="space-y-0.5">
                {groupEntry.items.map((report) => (
                  <ReportSidebarItem
                    key={report.code}
                    report={report}
                    active={selectedCode === report.code}
                    pinned={isFavorite(report.code)}
                    onSelect={() => onSelectReport(report.code)}
                    onToggleFavorite={() => toggleFavorite(report.code)}
                  />
                ))}
              </ul>
            </div>
          ))}

        {!isLoading && !isError && groups.length === 0 && (
          <p className="px-2.5 text-sm text-muted-foreground">
            Không tìm thấy báo cáo phù hợp.
          </p>
        )}
      </nav>
    </aside>
  );
}

interface ReportSidebarItemProps {
  report: ReportCatalogItem;
  active: boolean;
  pinned: boolean;
  onSelect: () => void;
  onToggleFavorite: () => void;
}

function ReportSidebarItem({ report, active, pinned, onSelect, onToggleFavorite }: ReportSidebarItemProps) {
  const Icon = getReportIcon(report.icon);
  return (
    <li>
      <div
        className={cn(
          "group flex w-full items-center gap-2 rounded-md pl-2.5 pr-1 text-sm transition-colors min-h-11",
          active ? "bg-primary text-primary-foreground" : "text-foreground hover:bg-muted"
        )}
      >
        <button
          type="button"
          onClick={onSelect}
          className="flex flex-1 min-w-0 items-center gap-2 py-2 text-left"
          aria-current={active ? "page" : undefined}
        >
          <Icon className="h-4 w-4 shrink-0" aria-hidden="true" />
          <span className="truncate">{report.title}</span>
        </button>
        <Button
          type="button"
          variant="ghost"
          size="icon-sm"
          onClick={(e) => {
            e.stopPropagation();
            onToggleFavorite();
          }}
          aria-label={pinned ? "Bỏ ghim yêu thích" : "Ghim yêu thích"}
          className={cn(
            active ? "hover:bg-primary-foreground/20" : "",
            pinned ? "opacity-100" : "opacity-0 group-hover:opacity-100 focus-visible:opacity-100"
          )}
        >
          <Star className={cn("h-3.5 w-3.5", pinned && "fill-current text-[color:var(--status-warning)]")} />
        </Button>
      </div>
    </li>
  );
}
