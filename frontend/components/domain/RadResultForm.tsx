"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Input } from "@/components/ui/input";
import type { RadResultCreateRequest, RadResultUpdateRequest } from "@/lib/api/rad-results";
import type { RadResultResponse } from "@/lib/api/rad-results";

const schema = z.object({
  rad_order_id: z.string().uuid("ID không hợp lệ").optional(),
  findings: z.string().min(5, "Mô tả hình ảnh tối thiểu 5 ký tự"),
  impression: z.string().nullable().optional(),
  conclusion: z.string().min(5, "Kết luận tối thiểu 5 ký tự"),
  recommendations: z.string().nullable().optional(),
  performed_at: z.string().min(1, "Vui lòng nhập thời gian"),
  amend_reason: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface RadResultFormProps {
  existing?: RadResultResponse;
  radOrderId?: string;
  onSubmit: (data: RadResultCreateRequest | RadResultUpdateRequest) => Promise<void>;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

export function RadResultForm({ existing, radOrderId, onSubmit, onCancel, isSubmitting }: RadResultFormProps) {
  const isEdit = !!existing;

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      rad_order_id: existing?.rad_order_id ?? radOrderId ?? "",
      findings: existing?.findings ?? "",
      impression: existing?.impression ?? "",
      conclusion: existing?.conclusion ?? "",
      recommendations: existing?.recommendations ?? "",
      performed_at: existing?.performed_at
        ? new Date(existing.performed_at).toISOString().slice(0, 16)
        : new Date().toISOString().slice(0, 16),
      amend_reason: "",
    },
  });

  async function handleSubmit(values: FormValues) {
    if (isEdit) {
      await onSubmit({
        findings: values.findings,
        impression: values.impression,
        conclusion: values.conclusion,
        recommendations: values.recommendations,
        amend_reason: values.amend_reason,
      } satisfies RadResultUpdateRequest);
    } else {
      await onSubmit({
        rad_order_id: values.rad_order_id!,
        findings: values.findings,
        impression: values.impression,
        conclusion: values.conclusion,
        recommendations: values.recommendations,
        performed_at: new Date(values.performed_at).toISOString(),
      } satisfies RadResultCreateRequest);
    }
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
      {!isEdit && (
        <div className="space-y-1.5">
          <Label htmlFor="rr-performed">Thời gian thực hiện *</Label>
          <Input id="rr-performed" type="datetime-local" {...form.register("performed_at")} />
          {form.formState.errors.performed_at && (
            <p className="text-xs text-destructive">{form.formState.errors.performed_at.message}</p>
          )}
        </div>
      )}

      <div className="space-y-1.5">
        <Label htmlFor="rr-findings">Mô tả hình ảnh *</Label>
        <Textarea
          id="rr-findings"
          {...form.register("findings")}
          rows={4}
          placeholder="Mô tả chi tiết hình ảnh quan sát được..."
          aria-invalid={!!form.formState.errors.findings}
        />
        {form.formState.errors.findings && (
          <p className="text-xs text-destructive">{form.formState.errors.findings.message}</p>
        )}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rr-impression">Ấn tượng / Đánh giá</Label>
        <Textarea
          id="rr-impression"
          {...form.register("impression")}
          rows={2}
          placeholder="Nhận xét, ấn tượng ban đầu..."
        />
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rr-conclusion">Kết luận *</Label>
        <Textarea
          id="rr-conclusion"
          {...form.register("conclusion")}
          rows={3}
          placeholder="Kết luận chẩn đoán hình ảnh..."
          aria-invalid={!!form.formState.errors.conclusion}
        />
        {form.formState.errors.conclusion && (
          <p className="text-xs text-destructive">{form.formState.errors.conclusion.message}</p>
        )}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="rr-recommendations">Đề nghị</Label>
        <Textarea
          id="rr-recommendations"
          {...form.register("recommendations")}
          rows={2}
          placeholder="Các đề nghị theo dõi, thêm xét nghiệm..."
        />
      </div>

      {isEdit && existing?.status === "VERIFIED" && (
        <div className="space-y-1.5">
          <Label htmlFor="rr-amend">Lý do sửa (bắt buộc khi sửa kết quả đã ký)</Label>
          <Textarea id="rr-amend" {...form.register("amend_reason")} rows={2} />
        </div>
      )}

      <div className="flex justify-end gap-2 pt-2">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Huỷ
          </Button>
        )}
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang lưu..." : isEdit ? "Cập nhật" : "Lưu kết quả"}
        </Button>
      </div>
    </form>
  );
}
