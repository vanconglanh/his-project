"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { listPendingLabOrderItems } from "@/lib/api/lab-results";
import type { LabResultCreateRequest, LabResultUpdateRequest } from "@/lib/api/lab-results";
import type { LabResultResponse, PendingLabOrderItem } from "@/lib/api/lab-results";

const schema = z.object({
  lab_order_item_id: z.string().uuid("ID không hợp lệ").optional(),
  value: z.string().min(1, "Giá trị không được để trống"),
  value_numeric: z.number().nullable().optional(),
  unit: z.string().nullable().optional(),
  method: z.string().nullable().optional(),
  performed_at: z.string().min(1, "Vui lòng nhập thời gian thực hiện"),
  note: z.string().nullable().optional(),
  amend_reason: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface LabResultFormProps {
  /** Edit mode: pass existing result */
  existing?: LabResultResponse;
  labOrderItemId?: string;
  onSubmit: (data: LabResultCreateRequest | LabResultUpdateRequest) => Promise<void>;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

function fmtDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString("vi-VN");
  } catch {
    return iso;
  }
}

export function LabResultForm({ existing, labOrderItemId, onSubmit, onCancel, isSubmitting }: LabResultFormProps) {
  const isEdit = !!existing;

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      lab_order_item_id: existing?.lab_order_item_id ?? labOrderItemId ?? "",
      value: existing?.value ?? "",
      value_numeric: existing?.value_numeric ?? null,
      unit: existing?.unit ?? "",
      method: existing?.method ?? "",
      performed_at: existing?.performed_at
        ? new Date(existing.performed_at).toISOString().slice(0, 16)
        : new Date().toISOString().slice(0, 16),
      note: existing?.note ?? "",
      amend_reason: "",
    },
  });

  // ─── Bộ chọn chỉ định XN đang chờ kết quả (chỉ ở chế độ tạo mới) ───
  const [pickerOpen, setPickerOpen] = useState(false);
  const [search, setSearch] = useState("");
  const [items, setItems] = useState<PendingLabOrderItem[]>([]);
  const [loading, setLoading] = useState(false);
  const [selected, setSelected] = useState<PendingLabOrderItem | null>(null);
  const [pickerError, setPickerError] = useState<string | null>(null);
  const boxRef = useRef<HTMLDivElement>(null);

  // register field ẩn để RHF quản lý giá trị
  useEffect(() => {
    if (!isEdit) form.register("lab_order_item_id");
  }, [isEdit, form]);

  // debounce fetch danh sách chờ kết quả theo từ khoá
  useEffect(() => {
    if (isEdit) return;
    let active = true;
    setLoading(true);
    setPickerError(null);
    const t = setTimeout(async () => {
      try {
        const data = await listPendingLabOrderItems({ q: search.trim() || undefined, limit: 50 });
        if (active) setItems(data);
      } catch {
        if (active) setPickerError("Không tải được danh sách chỉ định đang chờ kết quả.");
      } finally {
        if (active) setLoading(false);
      }
    }, 250);
    return () => {
      active = false;
      clearTimeout(t);
    };
  }, [search, isEdit]);

  // đóng dropdown khi click ra ngoài
  useEffect(() => {
    function onDocClick(e: MouseEvent) {
      if (boxRef.current && !boxRef.current.contains(e.target as Node)) setPickerOpen(false);
    }
    document.addEventListener("mousedown", onDocClick);
    return () => document.removeEventListener("mousedown", onDocClick);
  }, []);

  const selectItem = (it: PendingLabOrderItem) => {
    setSelected(it);
    form.setValue("lab_order_item_id", it.lab_order_item_id, { shouldValidate: true });
    if (it.sample_type) form.setValue("method", form.getValues("method") || "");
    setPickerOpen(false);
    setSearch("");
  };

  const orderItemId = form.watch("lab_order_item_id");
  const missingOrder = !isEdit && !orderItemId;

  const selectedLabel = useMemo(() => {
    if (!selected) return "";
    const name = selected.patient_name ?? "(chưa rõ tên)";
    const code = selected.patient_code ? ` · ${selected.patient_code}` : "";
    return `${name}${code} · ${selected.test_name} · ${fmtDate(selected.ordered_at)}`;
  }, [selected]);

  async function handleSubmit(values: FormValues) {
    if (isEdit) {
      await onSubmit({
        value: values.value,
        value_numeric: values.value_numeric,
        unit: values.unit,
        method: values.method,
        note: values.note,
        amend_reason: values.amend_reason,
      } satisfies LabResultUpdateRequest);
    } else {
      await onSubmit({
        lab_order_item_id: values.lab_order_item_id!,
        value: values.value,
        value_numeric: values.value_numeric,
        unit: values.unit,
        method: values.method,
        performed_at: new Date(values.performed_at).toISOString(),
        note: values.note,
      } satisfies LabResultCreateRequest);
    }
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
      {/* Bộ chọn chỉ định XN (tạo mới) */}
      {!isEdit && (
        <div className="space-y-1.5" ref={boxRef}>
          <Label htmlFor="lr-order-search">Chỉ định XN cần nhập kết quả *</Label>

          {selected && (
            <div className="rounded-md border bg-muted/40 px-3 py-2 text-sm">
              <span className="text-muted-foreground">Bệnh nhân: </span>
              <span className="font-medium text-foreground">{selected.patient_name ?? "(chưa rõ tên)"}</span>
              {selected.patient_code ? ` · ${selected.patient_code}` : ""}
              {" · "}
              <span className="font-medium text-foreground">{selected.test_name}</span>
              {" · "}
              {fmtDate(selected.ordered_at)}
            </div>
          )}

          <div className="relative">
            <Input
              id="lr-order-search"
              autoComplete="off"
              placeholder={selected ? "Đổi chỉ định khác..." : "Tìm theo tên bệnh nhân, mã BN hoặc tên XN..."}
              value={search}
              onFocus={() => setPickerOpen(true)}
              onChange={(e) => {
                setSearch(e.target.value);
                setPickerOpen(true);
              }}
              aria-invalid={missingOrder && form.formState.isSubmitted}
            />

            {pickerOpen && (
              <div className="absolute z-50 mt-1 max-h-72 w-full overflow-auto rounded-md border bg-popover shadow-md">
                {loading && <p className="px-3 py-2 text-sm text-muted-foreground">Đang tải...</p>}
                {!loading && pickerError && (
                  <p className="px-3 py-2 text-sm text-destructive">{pickerError}</p>
                )}
                {!loading && !pickerError && items.length === 0 && (
                  <p className="px-3 py-2 text-sm text-muted-foreground">
                    Không có chỉ định XN nào đang chờ kết quả.
                  </p>
                )}
                {!loading &&
                  !pickerError &&
                  items.map((it) => (
                    <button
                      key={it.lab_order_item_id}
                      type="button"
                      onClick={() => selectItem(it)}
                      className="flex w-full flex-col items-start gap-0.5 border-b px-3 py-2 text-left text-sm last:border-b-0 hover:bg-accent hover:text-accent-foreground"
                    >
                      <span className="font-medium">
                        {it.patient_name ?? "(chưa rõ tên)"}
                        {it.patient_code ? ` · ${it.patient_code}` : ""}
                      </span>
                      <span className="text-xs text-muted-foreground">
                        {it.test_name} ({it.test_code}) · Chỉ định {fmtDate(it.ordered_at)}
                      </span>
                    </button>
                  ))}
              </div>
            )}
          </div>

          {missingOrder && form.formState.isSubmitted && (
            <p className="text-xs text-destructive">Vui lòng chọn chỉ định XN cần nhập kết quả.</p>
          )}
        </div>
      )}

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5 col-span-2">
          <Label htmlFor="lr-value">Giá trị kết quả *</Label>
          <Input
            id="lr-value"
            {...form.register("value")}
            placeholder="Vd: 6.2, Âm tính..."
            aria-invalid={!!form.formState.errors.value}
          />
          {form.formState.errors.value && (
            <p className="text-xs text-destructive">{form.formState.errors.value.message}</p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lr-value-num">Giá trị số</Label>
          <Input
            id="lr-value-num"
            type="number"
            step="any"
            placeholder="Vd: 6.2"
            {...form.register("value_numeric", {
              setValueAs: (v) => (v === "" || v === null ? null : Number(v)),
            })}
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lr-unit">Đơn vị</Label>
          <Input id="lr-unit" {...form.register("unit")} placeholder="mmol/L, mg/dL..." />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lr-method">Phương pháp</Label>
          <Input id="lr-method" {...form.register("method")} placeholder="Enzymatic..." />
        </div>

        {!isEdit && (
          <div className="space-y-1.5">
            <Label htmlFor="lr-performed">Thời gian thực hiện *</Label>
            <Input
              id="lr-performed"
              type="datetime-local"
              {...form.register("performed_at")}
              aria-invalid={!!form.formState.errors.performed_at}
            />
            {form.formState.errors.performed_at && (
              <p className="text-xs text-destructive">{form.formState.errors.performed_at.message}</p>
            )}
          </div>
        )}

        <div className="space-y-1.5 col-span-2">
          <Label htmlFor="lr-note">Ghi chú</Label>
          <Textarea id="lr-note" {...form.register("note")} rows={2} placeholder="Ghi chú thêm..." />
        </div>

        {isEdit && existing?.status === "VERIFIED" && (
          <div className="space-y-1.5 col-span-2">
            <Label htmlFor="lr-amend">Lý do sửa (bắt buộc khi sửa kết quả đã xác thực)</Label>
            <Textarea
              id="lr-amend"
              {...form.register("amend_reason")}
              rows={2}
              placeholder="Lý do sửa đổi..."
            />
          </div>
        )}
      </div>

      <div className="flex justify-end gap-2 pt-2">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Huỷ
          </Button>
        )}
        <Button type="submit" disabled={isSubmitting || missingOrder}>
          {isSubmitting ? "Đang lưu..." : isEdit ? "Cập nhật" : "Nhập kết quả"}
        </Button>
      </div>
    </form>
  );
}
