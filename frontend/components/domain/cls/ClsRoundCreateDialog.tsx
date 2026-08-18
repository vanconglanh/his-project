"use client";

import { useMemo, useState } from "react";
import { Loader2, Plus, Search, Trash2 } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Skeleton } from "@/components/ui/skeleton";
import { useClsCatalog } from "@/lib/hooks/use-cls-orders";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { formatVnd } from "@/lib/utils/encounter-format";
import type { ClsCatalogItem } from "@/lib/api/types";
import type { CreateClsRoundRequest } from "@/lib/api/cls-rounds";

export interface ClsRoundCreateDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  isPending?: boolean;
  onSubmit: (body: CreateClsRoundRequest) => void;
}

export function ClsRoundCreateDialog({
  open,
  onOpenChange,
  isPending,
  onSubmit,
}: ClsRoundCreateDialogProps) {
  const [query, setQuery] = useState("");
  const debouncedQuery = useDebounce(query, 300);
  const [cart, setCart] = useState<ClsCatalogItem[]>([]);
  const [note, setNote] = useState("");

  const { data: catalog, isLoading } = useClsCatalog({ q: debouncedQuery, limit: 20 });

  const total = useMemo(
    () => cart.reduce((sum, item) => sum + (item.default_price ?? 0), 0),
    [cart]
  );

  function addItem(item: ClsCatalogItem) {
    setCart((prev) => (prev.some((x) => x.code === item.code) ? prev : [...prev, item]));
  }

  function removeItem(code: string) {
    setCart((prev) => prev.filter((x) => x.code !== code));
  }

  function reset() {
    setQuery("");
    setCart([]);
    setNote("");
  }

  function handleSubmit() {
    const body: CreateClsRoundRequest = {
      note: note.trim() || undefined,
      lab_tests: cart
        .filter((x) => x.kind === "LAB")
        .map((x) => ({
          test_code: x.code,
          test_name: x.name,
          sample_type: x.sample_type ?? undefined,
          priority: "NORMAL",
        })),
      rad_orders: cart
        .filter((x) => x.kind === "RAD")
        .map((x) => ({
          modality: x.modality ?? "XRAY",
          contrast: false,
          procedure_code: x.code,
          procedure_name: x.name,
          priority: "NORMAL",
        })),
    };
    onSubmit(body);
    reset();
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(v) => {
        if (!v) reset();
        onOpenChange(v);
      }}
    >
      <DialogContent className="max-w-4xl">
        <DialogHeader>
          <DialogTitle>Tạo đợt chỉ định cận lâm sàng</DialogTitle>
          <DialogDescription>
            Chọn dịch vụ xét nghiệm / chẩn đoán hình ảnh cho đợt chỉ định này.
          </DialogDescription>
        </DialogHeader>

        <div className="grid gap-4 md:grid-cols-2">
          {/* Trái — tìm dịch vụ */}
          <div className="space-y-2">
            <Label htmlFor="cls-search">Tìm dịch vụ</Label>
            <div className="relative">
              <Search
                className="pointer-events-none absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground"
                aria-hidden="true"
              />
              <Input
                id="cls-search"
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                placeholder="Nhập mã hoặc tên dịch vụ..."
                className="min-h-[44px] pl-9"
              />
            </div>
            <div className="max-h-72 space-y-1 overflow-y-auto rounded-lg border border-border p-1">
              {isLoading ? (
                [1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)
              ) : !catalog || catalog.length === 0 ? (
                <p className="p-3 text-sm text-muted-foreground">
                  {debouncedQuery
                    ? "Không tìm thấy dịch vụ phù hợp."
                    : "Nhập từ khoá để tìm dịch vụ."}
                </p>
              ) : (
                catalog.map((item) => (
                  <button
                    key={`${item.kind}-${item.code}`}
                    type="button"
                    onClick={() => addItem(item)}
                    className="flex min-h-[44px] w-full items-center gap-2 rounded-md px-2 text-left hover:bg-muted focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[color:var(--focus-ring)]"
                  >
                    <Plus className="h-4 w-4 shrink-0" aria-hidden="true" />
                    <span className="font-mono text-xs tabular-nums text-primary">{item.code}</span>
                    <span className="flex-1 truncate text-sm">{item.name}</span>
                    <span className="text-xs text-muted-foreground">
                      {item.kind === "LAB" ? "XN" : "CĐHA"}
                    </span>
                    <span className="font-mono text-xs tabular-nums">
                      {formatVnd(item.default_price)} ₫
                    </span>
                  </button>
                ))
              )}
            </div>
          </div>

          {/* Phải — giỏ dịch vụ */}
          <div className="space-y-2">
            <Label>Dịch vụ đã chọn ({cart.length})</Label>
            <div className="max-h-56 space-y-1 overflow-y-auto rounded-lg border border-border p-1">
              {cart.length === 0 ? (
                <p className="p-3 text-sm text-muted-foreground">Chưa chọn dịch vụ nào.</p>
              ) : (
                cart.map((item) => (
                  <div key={item.code} className="flex items-center gap-2 rounded-md px-2 py-1">
                    <span className="font-mono text-xs tabular-nums text-primary">{item.code}</span>
                    <span className="flex-1 truncate text-sm">{item.name}</span>
                    <span className="font-mono text-xs tabular-nums">
                      {formatVnd(item.default_price)} ₫
                    </span>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-destructive"
                      onClick={() => removeItem(item.code)}
                      aria-label={`Bỏ dịch vụ ${item.name}`}
                    >
                      <Trash2 className="h-4 w-4" aria-hidden="true" />
                    </Button>
                  </div>
                ))
              )}
            </div>
            <div className="flex items-center justify-between text-sm font-semibold">
              <span>Tổng tiền</span>
              <span className="font-mono tabular-nums">
                {formatVnd(total)} <span aria-label="đồng">₫</span>
              </span>
            </div>
            <div className="space-y-1">
              <Label htmlFor="cls-round-note">Ghi chú</Label>
              <Textarea
                id="cls-round-note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                placeholder="Ghi chú cho đợt chỉ định..."
                rows={2}
              />
            </div>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
            Huỷ
          </Button>
          <Button onClick={handleSubmit} disabled={cart.length === 0 || isPending} className="gap-2">
            {isPending && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
            {isPending ? "Đang lưu…" : "Lưu đợt chỉ định"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
