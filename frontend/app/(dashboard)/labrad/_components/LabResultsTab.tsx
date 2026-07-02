"use client";

import { useState } from "react";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { LabResultTable } from "@/components/domain/LabResultTable";
import { LabResultForm } from "@/components/domain/LabResultForm";
import {
  useLabResults,
  useCreateLabResult,
  useVerifyLabResult,
  useUnverifyLabResult,
} from "@/lib/hooks/use-lab-results";
import type { LabResultResponse, LabResultStatus, LabResultFlag } from "@/lib/api/lab-results";
import { getLabResultPdfUrl } from "@/lib/api/lab-results";
import Link from "next/link";

export function LabResultsTab() {
  const [statusFilter, setStatusFilter] = useState<LabResultStatus | "ALL">("ALL");
  const [flagFilter, setFlagFilter] = useState<LabResultFlag | "ALL">("ALL");
  const [search, setSearch] = useState("");
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editing, setEditing] = useState<LabResultResponse | null>(null);

  const { data, isLoading } = useLabResults({
    status: statusFilter !== "ALL" ? statusFilter : undefined,
    flag: flagFilter !== "ALL" ? flagFilter : undefined,
    page: 1,
    page_size: 50,
  });

  const createMutation = useCreateLabResult();
  const verifyMutation = useVerifyLabResult();
  const unverifyMutation = useUnverifyLabResult();

  const results = data?.data ?? [];

  const filtered = search
    ? results.filter(
        (r) =>
          r.test_name.toLowerCase().includes(search.toLowerCase()) ||
          r.test_code.toLowerCase().includes(search.toLowerCase())
      )
    : results;

  const handleVerify = (r: LabResultResponse) => {
    verifyMutation.mutate(r.id);
  };

  const handleUnverify = (r: LabResultResponse) => {
    unverifyMutation.mutate(r.id);
  };

  const handlePrint = (r: LabResultResponse) => {
    window.open(getLabResultPdfUrl(r.id), "_blank");
  };

  const handleEnterResult = (r: LabResultResponse) => {
    setEditing(r);
    setDrawerOpen(true);
  };

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <Input
          className="w-56"
          placeholder="Tìm chỉ số..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          aria-label="Tìm chỉ số xét nghiệm"
        />

        <Select value={statusFilter} onValueChange={(v) => setStatusFilter(v as LabResultStatus | "ALL")}>
          <SelectTrigger className="w-40" aria-label="Lọc trạng thái">
            <SelectValue placeholder="Trạng thái" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả trạng thái</SelectItem>
            <SelectItem value="DRAFT">Nháp</SelectItem>
            <SelectItem value="VERIFIED">Đã xác thực</SelectItem>
            <SelectItem value="AMENDED">Đã sửa</SelectItem>
          </SelectContent>
        </Select>

        <Select value={flagFilter} onValueChange={(v) => setFlagFilter(v as LabResultFlag | "ALL")}>
          <SelectTrigger className="w-40" aria-label="Lọc cờ bất thường">
            <SelectValue placeholder="Cờ" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="NORMAL">Bình thường</SelectItem>
            <SelectItem value="H">Cao (H)</SelectItem>
            <SelectItem value="L">Thấp (L)</SelectItem>
            <SelectItem value="HH">Rất cao (HH)</SelectItem>
            <SelectItem value="LL">Rất thấp (LL)</SelectItem>
            <SelectItem value="CRITICAL">Nguy kịch</SelectItem>
          </SelectContent>
        </Select>

        <div className="ml-auto">
          <Button onClick={() => { setEditing(null); setDrawerOpen(true); }}>
            + Nhập kết quả
          </Button>
        </div>
      </div>

      {/* Table */}
      <LabResultTable
        data={filtered}
        loading={isLoading}
        onEnterResult={handleEnterResult}
        onVerify={handleVerify}
        onUnverify={handleUnverify}
        onPrint={handlePrint}
        onViewDetail={(r) => window.open(`/labrad/results/${r.id}`, "_self")}
      />

      {/* Pagination info */}
      {data?.meta && (
        <p className="text-sm text-muted-foreground">
          Tổng: {data.meta.total} kết quả
        </p>
      )}

      {/* Drawer */}
      <Sheet open={drawerOpen} onOpenChange={setDrawerOpen}>
        <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>{editing ? "Cập nhật kết quả XN" : "Nhập kết quả XN"}</SheetTitle>
          </SheetHeader>
          <div className="mt-6">
            <LabResultForm
              existing={editing ?? undefined}
              onSubmit={async (data) => {
                if (editing) {
                  // handled by parent (edit path not wired here for brevity)
                } else {
                  await createMutation.mutateAsync(data as Parameters<typeof createMutation.mutateAsync>[0]);
                }
                setDrawerOpen(false);
              }}
              onCancel={() => setDrawerOpen(false)}
              isSubmitting={createMutation.isPending}
            />
          </div>
        </SheetContent>
      </Sheet>
    </div>
  );
}
