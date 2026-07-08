"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { LayoutDashboard, Pencil, Plus, Trash2 } from "lucide-react";
import { toast } from "sonner";
import { PageHeader } from "@/components/ui/page-header";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { EmptyState } from "@/components/ui/EmptyState";
import { Can } from "@/components/auth/Can";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import type { ReportDashboard } from "@/lib/api/reports";
import { useDeleteReportDashboard, useReportDashboards } from "@/lib/hooks/use-reports";

const VISIBILITY_LABELS: Record<string, string> = {
  TENANT: "Cả phòng khám",
  PRIVATE: "Chỉ mình tôi",
  ROLE: "Theo vai trò",
};

export function DashboardsListClient() {
  const router = useRouter();
  const { data: dashboards = [], isLoading, isError } = useReportDashboards();
  const deleteMutation = useDeleteReportDashboard();
  const [deleteTarget, setDeleteTarget] = useState<ReportDashboard | null>(null);

  function handleDelete() {
    if (!deleteTarget) return;
    deleteMutation.mutate(String(deleteTarget.id), {
      onSuccess: () => {
        toast.success("Đã xoá bảng điều khiển.");
        setDeleteTarget(null);
      },
      onError: () => toast.error("Không xoá được bảng điều khiển. Vui lòng thử lại."),
    });
  }

  return (
    <div className="space-y-4">
      <PageHeader
        title="Bảng điều khiển"
        description="Ghép nhiều báo cáo đã lưu thành 1 màn hình theo dõi tổng quan"
        actions={
          <Can permission="report.build">
            <Button onClick={() => router.push("/reports/dashboards/builder")} className="gap-1.5">
              <Plus className="h-4 w-4" />
              Tạo bảng điều khiển
            </Button>
          </Can>
        }
      />

      {isLoading ? (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-32 w-full" />
          ))}
        </div>
      ) : isError ? (
        <p className="text-sm text-destructive">Không tải được danh sách bảng điều khiển. Vui lòng thử lại.</p>
      ) : dashboards.length === 0 ? (
        <EmptyState
          variant="generic"
          title="Chưa có bảng điều khiển nào"
          description="Tạo bảng điều khiển đầu tiên bằng cách ghép các báo cáo đã lưu."
          action={{ label: "Tạo bảng điều khiển", onClick: () => router.push("/reports/dashboards/builder") }}
        />
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {dashboards.map((dashboard) => (
            <Card key={dashboard.id} className="cursor-pointer transition-shadow hover:shadow-md" onClick={() => router.push(`/reports/dashboards/${dashboard.id}`)}>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <LayoutDashboard className="h-4 w-4 shrink-0 text-accent-primary" aria-hidden="true" />
                  <span className="truncate">{dashboard.title}</span>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-3">
                <div className="flex items-center gap-2">
                  <Badge variant="outline">{dashboard.widgets.length} widget</Badge>
                  <Badge variant="outline">{VISIBILITY_LABELS[dashboard.visibility] ?? dashboard.visibility}</Badge>
                </div>
                <Can permission="report.build">
                  <div className="flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="gap-1.5"
                      onClick={() => router.push(`/reports/dashboards/builder?edit=${dashboard.id}`)}
                    >
                      <Pencil className="h-3.5 w-3.5" />
                      Sửa
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      size="sm"
                      className="gap-1.5 text-destructive"
                      onClick={() => setDeleteTarget(dashboard)}
                    >
                      <Trash2 className="h-3.5 w-3.5" />
                      Xoá
                    </Button>
                  </div>
                </Can>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Xoá bảng điều khiển"
        description={`Bạn có chắc muốn xoá bảng điều khiển "${deleteTarget?.title}"? Hành động này không thể hoàn tác.`}
        variant="destructive"
        isLoading={deleteMutation.isPending}
        onConfirm={handleDelete}
      />
    </div>
  );
}
