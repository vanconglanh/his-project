"use client";

import { useState } from "react";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { RadResultForm } from "@/components/domain/RadResultForm";
import { DicomUploadZone } from "@/components/domain/DicomUploadZone";
import {
  useRadResults,
  useCreateRadResult,
  useVerifyRadResult,
  useUploadDicom,
} from "@/lib/hooks/use-rad-results";
import type { RadResultResponse } from "@/lib/api/rad-results";
import { getRadResultPdfUrl } from "@/lib/api/rad-results";

const STATUS_LABELS: Record<string, string> = {
  DRAFT: "Nháp",
  VERIFIED: "Đã ký",
  AMENDED: "Đã sửa",
};

const STATUS_VARIANT: Record<string, "default" | "secondary" | "outline"> = {
  DRAFT: "secondary",
  VERIFIED: "default",
  AMENDED: "outline",
};

export function RadResultsTab() {
  const [drawerMode, setDrawerMode] = useState<"form" | "dicom" | null>(null);
  const [selected, setSelected] = useState<RadResultResponse | null>(null);

  const { data: results, isLoading } = useRadResults({ page: 1, page_size: 50 });
  const createMutation = useCreateRadResult();
  const verifyMutation = useVerifyRadResult();
  const uploadDicomMutation = useUploadDicom(selected?.id ?? "");

  const rows = results ?? [];

  const openDicom = (r: RadResultResponse) => {
    setSelected(r);
    setDrawerMode("dicom");
  };

  const openEnter = (r?: RadResultResponse) => {
    setSelected(r ?? null);
    setDrawerMode("form");
  };

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 4 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full rounded-md" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => openEnter()}>+ Nhập kết quả CĐHA</Button>
      </div>

      {rows.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground">
          <span className="text-4xl mb-3">🩻</span>
          <p className="font-medium">Chưa có kết quả CĐHA</p>
        </div>
      ) : (
        <div className="rounded-md border overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Kỹ thuật</TableHead>
                <TableHead>Kết luận</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>DICOM</TableHead>
                <TableHead>Thực hiện</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id}>
                  <TableCell className="font-medium">{row.modality}</TableCell>
                  <TableCell className="max-w-xs truncate">{row.conclusion}</TableCell>
                  <TableCell>
                    <Badge variant={STATUS_VARIANT[row.status] ?? "secondary"}>
                      {STATUS_LABELS[row.status] ?? row.status}
                    </Badge>
                  </TableCell>
                  <TableCell>{row.dicom_count} file</TableCell>
                  <TableCell className="text-xs text-muted-foreground">
                    {format(new Date(row.performed_at), "dd/MM/yyyy HH:mm", { locale: vi })}
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex gap-1 justify-end flex-wrap">
                      <Button variant="outline" size="sm" onClick={() => openDicom(row)}>
                        Upload DICOM
                      </Button>
                      {row.status === "DRAFT" && (
                        <Button
                          variant="default"
                          size="sm"
                          onClick={() => verifyMutation.mutate(row.id)}
                          disabled={verifyMutation.isPending}
                        >
                          Ký phát hành
                        </Button>
                      )}
                      {row.status === "VERIFIED" && row.signed_pdf_url && (
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => window.open(getRadResultPdfUrl(row.id), "_blank")}
                        >
                          In PDF
                        </Button>
                      )}
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Drawer */}
      <Sheet open={!!drawerMode} onOpenChange={(open) => !open && setDrawerMode(null)}>
        <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">
          {drawerMode === "form" && (
            <>
              <SheetHeader>
                <SheetTitle>Nhập kết quả CĐHA</SheetTitle>
              </SheetHeader>
              <div className="mt-6">
                <RadResultForm
                  onSubmit={async (data) => {
                    await createMutation.mutateAsync(data as Parameters<typeof createMutation.mutateAsync>[0]);
                    setDrawerMode(null);
                  }}
                  onCancel={() => setDrawerMode(null)}
                  isSubmitting={createMutation.isPending}
                />
              </div>
            </>
          )}

          {drawerMode === "dicom" && selected && (
            <>
              <SheetHeader>
                <SheetTitle>Upload ảnh DICOM — {selected.modality}</SheetTitle>
              </SheetHeader>
              <div className="mt-6">
                <DicomUploadZone
                  onUpload={async (files) => {
                    await uploadDicomMutation.mutateAsync(files);
                    setDrawerMode(null);
                  }}
                  isUploading={uploadDicomMutation.isPending}
                />
              </div>
            </>
          )}
        </SheetContent>
      </Sheet>
    </div>
  );
}
