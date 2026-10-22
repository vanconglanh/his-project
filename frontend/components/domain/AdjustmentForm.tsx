"use client";

import { useEffect, useState } from "react";
import { useFieldArray, useForm, useWatch, type Control, type FieldErrors, type UseFormRegister, type UseFormSetValue } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { DrugAutocomplete } from "@/components/domain/DrugAutocomplete";
import type { StockResponse } from "@/lib/api/pharmacy-warehouse";
import { useCreateAdjustment, useStocks, useWarehouses } from "@/lib/hooks/use-pharmacy-warehouse";
import { Plus, Trash2 } from "lucide-react";

const REASONS = [
  { value: "STOCKTAKE", label: "Kiểm kê" },
  { value: "DAMAGED", label: "Hư hỏng" },
  { value: "EXPIRED", label: "Hết hạn" },
  { value: "LOST", label: "Thất thoát" },
  { value: "OTHER", label: "Khác" },
] as const;

const schema = z.object({
  warehouse_id: z.string().min(1, "Chọn kho"),
  reason: z.enum(["STOCKTAKE", "DAMAGED", "EXPIRED", "LOST", "OTHER"]),
  note: z.string().optional(),
  items: z
    .array(
      z.object({
        drug_id: z.string().min(1, "Chọn thuốc"),
        batch_no: z.string().min(1, "Chọn số lô"),
        quantity_diff: z.coerce.number(),
      })
    )
    .min(1, "Cần ít nhất 1 dòng"),
});

type FormData = z.infer<typeof schema>;

interface RowProps {
  idx: number;
  canRemove: boolean;
  warehouseId: string;
  control: Control<FormData>;
  register: UseFormRegister<FormData>;
  setValue: UseFormSetValue<FormData>;
  errors: FieldErrors<FormData>;
  onRemove: () => void;
}

/** Một dòng điều chỉnh: chọn thuốc -> chọn lô (từ tồn kho) -> tồn cuối -> 2 ô số lượng liên động. */
function AdjustmentRow({ idx, canRemove, warehouseId, control, register, setValue, errors, onRemove }: RowProps) {
  const drugId = useWatch({ control, name: `items.${idx}.drug_id` }) ?? "";
  const batchNo = useWatch({ control, name: `items.${idx}.batch_no` }) ?? "";
  const diffRaw = useWatch({ control, name: `items.${idx}.quantity_diff` });
  const [drugLabel, setDrugLabel] = useState("");
  // "Số lượng sau" do người dùng gõ; null = tự tính từ tồn cuối + diff
  const [afterEdit, setAfterEdit] = useState<string | null>(null);

  const { data: stockData, isLoading: loadingStocks } = useStocks(
    warehouseId && drugId ? { warehouse_id: warehouseId, drug_id: drugId, page_size: 200 } : undefined
  );
  const raw = warehouseId && drugId ? ((stockData as unknown as { data?: StockResponse[] })?.data ?? stockData) : [];
  const batches: StockResponse[] = Array.isArray(raw) ? raw : [];
  const current = batches.find((b) => b.batch_no === batchNo)?.quantity_available;
  const rowErr = errors.items?.[idx];

  function setDiff(n: number) {
    setValue(`items.${idx}.quantity_diff`, n, { shouldValidate: true });
  }

  function onAfterChange(v: string) {
    setAfterEdit(v);
    if (current === undefined || v === "") return;
    const a = Number(v);
    if (Number.isFinite(a)) setDiff(a - current);
  }

  const diffNum = Number(diffRaw ?? 0);
  const computedAfter = current !== undefined ? String(current + (Number.isFinite(diffNum) ? diffNum : 0)) : "";
  const afterShown = afterEdit ?? computedAfter;

  return (
    <div className="grid grid-cols-[2fr_1.2fr_1fr_1fr_1fr_auto] gap-2 items-start">
      <div className="space-y-1">
        <Label className="text-xs">Thuốc</Label>
        {drugId ? (
          <div className="flex items-center gap-1">
            <div className="flex-1 truncate rounded-md border px-3 h-10 flex items-center text-sm" title={drugLabel}>
              {drugLabel || drugId}
            </div>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => {
                setValue(`items.${idx}.drug_id`, "");
                setValue(`items.${idx}.batch_no`, "");
                setDiff(0);
                setDrugLabel("");
                setAfterEdit(null);
              }}
            >
              Đổi
            </Button>
          </div>
        ) : (
          <DrugAutocomplete
            placeholder="Tìm theo tên hoặc mã..."
            onSelect={(d) => {
              setValue(`items.${idx}.drug_id`, d.id, { shouldValidate: true });
              setValue(`items.${idx}.batch_no`, "");
              setDiff(0);
              setAfterEdit(null);
              setDrugLabel(`${d.code} — ${d.name_vi}`);
            }}
          />
        )}
        {rowErr?.drug_id && <p className="text-xs text-destructive">{rowErr.drug_id.message}</p>}
      </div>

      <div className="space-y-1">
        <Label className="text-xs">Số lô</Label>
        <Select
          items={Object.fromEntries(batches.map((b) => [b.batch_no, `${b.batch_no} (tồn ${b.quantity_available})`]))}
          value={batchNo || null}
          disabled={!warehouseId || !drugId}
          onValueChange={(v) => {
            setValue(`items.${idx}.batch_no`, String(v ?? ""), { shouldValidate: true });
            setDiff(0);
            setAfterEdit(null);
          }}
        >
          <SelectTrigger>
            <SelectValue
              placeholder={
                !warehouseId ? "Chọn kho trước" : !drugId ? "Chọn thuốc trước" : loadingStocks ? "Đang tải..." : batches.length ? "-- Chọn lô --" : "Không có lô tồn"
              }
            />
          </SelectTrigger>
          <SelectContent>
            {batches.map((b) => (
              <SelectItem key={b.id} value={b.batch_no}>
                {b.batch_no} (tồn {b.quantity_available})
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
        {rowErr?.batch_no && <p className="text-xs text-destructive">{rowErr.batch_no.message}</p>}
      </div>

      <div className="space-y-1">
        <Label className="text-xs">Tồn cuối hiện tại</Label>
        <Input readOnly tabIndex={-1} value={current !== undefined ? String(current) : ""} placeholder="—" className="bg-muted" aria-label="Tồn cuối hiện tại" />
      </div>

      <div className="space-y-1">
        <Label className="text-xs">Số lượng (+/-)</Label>
        <Input
          type="number"
          step="0.5"
          {...register(`items.${idx}.quantity_diff`, { onChange: () => setAfterEdit(null) })}
        />
      </div>

      <div className="space-y-1">
        <Label className="text-xs">Số lượng sau</Label>
        <Input
          type="number"
          step="0.5"
          disabled={current === undefined}
          value={afterShown}
          onChange={(e) => onAfterChange(e.target.value)}
          aria-label="Số lượng sau điều chỉnh"
        />
      </div>

      <div className="space-y-1">
        <Label className="text-xs invisible">Xóa</Label>
        <Button type="button" variant="ghost" size="icon" className="h-10 w-10 text-destructive" onClick={onRemove} disabled={!canRemove} aria-label="Xóa dòng">
          <Trash2 className="h-4 w-4" />
        </Button>
      </div>
    </div>
  );
}

interface Props {
  onSuccess?: () => void;
  /** id gắn lên thẻ <form> — trang cha (Fullpage shell) dùng để trigger submit từ ngoài */
  formId?: string;
  /** Báo trạng thái đang submit (mutation pending) lên trang cha để hiện ở StickyActionBar */
  onSubmittingChange?: (submitting: boolean) => void;
}

export function AdjustmentForm({ onSuccess, formId = "adjustment-form", onSubmittingChange }: Props) {
  const { data: warehouses } = useWarehouses();
  const createAdjustment = useCreateAdjustment();

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
      reason: "STOCKTAKE",
      items: [{ drug_id: "", batch_no: "", quantity_diff: 0 }],
    },
  });

  const warehouseId = watch("warehouse_id");
  const { fields, append, remove } = useFieldArray({ control, name: "items" });

  useEffect(() => {
    onSubmittingChange?.(createAdjustment.isPending);
  }, [createAdjustment.isPending, onSubmittingChange]);

  async function onSubmit(data: FormData) {
    await createAdjustment.mutateAsync(data as any);
    onSuccess?.();
  }

  return (
    <form id={formId} onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label>Kho</Label>
          <Select
            items={Object.fromEntries((warehouses ?? []).map((w) => [w.id, w.name]))}
            onValueChange={(v) => {
              setValue("warehouse_id", String(v ?? ""), { shouldValidate: true });
              // Đổi kho -> lô cũ không còn hợp lệ, reset lô + số lượng
              fields.forEach((_, i) => {
                setValue(`items.${i}.batch_no`, "");
                setValue(`items.${i}.quantity_diff`, 0);
              });
            }}
          >
            <SelectTrigger>
              <SelectValue placeholder="-- Chọn kho --" />
            </SelectTrigger>
            <SelectContent>
              {warehouses?.map((w) => (
                <SelectItem key={w.id} value={w.id}>{w.name}</SelectItem>
              ))}
            </SelectContent>
          </Select>
          {errors.warehouse_id && <p className="text-xs text-destructive">{errors.warehouse_id.message}</p>}
        </div>

        <div className="space-y-1">
          <Label>Lý do điều chỉnh</Label>
          <Select
            items={Object.fromEntries(REASONS.map((r) => [r.value, r.label]))}
            defaultValue="STOCKTAKE"
            onValueChange={(v) => setValue("reason", (v ?? "STOCKTAKE") as FormData["reason"])}
          >
            <SelectTrigger>
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {REASONS.map((r) => (
                <SelectItem key={r.value} value={r.value}>{r.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="space-y-1">
        <Label htmlFor="note">Ghi chú</Label>
        <Input id="note" {...register("note")} placeholder="Ghi chú thêm..." />
      </div>

      <div className="space-y-2">
        <div className="flex items-center justify-between">
          <Label>Danh sách thuốc điều chỉnh</Label>
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => append({ drug_id: "", batch_no: "", quantity_diff: 0 })}
          >
            <Plus className="h-4 w-4 mr-1" />
            Thêm dòng
          </Button>
        </div>

        {fields.map((field, idx) => (
          <AdjustmentRow
            key={field.id}
            idx={idx}
            canRemove={fields.length > 1}
            warehouseId={warehouseId ?? ""}
            control={control}
            register={register}
            setValue={setValue}
            errors={errors}
            onRemove={() => remove(idx)}
          />
        ))}
      </div>
    </form>
  );
}
