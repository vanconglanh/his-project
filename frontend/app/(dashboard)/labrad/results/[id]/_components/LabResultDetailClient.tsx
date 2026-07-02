"use client";

import { useState } from "react";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { FlagBadge } from "@/components/domain/FlagBadge";
import { LabResultTrendChart } from "@/components/domain/LabResultTrendChart";
import { LabResultForm } from "@/components/domain/LabResultForm";
import {
  useLabResults,
  useVerifyLabResult,
  useUnverifyLabResult,
  useUpdateLabResult,
  useLabResultTrend,
} from "@/lib/hooks/use-lab-results";
import { getLabResultPdfUrl } from "@/lib/api/lab-results";

interface Props {
  id: string;
}

const STATUS_LABELS: Record<string, string> = {
  DRAFT: "Nháp",
  VERIFIED: "Đã xác thực",
  AMENDED: "Đã sửa",
};

export function LabResultDetailClient({ id }: Props) {
  const { data, isLoading } = useLabResults({ page: 1, page_size: 100 });
  const [amendDrawer, setAmendDrawer] = useState(false);

  const verifyMutation = useVerifyLabResult();
  const unverifyMutation = useUnverifyLabResult();

  // Find single result by id from list (in production would call /lab-results/:id if available)
  const result = data?.data?.find((r) => r.id === id);

  const { data: trend } = useLabResultTrend(
    result?.patient_id ?? "",
    result?.test_code ?? "",
    !!result
  );

  const updateMutation = useUpdateLabResult(id);

  if (isLoading) {
    return (
      <div className="space-y-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-48 w-full rounded-xl" />
        <Skeleton className="h-64 w-full rounded-xl" />
      </div>
    );
  }

  if (!result) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-muted-foreground">
        <p className="text-lg font-medium">Không tìm thấy kết quả</p>
        <Link href="/labrad" className="mt-4 text-sm underline">
          Quay lại danh sách
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Link href="/labrad" className="hover:underline">
          CLS
        </Link>
        <span>/</span>
        <span>Chi tiết kết quả</span>
      </div>

      {/* Header */}
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-xl font-bold">{result.test_name}</h2>
          <p className="text-sm text-muted-foreground">
            Mã chỉ số: {result.test_code} &middot; Nguồn: {result.source}
          </p>
          <p className="text-sm text-muted-foreground mt-1">
            Thực hiện: {format(new Date(result.performed_at), "dd/MM/yyyy HH:mm", { locale: vi })}
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          {result.status === "DRAFT" && (
            <Button onClick={() => verifyMutation.mutate(result.id)} disabled={verifyMutation.isPending}>
              Xác thực
            </Button>
          )}
          {result.status === "VERIFIED" && (
            <>
              <Button
                variant="outline"
                onClick={() => unverifyMutation.mutate(result.id)}
                disabled={unverifyMutation.isPending}
              >
                Hủy xác thực
              </Button>
              <Button variant="outline" onClick={() => window.open(getLabResultPdfUrl(result.id), "_blank")}>
                In PDF
              </Button>
            </>
          )}
          <Button variant="outline" onClick={() => setAmendDrawer(true)}>
            Sửa / Amend
          </Button>
        </div>
      </div>

      {/* Result Card */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-3">
            Kết quả
            <FlagBadge flag={result.flag} />
            <Badge variant={result.status === "VERIFIED" ? "default" : "secondary"}>
              {STATUS_LABELS[result.status]}
            </Badge>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-2 gap-4 sm:grid-cols-3">
            <div>
              <dt className="text-xs text-muted-foreground">Giá trị</dt>
              <dd className="font-mono text-lg font-bold">{result.value}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Đơn vị</dt>
              <dd>{result.unit || "—"}</dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Khoảng tham chiếu</dt>
              <dd>
                {result.reference_range_low != null && result.reference_range_high != null
                  ? `${result.reference_range_low} – ${result.reference_range_high} ${result.unit}`
                  : "—"}
              </dd>
            </div>
            <div>
              <dt className="text-xs text-muted-foreground">Phương pháp</dt>
              <dd>{result.method || "—"}</dd>
            </div>
            {result.verified_at && (
              <div>
                <dt className="text-xs text-muted-foreground">Xác thực lúc</dt>
                <dd>{format(new Date(result.verified_at), "dd/MM/yyyy HH:mm", { locale: vi })}</dd>
              </div>
            )}
            {result.note && (
              <div className="col-span-2 sm:col-span-3">
                <dt className="text-xs text-muted-foreground">Ghi chú</dt>
                <dd>{result.note}</dd>
              </div>
            )}
          </dl>
        </CardContent>
      </Card>

      {/* Trend Chart */}
      {trend && trend.points.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Lịch sử xu hướng</CardTitle>
          </CardHeader>
          <CardContent>
            <LabResultTrendChart data={trend} />
          </CardContent>
        </Card>
      )}

      {/* Amend Drawer */}
      <Sheet open={amendDrawer} onOpenChange={setAmendDrawer}>
        <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto px-6 pb-6">
          <SheetHeader>
            <SheetTitle>Sửa kết quả (Amend)</SheetTitle>
          </SheetHeader>
          <div className="mt-6">
            <LabResultForm
              existing={result}
              onSubmit={async (data) => {
                await updateMutation.mutateAsync(data as Parameters<typeof updateMutation.mutateAsync>[0]);
                setAmendDrawer(false);
              }}
              onCancel={() => setAmendDrawer(false)}
              isSubmitting={updateMutation.isPending}
            />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
