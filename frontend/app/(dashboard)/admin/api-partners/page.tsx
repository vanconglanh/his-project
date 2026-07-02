"use client";

import { useState } from "react";
import { Plus, RefreshCw, Trash2, TestTube, BarChart2, FileText, Edit } from "lucide-react";
import { format, parseISO } from "date-fns";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import {
  Drawer,
  DrawerContent,
  DrawerHeader,
  DrawerTitle,
} from "@/components/ui/drawer";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Input } from "@/components/ui/input";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { ApiPartnerForm } from "@/components/domain/ApiPartnerForm";
import { ApiPartnerKeyDisplay } from "@/components/domain/ApiPartnerKeyDisplay";
import { ApiPartnerUsageChart } from "@/components/domain/ApiPartnerUsageChart";
import { ApiPartnerRequestLogsTable } from "@/components/domain/ApiPartnerRequestLogsTable";
import {
  useApiPartners,
  useCreateApiPartner,
  useUpdateApiPartner,
  useDeleteApiPartner,
  useRegenerateApiKey,
  useTestApiPartnerCall,
} from "@/lib/hooks/use-api-partners";
import type { ApiPartnerResponse, ApiPartnerStatus } from "@/lib/api/api-partners";

const STATUS_BADGE: Record<ApiPartnerStatus, { label: string; variant: "default" | "secondary" | "destructive" | "outline" }> = {
  ACTIVE: { label: "Hoạt động", variant: "default" },
  DISABLED: { label: "Tắt", variant: "secondary" },
  EXPIRED: { label: "Hết hạn", variant: "destructive" },
};

export default function ApiPartnersPage() {
  const [q, setQ] = useState("");
  const [statusFilter, setStatusFilter] = useState<ApiPartnerStatus | "ALL">("ALL");
  const [showCreate, setShowCreate] = useState(false);
  const [newApiKey, setNewApiKey] = useState<string | null>(null);
  const [editPartner, setEditPartner] = useState<ApiPartnerResponse | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ApiPartnerResponse | null>(null);
  const [regenerateTarget, setRegenerateTarget] = useState<ApiPartnerResponse | null>(null);
  const [drawerPartner, setDrawerPartner] = useState<ApiPartnerResponse | null>(null);

  const { data, isLoading } = useApiPartners(
    statusFilter !== "ALL" ? { q, status: statusFilter } : { q }
  );
  const createMutation = useCreateApiPartner();
  const updateMutation = useUpdateApiPartner(editPartner?.id ?? "");
  const deleteMutation = useDeleteApiPartner();
  const regenMutation = useRegenerateApiKey();
  const testMutation = useTestApiPartnerCall();

  function handleCreate(form: Parameters<typeof createMutation.mutate>[0]) {
    createMutation.mutate(form, {
      onSuccess: (res) => {
        setShowCreate(false);
        setNewApiKey(res.api_key_plain);
      },
    });
  }

  function handleUpdate(form: Parameters<typeof updateMutation.mutate>[0]) {
    updateMutation.mutate(form, {
      onSuccess: () => setEditPartner(null),
    });
  }

  function handleDelete() {
    if (!deleteTarget) return;
    deleteMutation.mutate(deleteTarget.id, {
      onSuccess: () => setDeleteTarget(null),
    });
  }

  function handleRegenerate() {
    if (!regenerateTarget) return;
    regenMutation.mutate(regenerateTarget.id, {
      onSuccess: (res) => {
        setRegenerateTarget(null);
        setNewApiKey(res.api_key_plain);
      },
    });
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold tracking-tight">API Partners</h1>
          <p className="text-sm text-muted-foreground">Quản lý đối tác tích hợp Public API</p>
        </div>
        <Button onClick={() => setShowCreate(true)}>
          <Plus className="mr-2 h-4 w-4" />
          Tạo partner mới
        </Button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3">
        <Input
          placeholder="Tìm kiếm tên, email..."
          value={q}
          onChange={(e) => setQ(e.target.value)}
          className="max-w-xs"
        />
        <Select
          value={statusFilter}
          onValueChange={(v) => setStatusFilter(v as ApiPartnerStatus | "ALL")}
        >
          <SelectTrigger className="w-36">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="ALL">Tất cả</SelectItem>
            <SelectItem value="ACTIVE">Hoạt động</SelectItem>
            <SelectItem value="DISABLED">Tắt</SelectItem>
            <SelectItem value="EXPIRED">Hết hạn</SelectItem>
          </SelectContent>
        </Select>
      </div>

      {/* Table */}
      <div className="rounded-lg border">
        {isLoading ? (
          <div className="space-y-3 p-4">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-12 animate-pulse rounded bg-muted" />
            ))}
          </div>
        ) : !data?.data?.length ? (
          <div className="py-16 text-center text-sm text-muted-foreground">
            <p className="text-4xl mb-3">🔌</p>
            <p className="font-medium">Chưa có đối tác nào</p>
            <p className="text-xs mt-1">Nhấn "Tạo partner mới" để bắt đầu</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead className="border-b bg-muted/40">
                <tr>
                  <th className="px-4 py-3 text-left font-medium">Tên</th>
                  <th className="px-4 py-3 text-left font-medium">API Key</th>
                  <th className="px-4 py-3 text-left font-medium">Scopes</th>
                  <th className="px-4 py-3 text-left font-medium">Rate limit</th>
                  <th className="px-4 py-3 text-left font-medium">Hạn mức ngày</th>
                  <th className="px-4 py-3 text-left font-medium">Trạng thái</th>
                  <th className="px-4 py-3 text-left font-medium">Hết hạn</th>
                  <th className="px-4 py-3 text-right font-medium">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map((partner) => {
                  const statusInfo = STATUS_BADGE[partner.status];
                  return (
                    <tr key={partner.id} className="border-b last:border-0 hover:bg-muted/20">
                      <td className="px-4 py-3">
                        <div>
                          <p className="font-medium">{partner.name}</p>
                          {partner.contact_email && (
                            <p className="text-xs text-muted-foreground">{partner.contact_email}</p>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <code className="text-xs bg-muted px-1.5 py-0.5 rounded">
                          {partner.api_key_masked}
                        </code>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1 max-w-[200px]">
                          {partner.scopes.slice(0, 3).map((s) => (
                            <Badge key={s} variant="outline" className="text-xs font-mono">
                              {s.replace("public.", "")}
                            </Badge>
                          ))}
                          {partner.scopes.length > 3 && (
                            <Badge variant="outline" className="text-xs">
                              +{partner.scopes.length - 3}
                            </Badge>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">
                        {partner.rate_limit_per_min}/min
                      </td>
                      <td className="px-4 py-3 text-muted-foreground">
                        {(partner.daily_used ?? 0).toLocaleString("vi-VN")}/
                        {partner.daily_quota.toLocaleString("vi-VN")}
                      </td>
                      <td className="px-4 py-3">
                        <Badge variant={statusInfo.variant}>{statusInfo.label}</Badge>
                      </td>
                      <td className="px-4 py-3 text-muted-foreground text-xs">
                        {partner.expires_at
                          ? format(parseISO(partner.expires_at), "dd/MM/yyyy")
                          : "Không giới hạn"}
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center justify-end gap-1">
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Xem usage & logs"
                            onClick={() => setDrawerPartner(partner)}
                          >
                            <BarChart2 className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Test call"
                            onClick={() => testMutation.mutate(partner.id)}
                          >
                            <TestTube className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Sửa"
                            onClick={() => setEditPartner(partner)}
                          >
                            <Edit className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Tạo lại API key"
                            onClick={() => setRegenerateTarget(partner)}
                          >
                            <RefreshCw className="h-4 w-4" />
                          </Button>
                          <Button
                            variant="ghost"
                            size="icon"
                            title="Xoá"
                            className="text-destructive hover:text-destructive"
                            onClick={() => setDeleteTarget(partner)}
                          >
                            <Trash2 className="h-4 w-4" />
                          </Button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Create Dialog */}
      <Dialog open={showCreate} onOpenChange={setShowCreate}>
        <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Tạo đối tác mới</DialogTitle>
          </DialogHeader>
          <ApiPartnerForm
            onSubmit={handleCreate}
            isLoading={createMutation.isPending}
            submitLabel="Tạo đối tác"
          />
        </DialogContent>
      </Dialog>

      {/* Edit Dialog */}
      <Dialog open={!!editPartner} onOpenChange={(open) => !open && setEditPartner(null)}>
        <DialogContent className="max-w-xl max-h-[90vh] overflow-y-auto">
          <DialogHeader>
            <DialogTitle>Sửa đối tác: {editPartner?.name}</DialogTitle>
          </DialogHeader>
          {editPartner && (
            <ApiPartnerForm
              defaultValues={editPartner}
              onSubmit={handleUpdate}
              isLoading={updateMutation.isPending}
              submitLabel="Cập nhật"
            />
          )}
        </DialogContent>
      </Dialog>

      {/* New API key dialog (shown after create or regenerate) */}
      <Dialog open={!!newApiKey} onOpenChange={(open) => !open && setNewApiKey(null)}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>API Key mới</DialogTitle>
          </DialogHeader>
          {newApiKey && (
            <div className="space-y-4">
              <ApiPartnerKeyDisplay apiKey={newApiKey} />
              <Button
                variant="outline"
                className="w-full"
                onClick={() => setNewApiKey(null)}
              >
                Đã copy, đóng cửa sổ
              </Button>
            </div>
          )}
        </DialogContent>
      </Dialog>

      {/* Delete confirm */}
      <AlertDialog open={!!deleteTarget} onOpenChange={(open) => !open && setDeleteTarget(null)}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Xoá đối tác "{deleteTarget?.name}"?</AlertDialogTitle>
            <AlertDialogDescription>
              Hành động này không thể khôi phục. API key của đối tác sẽ bị vô hiệu ngay lập tức.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Huỷ</AlertDialogCancel>
            <AlertDialogAction
              onClick={handleDelete}
              className="bg-destructive text-destructive-foreground hover:bg-destructive/90"
            >
              Xoá
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Regenerate key confirm */}
      <AlertDialog
        open={!!regenerateTarget}
        onOpenChange={(open) => !open && setRegenerateTarget(null)}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Tạo lại API key cho "{regenerateTarget?.name}"?</AlertDialogTitle>
            <AlertDialogDescription>
              API key cũ sẽ bị vô hiệu ngay lập tức. Đối tác sẽ cần cập nhật key mới trong hệ
              thống của họ.
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Huỷ</AlertDialogCancel>
            <AlertDialogAction onClick={handleRegenerate} disabled={regenMutation.isPending}>
              {regenMutation.isPending ? "Đang tạo..." : "Tạo lại key"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      {/* Usage & Logs Drawer */}
      <Drawer open={!!drawerPartner} onOpenChange={(open) => !open && setDrawerPartner(null)}>
        <DrawerContent className="max-h-[85vh]">
          <DrawerHeader>
            <DrawerTitle>Chi tiết: {drawerPartner?.name}</DrawerTitle>
          </DrawerHeader>
          {drawerPartner && (
            <div className="overflow-y-auto p-4">
              <Tabs defaultValue="usage">
                <TabsList className="mb-4">
                  <TabsTrigger value="usage">
                    <BarChart2 className="mr-1.5 h-3.5 w-3.5" />
                    Usage 7 ngày
                  </TabsTrigger>
                  <TabsTrigger value="logs">
                    <FileText className="mr-1.5 h-3.5 w-3.5" />
                    Request Logs
                  </TabsTrigger>
                </TabsList>
                <TabsContent value="usage">
                  <ApiPartnerUsageChart partnerId={drawerPartner.id} />
                </TabsContent>
                <TabsContent value="logs">
                  <ApiPartnerRequestLogsTable partnerId={drawerPartner.id} />
                </TabsContent>
              </Tabs>
            </div>
          )}
        </DrawerContent>
      </Drawer>
    </div>
  );
}
