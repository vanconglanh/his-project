"use client";

import { useState } from "react";
import { useStocks } from "@/lib/hooks/use-pharmacy-warehouse";
import { useWarehouses } from "@/lib/hooks/use-pharmacy-warehouse";
import { StockTable } from "@/components/domain/StockTable";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Search } from "lucide-react";

export function StockTab() {
  const { data: warehouses } = useWarehouses();
  const [warehouseId, setWarehouseId] = useState("");
  const [batchNo, setBatchNo] = useState("");
  const [lowStock, setLowStock] = useState(false);
  const [nearExpiry, setNearExpiry] = useState(false);
  const [page, setPage] = useState(1);

  const { data, isLoading } = useStocks({
    warehouse_id: warehouseId || undefined,
    batch_no: batchNo || undefined,
    low_stock: lowStock || undefined,
    near_expiry: nearExpiry || undefined,
    page,
    page_size: 50,
  });

  return (
    <div className="space-y-4">
      {/* Filters */}
      <div className="flex flex-wrap gap-3 items-end">
        <div className="space-y-1">
          <Label className="text-xs">Kho</Label>
          <Select
            items={{ all: "Tất cả kho", ...Object.fromEntries((warehouses ?? []).map((w) => [w.id, w.name])) }}
            value={warehouseId}
            onValueChange={(v) => { const val = v ?? ""; setWarehouseId(val === "all" ? "" : val); setPage(1); }}
          >
            <SelectTrigger className="w-40">
              <SelectValue placeholder="Tất cả kho" />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="all">Tất cả kho</SelectItem>
              {warehouses?.map((w) => (
                <SelectItem key={w.id} value={w.id}>{w.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>

        <div className="relative flex-1 min-w-[160px]">
          <Label className="text-xs block mb-1">Số lô</Label>
          <div className="relative">
            <Search className="absolute left-2 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
            <Input
              value={batchNo}
              onChange={(e) => { setBatchNo(e.target.value); setPage(1); }}
              placeholder="Số lô..."
              className="pl-8"
            />
          </div>
        </div>

        <label className="flex items-center gap-2 cursor-pointer pt-5">
          <Checkbox
            checked={lowStock}
            onCheckedChange={(c) => { setLowStock(!!c); setPage(1); }}
            id="low-stock"
          />
          <Label htmlFor="low-stock" className="text-sm cursor-pointer">Tồn thấp</Label>
        </label>

        <label className="flex items-center gap-2 cursor-pointer pt-5">
          <Checkbox
            checked={nearExpiry}
            onCheckedChange={(c) => { setNearExpiry(!!c); setPage(1); }}
            id="near-expiry"
          />
          <Label htmlFor="near-expiry" className="text-sm cursor-pointer">Gần hết hạn</Label>
        </label>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {Array.from({ length: 6 }).map((_, i) => <Skeleton key={i} className="h-12 w-full" />)}
        </div>
      ) : (
        <StockTable stocks={data?.data ?? []} />
      )}

      {/* Pagination */}
      {data?.meta && data.meta.total > 50 && (
        <div className="flex items-center justify-between text-sm text-muted-foreground">
          <span>Tổng: {data.meta.total} lô thuốc</span>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Trước</Button>
            <span className="flex items-center px-2">{page} / {Math.ceil(data.meta.total / 50)}</span>
            <Button variant="outline" size="sm" disabled={page >= Math.ceil(data.meta.total / 50)} onClick={() => setPage((p) => p + 1)}>Tiếp</Button>
          </div>
        </div>
      )}
    </div>
  );
}
