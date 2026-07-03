"use client";

import { useRouter } from "next/navigation";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { usePurchaseOrders } from "@/lib/hooks/use-pharmacy-warehouse";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import { Plus } from "lucide-react";

const PO_STATUS_CFG: Record<string, { label: string; cls: string }> = {
  DRAFT: { label: "Nháp", cls: "bg-gray-100 text-gray-700 border-gray-300" },
  SENT: { label: "Đã gửi", cls: "bg-blue-100 text-blue-800 border-blue-300" },
  PARTIAL: { label: "Nhận một phần", cls: "bg-yellow-100 text-yellow-800 border-yellow-300" },
  RECEIVED: { label: "Đã nhận đủ", cls: "bg-green-100 text-green-800 border-green-300" },
  CANCELLED: { label: "Đã hủy", cls: "bg-red-100 text-red-800 border-red-300" },
};

export function WarehouseTab() {
  const router = useRouter();
  const { data: poData, isLoading } = usePurchaseOrders({ page: 1, page_size: 20 });

  return (
    <div className="space-y-4">
      <Tabs defaultValue="po">
        <div className="flex items-center justify-between">
          <TabsList>
            <TabsTrigger value="po">Đơn đặt hàng (PO)</TabsTrigger>
            <TabsTrigger value="grn">Phiếu nhập (GRN)</TabsTrigger>
          </TabsList>
          <Button size="sm" onClick={() => router.push("/pharmacy/purchase-orders/new")}>
            <Plus className="h-4 w-4 mr-2" />
            Tạo đơn đặt hàng
          </Button>
        </div>

        <TabsContent value="po" className="mt-4">
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 4 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
            </div>
          ) : (
            <div className="rounded-md border overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Số đơn</TableHead>
                    <TableHead>NCC</TableHead>
                    <TableHead>Ngày đặt</TableHead>
                    <TableHead>Dự kiến giao</TableHead>
                    <TableHead>Trạng thái</TableHead>
                    <TableHead className="text-right">Tổng tiền</TableHead>
                    <TableHead />
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {(poData?.data ?? []).map((po) => {
                    const cfg = PO_STATUS_CFG[po.status] ?? { label: po.status, cls: "" };
                    return (
                      <TableRow key={po.id}>
                        <TableCell className="font-mono text-sm">{po.order_no}</TableCell>
                        <TableCell>{po.supplier_name}</TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {po.ordered_at ? format(parseISO(po.ordered_at), "dd/MM/yyyy", { locale: vi }) : "—"}
                        </TableCell>
                        <TableCell className="text-sm text-muted-foreground">
                          {po.expected_delivery
                            ? format(parseISO(po.expected_delivery), "dd/MM/yyyy", { locale: vi })
                            : "—"}
                        </TableCell>
                        <TableCell>
                          <Badge className={cfg.cls} variant="outline">{cfg.label}</Badge>
                        </TableCell>
                        <TableCell className="text-right font-medium">
                          {(po.total_amount ?? 0).toLocaleString("vi-VN")}đ
                        </TableCell>
                        <TableCell>
                          {(po.status === "SENT" || po.status === "PARTIAL") && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() =>
                                router.push(
                                  `/pharmacy/grn/new?poId=${encodeURIComponent(po.id)}&poNo=${encodeURIComponent(po.order_no)}`
                                )
                              }
                            >
                              Nhập kho
                            </Button>
                          )}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                  {poData?.data.length === 0 && (
                    <TableRow>
                      <TableCell colSpan={7} className="text-center py-8 text-muted-foreground text-sm">
                        Chưa có đơn đặt hàng
                      </TableCell>
                    </TableRow>
                  )}
                </TableBody>
              </Table>
            </div>
          )}
        </TabsContent>

        <TabsContent value="grn" className="mt-4">
          <div className="text-sm text-muted-foreground py-4">
            Chọn một đơn đặt hàng (PO) ở tab trên và nhấn "Nhập kho" để tạo phiếu nhập kho (GRN).
          </div>
        </TabsContent>
      </Tabs>
    </div>
  );
}
