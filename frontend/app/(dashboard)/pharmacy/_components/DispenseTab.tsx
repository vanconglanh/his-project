"use client";

import { useState } from "react";
import { useDispenseQueue, useDispenseHistory, useRejectDispense } from "@/lib/hooks/use-pharmacy-dispensing";
import { DispenseQueueCard } from "@/components/domain/DispenseQueueCard";
import { DispenseConfirmDialog } from "@/components/domain/DispenseConfirmDialog";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { Search, RefreshCw, Clock, History, Undo2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { DispenseQueueItem } from "@/lib/api/pharmacy-dispensing";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

export function DispenseTab() {
  const [activeTab, setActiveTab] = useState<"queue" | "history" | "return">("queue");
  const [q, setQ] = useState("");
  const [selectedItem, setSelectedItem] = useState<DispenseQueueItem | null>(null);
  const [rejectItem, setRejectItem] = useState<DispenseQueueItem | null>(null);
  const [rejectReason, setRejectReason] = useState("");

  const { data: queueData, isLoading: queueLoading, refetch } = useDispenseQueue({ q: q || undefined });
  const { data: historyData, isLoading: historyLoading } = useDispenseHistory({ page_size: 50 });

  const reject = useRejectDispense();

  const queueItems = queueData?.data ?? [];
  const historyItems = historyData?.data ?? [];

  return (
    <Tabs value={activeTab} onValueChange={(v) => setActiveTab(v as typeof activeTab)}>
      <div className="flex items-center justify-between flex-wrap gap-3">
        <TabsList>
          <TabsTrigger value="queue" className="gap-2">
            <Clock className="h-4 w-4" />
            Hàng chờ
            {queueItems.length > 0 && (
              <Badge variant="destructive" className="h-5 px-1.5 text-xs ml-1">
                {queueItems.length}
              </Badge>
            )}
          </TabsTrigger>
          <TabsTrigger value="history" className="gap-2">
            <History className="h-4 w-4" />
            Lịch sử
          </TabsTrigger>
          <TabsTrigger value="return" className="gap-2">
            <Undo2 className="h-4 w-4" />
            Hoàn trả
          </TabsTrigger>
        </TabsList>

        <div className="flex items-center gap-2">
          {activeTab === "queue" && (
            <>
              <div className="relative max-w-sm">
                <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  value={q}
                  onChange={(e) => setQ(e.target.value)}
                  placeholder="Tìm bệnh nhân..."
                  className="pl-8 h-9"
                />
              </div>
              <Button variant="outline" size="icon" onClick={() => refetch()} aria-label="Làm mới">
                <RefreshCw className="h-4 w-4" />
              </Button>
              <Badge variant="secondary" className="text-xs">Tự động 10 giây</Badge>
            </>
          )}
        </div>
      </div>

      {/* Tab: Hàng chờ */}
      <TabsContent value="queue" className="mt-4">
        {queueLoading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className="h-36 w-full rounded-lg" />
            ))}
          </div>
        ) : queueItems.length === 0 ? (
          <div className="flex flex-col items-center py-16 text-muted-foreground text-sm gap-2">
            <Clock className="h-10 w-10 opacity-30" />
            <p className="font-medium">Không có đơn thuốc chờ phát</p>
            <p className="text-xs">Hàng đợi sẽ tự động cập nhật mỗi 10 giây</p>
          </div>
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {queueItems.map((item) => (
              <DispenseQueueCard
                key={item.prescription_id}
                item={item}
                onDispense={setSelectedItem}
                onReject={setRejectItem}
              />
            ))}
          </div>
        )}
      </TabsContent>

      {/* Tab: Lịch sử */}
      <TabsContent value="history" className="mt-4">
        {historyLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 5 }).map((_, i) => (
              <Skeleton key={i} className="h-14 w-full rounded-md" />
            ))}
          </div>
        ) : historyItems.length === 0 ? (
          <div className="flex flex-col items-center py-16 text-muted-foreground text-sm gap-2">
            <History className="h-10 w-10 opacity-30" />
            <p className="font-medium">Chưa có lịch sử phát thuốc</p>
          </div>
        ) : (
          <div className="rounded-xl border overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b bg-muted/40">
                  <th className="text-left px-4 py-2 font-medium">Bệnh nhân</th>
                  <th className="text-left px-4 py-2 font-medium">Dược sĩ</th>
                  <th className="text-left px-4 py-2 font-medium">Trạng thái</th>
                  <th className="text-left px-4 py-2 font-medium">Thời gian</th>
                  <th className="text-left px-4 py-2 font-medium">Số thuốc</th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {historyItems.map((item) => (
                  <tr key={item.id} className="hover:bg-accent/50">
                    <td className="px-4 py-2 font-medium text-muted-foreground font-mono text-xs">{item.prescription_id}</td>
                    <td className="px-4 py-2 text-muted-foreground">{item.dispensed_by_name ?? "—"}</td>
                    <td className="px-4 py-2">
                      <Badge
                        variant={item.status === "DISPENSED" ? "default" : item.status === "RETURNED" ? "secondary" : "outline"}
                        className="text-xs"
                      >
                        {item.status === "DISPENSED" ? "Đã phát" : item.status === "RETURNED" ? "Đã hoàn trả" : item.status === "REJECTED" ? "Từ chối" : item.status}
                      </Badge>
                    </td>
                    <td className="px-4 py-2 text-muted-foreground">
                      {item.dispensed_at
                        ? format(parseISO(item.dispensed_at), "HH:mm dd/MM/yyyy", { locale: vi })
                        : "—"}
                    </td>
                    <td className="px-4 py-2 text-center">{item.items?.length ?? 0}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </TabsContent>

      {/* Tab: Hoàn trả */}
      <TabsContent value="return" className="mt-4">
        {historyLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className="h-14 w-full rounded-md" />
            ))}
          </div>
        ) : (
          <div className="rounded-xl border overflow-x-auto">
            <div className="px-4 py-3 border-b bg-muted/40 text-sm text-muted-foreground">
              Chọn đơn đã phát từ lịch sử để hoàn trả
            </div>
            {historyItems.filter((i) => i.status === "DISPENSED").length === 0 ? (
              <div className="flex flex-col items-center py-16 text-muted-foreground text-sm gap-2">
                <Undo2 className="h-10 w-10 opacity-30" />
                <p className="font-medium">Không có đơn nào có thể hoàn trả</p>
              </div>
            ) : (
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b">
                    <th className="text-left px-4 py-2 font-medium">Bệnh nhân</th>
                    <th className="text-left px-4 py-2 font-medium">Thời gian phát</th>
                    <th className="text-left px-4 py-2 font-medium">Số thuốc</th>
                    <th className="text-right px-4 py-2 font-medium"></th>
                  </tr>
                </thead>
                <tbody className="divide-y">
                  {historyItems
                    .filter((i) => i.status === "DISPENSED")
                    .map((item) => (
                      <tr key={item.id} className="hover:bg-accent/50">
                        <td className="px-4 py-2 font-mono text-xs text-muted-foreground">{item.prescription_id}</td>
                        <td className="px-4 py-2 text-muted-foreground">
                          {item.dispensed_at
                            ? format(parseISO(item.dispensed_at), "HH:mm dd/MM/yyyy", { locale: vi })
                            : "—"}
                        </td>
                        <td className="px-4 py-2">{item.items?.length ?? 0} thuốc</td>
                        <td className="px-4 py-2 text-right">
                          <Button
                            variant="outline"
                            size="sm"
                            onClick={() => {
                              // TODO: mở ReturnDispenseDialog khi có component
                              alert(`Hoàn trả đơn ${item.id} — chức năng đang phát triển`);
                            }}
                          >
                            <Undo2 className="h-3.5 w-3.5 mr-1" />
                            Hoàn trả
                          </Button>
                        </td>
                      </tr>
                    ))}
                </tbody>
              </table>
            )}
          </div>
        )}
      </TabsContent>

      {/* Dispense dialog */}
      {selectedItem && (
        <DispenseConfirmDialog
          open={!!selectedItem}
          onClose={() => setSelectedItem(null)}
          item={selectedItem}
        />
      )}

      {/* Reject dialog */}
      <ConfirmDialog
        open={!!rejectItem}
        onOpenChange={(o) => { if (!o) setRejectItem(null); }}
        title="Từ chối phát thuốc"
        description={
          <div className="space-y-2">
            <p>Nhập lý do từ chối:</p>
            <Input
              value={rejectReason}
              onChange={(e) => setRejectReason(e.target.value)}
              placeholder="Lý do..."
              autoFocus
            />
          </div>
        }
        confirmLabel="Từ chối"
        variant="destructive"
        isLoading={reject.isPending}
        onConfirm={async () => {
          if (!rejectItem || !rejectReason) return;
          await reject.mutateAsync({ id: rejectItem.prescription_id, reason: rejectReason });
          setRejectItem(null);
          setRejectReason("");
        }}
      />
    </Tabs>
  );
}
