"use client";

import { useEffect, useMemo } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { DrugAutocomplete } from "@/components/domain/DrugAutocomplete";
import { useBranches } from "@/lib/hooks/use-branches";
import { useCreateStockTransfer } from "@/lib/hooks/use-stock-transfers";
import { STOCK_TRANSFER_APPROVAL_THRESHOLD } from "@/lib/api/stock-transfers";
import type { DrugMasterResponse } from "@/lib/api/drugs";
import { AlertTriangle, Plus, Trash2 } from "lucide-react";
import { useBranchStore } from "@/lib/stores/branch-store";

const itemSchema = z.object({
  drug_id: z.string().min(1, "Chọn thuốc/vật tư"),
  drug_name: z.string().optional(),
  lot_no: z.string().optional(),
  expiry_date: z.string().optional(),
  qty_requested: z.coerce.number().positive("Số lượng phải > 0"),
  unit_cost: z.coerce.number().min(0, "Đơn giá không hợp lệ"),
  note: z.string().optional(),
});

const schema = z
  .object({
    from_branch_id: z.string().min(1, "Chọn chi nhánh gửi"),
    to_branch_id: z.string().min(1, "Chọn chi nhánh nhận"),
    reason: z.string().optional(),
    items: z.array(itemSchema).min(1, "Cần ít nhất 1 dòng thuốc"),
  })
  .refine((data) => data.from_branch_id !== data.to_branch_id, {
    message: "Chi nhánh gửi và chi nhánh nhận phải khác nhau",
    path: ["to_branch_id"],
  });

type FormData = z.infer<typeof schema>;

interface Props {
  onSuccess?: (id: string) => void;
  formId?: string;
  onSubmittingChange?: (submitting: boolean) => void;
}

export function StockTransferForm({ onSuccess, formId = "stock-transfer-form", onSubmittingChange }: Props) {
  const { data: branchesData } = useBranches();
  const branches = branchesData?.data ?? [];
  const activeBranchId = useBranchStore((s) => s.activeBranchId);
  const createTransfer = useCreateStockTransfer();

  const {
    register,
    handleSubmit,
    control,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: {
      from_branch_id: activeBranchId ? String(activeBranchId) : "",
      to_branch_id: "",
      reason: "",
      items: [],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "items" });
  const items = watch("items");
  const fromBranchId = watch("from_branch_id");
  const toBranchId = watch("to_branch_id");

  const totalValue = useMemo(
    () =>
      (items ?? []).reduce(
        (sum, it) => sum + (Number(it.qty_requested) || 0) * (Number(it.unit_cost) || 0),
        0
      ),
    [items]
  );

  const requiresRegionalApproval = totalValue > STOCK_TRANSFER_APPROVAL_THRESHOLD;

  useEffect(() => {
    onSubmittingChange?.(createTransfer.isPending);
  }, [createTransfer.isPending, onSubmittingChange]);

  function handleSelectDrug(idx: number, drug: DrugMasterResponse) {
    setValue(`items.${idx}.drug_id`, drug.id);
    setValue(`items.${idx}.drug_name`, drug.name_vi || drug.generic_name || drug.code);
    if (drug.price) setValue(`items.${idx}.unit_cost`, drug.price);
  }

  async function onSubmit(data: FormData) {
    const created = await createTransfer.mutateAsync({
      from_branch_id: Number(data.from_branch_id),
      to_branch_id: Number(data.to_branch_id),
      reason: data.reason || undefined,
      items: data.items.map((it) => ({
        drug_id: it.drug_id,
        lot_no: it.lot_no || null,
        expiry_date: it.expiry_date || null,
        qty_requested: Number(it.qty_requested),
        unit_cost: Number(it.unit_cost),
        note: it.note || null,
      })),
    });
    onSuccess?.(created.id);
  }

  return (
    <form id={formId} onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Chi nhánh gửi</Label>
          <Select
            value={fromBranchId}
            items={Object.fromEntries(branches.map((b) => [String(b.id), b.name]))}
            onValueChange={(v) => setValue("from_branch_id", String(v ?? ""))}
          >
            <SelectTrigger>
              <SelectValue placeholder="-- Chọn chi nhánh gửi --" />
            </SelectTrigger>
            <SelectContent>
              {branches.map((b) => (
                <SelectItem key={b.id} value={String(b.id)}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.from_branch_id && <p className="text-xs text-destructive">{errors.from_branch_id.message}</p>}
        </div>

        <div className="space-y-1">
          <Label>Chi nhánh nhận</Label>
          <Select
            value={toBranchId}
            items={Object.fromEntries(branches.map((b) => [String(b.id), b.name]))}
            onValueChange={(v) => setValue("to_branch_id", String(v ?? ""))}
          >
            <SelectTrigger>
              <SelectValue placeholder="-- Chọn chi nhánh nhận --" />
            </SelectTrigger>
            <SelectContent>
              {branches.map((b) => (
                <SelectItem key={b.id} value={String(b.id)}>{b.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.to_branch_id && <p className="text-xs text-destructive">{errors.to_branch_id.message}</p>}
        </div>
      </div>

      <div className="space-y-1">
        <Label htmlFor="reason">Lý do điều chuyển</Label>
        <Textarea id="reason" {...register("reason")} placeholder="Vd: sắp hết Insulin, xin điều chuyển từ CN Quận 1..." rows={2} />
      </div>

      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <Label>Danh sách thuốc/vật tư điều chuyển</Label>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() =>
              append({ drug_id: "", drug_name: "", lot_no: "", expiry_date: "", qty_requested: 1, unit_cost: 0, note: "" })
            }
          >
            <Plus className="h-4 w-4 mr-1" />
            Thêm dòng
          </Button>
        </div>

        {errors.items?.message && <p className="text-xs text-destructive">{errors.items.message}</p>}

        {fields.length === 0 && (
          <p className="text-sm text-muted-foreground py-4 text-center border border-dashed rounded-md">
            Chưa có dòng thuốc nào — bấm "Thêm dòng" để bắt đầu
          </p>
        )}

        {fields.map((field, idx) => (
          <div key={field.id} className="rounded-md border p-3 space-y-2">
            <div className="flex items-start gap-2">
              <div className="flex-1 space-y-1">
                <Label className="text-xs">Thuốc/vật tư</Label>
                {items?.[idx]?.drug_name ? (
                  <div className="flex items-center justify-between rounded-md border bg-muted/30 px-3 py-2 text-sm">
                    <span className="font-medium">{items[idx].drug_name}</span>
                    <button
                      type="button"
                      className="text-xs text-muted-foreground hover:text-foreground"
                      onClick={() => {
                        setValue(`items.${idx}.drug_id`, "");
                        setValue(`items.${idx}.drug_name`, "");
                      }}
                    >
                      Đổi
                    </button>
                  </div>
                ) : (
                  <DrugAutocomplete onSelect={(d) => handleSelectDrug(idx, d)} />
                )}
                {errors.items?.[idx]?.drug_id && (
                  <p className="text-xs text-destructive">{errors.items[idx]?.drug_id?.message}</p>
                )}
              </div>
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="mt-5 h-10 w-10 text-destructive"
                onClick={() => remove(idx)}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            </div>

            <div className="grid grid-cols-4 gap-2">
              <div className="space-y-1">
                <Label className="text-xs">Số lô</Label>
                <Input placeholder="Số lô" {...register(`items.${idx}.lot_no`)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Hạn sử dụng</Label>
                <Input type="date" {...register(`items.${idx}.expiry_date`)} />
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Số lượng</Label>
                <Input type="number" step="0.5" {...register(`items.${idx}.qty_requested`)} />
                {errors.items?.[idx]?.qty_requested && (
                  <p className="text-xs text-destructive">{errors.items[idx]?.qty_requested?.message}</p>
                )}
              </div>
              <div className="space-y-1">
                <Label className="text-xs">Đơn giá (giá vốn)</Label>
                <Input type="number" step="100" {...register(`items.${idx}.unit_cost`)} />
              </div>
            </div>
          </div>
        ))}
      </div>

      <div className="flex items-center justify-between rounded-md border bg-muted/30 px-4 py-3">
        <span className="text-sm text-muted-foreground">Tổng giá trị phiếu (theo giá vốn)</span>
        <span className="text-lg font-bold">{totalValue.toLocaleString("vi-VN")}đ</span>
      </div>

      {requiresRegionalApproval && (
        <div className="flex items-start gap-2 rounded-md border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <AlertTriangle className="h-4 w-4 mt-0.5 shrink-0" />
          <p>
            Giá trị phiếu vượt ngưỡng {STOCK_TRANSFER_APPROVAL_THRESHOLD.toLocaleString("vi-VN")}đ (BR-58) — phiếu
            này cần <strong>Quản lý vùng/Admin</strong> duyệt thay vì Quản lý chi nhánh gửi.
          </p>
        </div>
      )}
    </form>
  );
}
