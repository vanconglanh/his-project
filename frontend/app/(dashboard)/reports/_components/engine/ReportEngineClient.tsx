"use client";

import { useEffect, useMemo, useState } from "react";
import { useRouter } from "next/navigation";
import { PageHeader } from "@/components/ui/page-header";
import { Skeleton } from "@/components/ui/skeleton";
import { useReportCatalog } from "@/lib/hooks/use-reports";
import { useReportRecentFavorites } from "@/lib/hooks/use-report-recent-favorites";
import { ReportSidebar } from "@/components/domain/reports-engine/ReportSidebar";
import { ReportRunner } from "@/components/domain/reports-engine/ReportRunner";
import { ReportsPageClient } from "../ReportsPageClient";

const DASHBOARD_CODE = "__dashboard__";

interface ReportEngineClientProps {
  /** Mã báo cáo chọn sẵn qua deep-link ?report=code — rỗng/không hợp lệ → về Tổng quan (Dashboard). */
  initialReportCode?: string;
}

/**
 * Màn hình báo cáo generic — dùng chung cho toàn bộ danh mục config-driven (GET /reports/catalog).
 * Sidebar 2 cấp (nhóm → báo cáo) bên trái, ReportRunner (filter + KPI + grid + export) bên phải.
 * Mục "Tổng quan (Dashboard)" giữ nguyên UI tabs chart cũ (ReportsPageClient) để không mất tính năng.
 */
export function ReportEngineClient({ initialReportCode }: ReportEngineClientProps) {
  const router = useRouter();
  const { data: catalog = [], isLoading, isError } = useReportCatalog();
  const { touchRecent } = useReportRecentFavorites();
  const [selectedCode, setSelectedCode] = useState<string>(initialReportCode || DASHBOARD_CODE);

  const selectedDescriptor = useMemo(
    () => catalog.find((item) => item.code === selectedCode),
    [catalog, selectedCode]
  );

  // Deep-link ?report=code không khớp báo cáo nào trong danh mục sau khi tải xong → về Dashboard an toàn
  useEffect(() => {
    if (!isLoading && !isError && selectedCode !== DASHBOARD_CODE && catalog.length > 0 && !selectedDescriptor) {
      setSelectedCode(DASHBOARD_CODE);
    }
  }, [isLoading, isError, catalog, selectedCode, selectedDescriptor]);

  function handleSelectDashboard() {
    setSelectedCode(DASHBOARD_CODE);
    router.replace("/reports", { scroll: false });
  }

  function handleSelectReport(code: string) {
    setSelectedCode(code);
    touchRecent(code);
    router.replace(`/reports?report=${code}`, { scroll: false });
  }

  const isDashboard = selectedCode === DASHBOARD_CODE;
  const title = isDashboard ? "Báo cáo & Thống kê" : (selectedDescriptor?.title ?? "Báo cáo");
  const description = isDashboard
    ? "Phân tích doanh thu, lâm sàng và dược"
    : 'Chọn bộ lọc và bấm "Lấy dữ liệu" để xem báo cáo chi tiết';

  return (
    <div className="space-y-4">
      <PageHeader title={title} description={description} />
      <div className="flex items-start gap-4">
        <ReportSidebar
          catalog={catalog}
          isLoading={isLoading}
          isError={isError}
          selectedCode={selectedCode}
          onSelectDashboard={handleSelectDashboard}
          onSelectReport={handleSelectReport}
        />
        <div className="flex-1 min-w-0">
          {isDashboard ? (
            <ReportsPageClient />
          ) : selectedDescriptor ? (
            <ReportRunner key={selectedDescriptor.code} descriptor={selectedDescriptor} />
          ) : (
            <div className="space-y-3">
              <Skeleton className="h-9 w-full" />
              <Skeleton className="h-64 w-full" />
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
