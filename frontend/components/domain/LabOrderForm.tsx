"use client";

import { useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useClsCatalog, useCreateLabOrder } from "@/lib/hooks/use-cls-orders";
import type { LabOrderRequest, ClsCatalogItem } from "@/lib/api/types";
import { Plus, X, Search } from "lucide-react";
import { Skeleton } from "@/components/ui/skeleton";

interface Props {
  encounterId: string;
}

const schema = z.object({
  note: z.string().optional(),
  priority: z.enum(["NORMAL", "URGENT", "STAT"]).optional(),
});

type FormValues = z.infer<typeof schema>;

export function LabOrderForm({ encounterId }: Props) {
  const [searchQ, setSearchQ] = useState("");
  const [selected, setSelected] = useState<ClsCatalogItem[]>([]);
  const createOrder = useCreateLabOrder(encounterId);
  const { data: catalog, isLoading: catalogLoading } = useClsCatalog({ q: searchQ, kind: "LAB" });

  const { register, handleSubmit, control, reset } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { priority: "NORMAL" },
  });

  function addItem(item: ClsCatalogItem) {
    if (!selected.some((s) => s.code === item.code)) {
      setSelected((prev) => [...prev, item]);
    }
    setSearchQ("");
  }

  function removeItem(code: string) {
    setSelected((prev) => prev.filter((s) => s.code !== code));
  }

  async function onSubmit(data: FormValues) {
    if (selected.length === 0) return;
    const tests: LabOrderRequest[] = selected.map((s) => ({
      test_code: s.code,
      sample_type: s.sample_type ?? undefined,
      priority: data.priority as "NORMAL" | "URGENT" | "STAT" | undefined,
      note: data.note,
    }));
    await createOrder.mutateAsync(tests);
    setSelected([]);
    reset();
  }

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      {/* Tìm kiếm */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Tìm xét nghiệm (VD: HbA1c, Creatinine...)"
          value={searchQ}
          onChange={(e) => setSearchQ(e.target.value)}
        />
        {searchQ.length >= 1 && (
          <div className="absolute z-10 left-0 right-0 top-full mt-1 rounded-md border bg-card shadow-lg max-h-48 overflow-y-auto">
            {catalogLoading ? (
              <div className="p-2 space-y-1">
                {[1, 2].map((i) => <Skeleton key={i} className="h-8 w-full" />)}
              </div>
            ) : !catalog || catalog.length === 0 ? (
              <p className="p-3 text-sm text-muted-foreground">Không tìm thấy</p>
            ) : (
              catalog.map((item) => (
                <button
                  key={item.code}
                  type="button"
                  onClick={() => addItem(item)}
                  className="w-full text-left px-3 py-2 hover:bg-accent text-sm flex items-center justify-between"
                >
                  <span>
                    <span className="font-mono text-primary mr-2">{item.code}</span>
                    {item.name}
                  </span>
                  <Plus className="h-4 w-4 text-muted-foreground" />
                </button>
              ))
            )}
          </div>
        )}
      </div>

      {/* Selected items */}
      {selected.length > 0 && (
        <div className="flex flex-wrap gap-2">
          {selected.map((item) => (
            <Badge key={item.code} variant="secondary" className="gap-1 text-sm pr-1">
              {item.code} — {item.name}
              <button
                type="button"
                onClick={() => removeItem(item.code)}
                className="ml-1 rounded hover:text-destructive"
                aria-label={`Xóa ${item.name}`}
              >
                <X className="h-3 w-3" />
              </button>
            </Badge>
          ))}
        </div>
      )}

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Độ ưu tiên</Label>
          <Controller
            name="priority"
            control={control}
            render={({ field }) => (
              <Select onValueChange={(v) => field.onChange(v ?? "NORMAL")} value={field.value ?? "NORMAL"}>
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="NORMAL">Thường</SelectItem>
                  <SelectItem value="URGENT">Khẩn</SelectItem>
                  <SelectItem value="STAT">Cấp cứu</SelectItem>
                </SelectContent>
              </Select>
            )}
          />
        </div>
        <div className="space-y-1">
          <Label>Ghi chú</Label>
          <Input placeholder="Ghi chú thêm" {...register("note")} />
        </div>
      </div>

      <Button
        type="submit"
        disabled={selected.length === 0 || createOrder.isPending}
        className="min-h-[44px]"
      >
        {createOrder.isPending ? "Đang lưu..." : `Lưu ${selected.length > 0 ? `(${selected.length})` : ""} chỉ định XN`}
      </Button>
    </form>
  );
}
