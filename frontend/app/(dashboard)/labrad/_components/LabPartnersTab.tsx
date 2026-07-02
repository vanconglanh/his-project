"use client";

import { useState } from "react";
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
import { LabPartnerForm } from "@/components/domain/LabPartnerForm";
import { LabPartnerConnectionTest } from "@/components/domain/LabPartnerConnectionTest";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import {
  useLabPartners,
  useCreateLabPartner,
  useUpdateLabPartner,
  useDeleteLabPartner,
  useRotateLabPartnerCredentials,
} from "@/lib/hooks/use-lab-partners";
import type { LabPartner } from "@/lib/api/lab-partners";

export function LabPartnersTab() {
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [editing, setEditing] = useState<LabPartner | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<LabPartner | null>(null);

  const { data: partners, isLoading } = useLabPartners();
  const createMutation = useCreateLabPartner();
  const updateMutation = useUpdateLabPartner(editing?.id ?? "");
  const deleteMutation = useDeleteLabPartner();

  if (isLoading) {
    return (
      <div className="space-y-2">
        {Array.from({ length: 3 }).map((_, i) => (
          <Skeleton key={i} className="h-12 w-full rounded-md" />
        ))}
      </div>
    );
  }

  const rows = partners ?? [];

  return (
    <div className="space-y-4">
      <div className="flex justify-end">
        <Button onClick={() => { setEditing(null); setDrawerOpen(true); }}>
          + Thêm đối tác lab
        </Button>
      </div>

      {rows.length === 0 ? (
        <div className="flex flex-col items-center justify-center py-16 text-muted-foreground">
          <span className="text-4xl mb-3">🔬</span>
          <p className="font-medium">Chưa có đối tác lab</p>
          <p className="text-sm mt-1">Thêm đối tác để tích hợp gửi / nhận kết quả</p>
        </div>
      ) : (
        <div className="rounded-md border overflow-auto">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead>Mã</TableHead>
                <TableHead>Tên</TableHead>
                <TableHead>Endpoint</TableHead>
                <TableHead>Giao thức</TableHead>
                <TableHead>Trạng thái</TableHead>
                <TableHead>Kết nối</TableHead>
                <TableHead className="text-right">Thao tác</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((partner) => (
                <TableRow key={partner.id}>
                  <TableCell className="font-mono text-xs">{partner.code}</TableCell>
                  <TableCell className="font-medium">{partner.name}</TableCell>
                  <TableCell className="max-w-xs truncate text-xs text-muted-foreground">
                    {partner.endpoint_url}
                  </TableCell>
                  <TableCell>
                    <Badge variant="outline">{partner.transport}</Badge>
                  </TableCell>
                  <TableCell>
                    <Badge variant={partner.status === "ACTIVE" ? "default" : "secondary"}>
                      {partner.status === "ACTIVE" ? "Hoạt động" : "Tạm ngưng"}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <LabPartnerConnectionTest partnerId={partner.id} />
                  </TableCell>
                  <TableCell className="text-right">
                    <div className="flex gap-1 justify-end flex-wrap">
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => { setEditing(partner); setDrawerOpen(true); }}
                      >
                        Sửa
                      </Button>
                      <RotateButton partnerId={partner.id} />
                      <Button
                        variant="destructive"
                        size="sm"
                        onClick={() => setDeleteTarget(partner)}
                      >
                        Xoá
                      </Button>
                    </div>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}

      {/* Drawer */}
      <Sheet open={drawerOpen} onOpenChange={setDrawerOpen}>
        <SheetContent side="right" className="w-full sm:max-w-lg overflow-y-auto">
          <SheetHeader>
            <SheetTitle>{editing ? "Cập nhật đối tác lab" : "Thêm đối tác lab"}</SheetTitle>
          </SheetHeader>
          <div className="mt-6">
            <LabPartnerForm
              existing={editing ?? undefined}
              onSubmit={async (data) => {
                if (editing) {
                  await updateMutation.mutateAsync(data as Parameters<typeof updateMutation.mutateAsync>[0]);
                } else {
                  await createMutation.mutateAsync(data as Parameters<typeof createMutation.mutateAsync>[0]);
                }
                setDrawerOpen(false);
              }}
              onCancel={() => setDrawerOpen(false)}
              isSubmitting={createMutation.isPending || updateMutation.isPending}
            />
          </div>
        </SheetContent>
      </Sheet>

      {/* Delete confirm */}
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Xoá đối tác lab"
        description={`Bạn có chắc muốn xoá đối tác "${deleteTarget?.name}"? Hành động này không thể hoàn tác.`}
        onConfirm={() => {
          if (deleteTarget) {
            deleteMutation.mutate(deleteTarget.id);
            setDeleteTarget(null);
          }
        }}
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
}

function RotateButton({ partnerId }: { partnerId: string }) {
  const rotateMutation = useRotateLabPartnerCredentials(partnerId);
  return (
    <Button
      variant="outline"
      size="sm"
      onClick={() => rotateMutation.mutate()}
      disabled={rotateMutation.isPending}
    >
      {rotateMutation.isPending ? "Xoay..." : "Rotate Key"}
    </Button>
  );
}
