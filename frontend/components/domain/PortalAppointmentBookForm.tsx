"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import type { PortalAppointmentCreateRequest } from "@/lib/api/portal";

const schema = z.object({
  appointment_at: z.string().min(1, "Chọn ngày giờ hẹn"),
  note: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface PortalAppointmentBookFormProps {
  onSubmit: (data: PortalAppointmentCreateRequest) => void;
  isLoading?: boolean;
  onCancel?: () => void;
}

export function PortalAppointmentBookForm({
  onSubmit,
  isLoading,
  onCancel,
}: PortalAppointmentBookFormProps) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
  });

  function handleFormSubmit(values: FormValues) {
    onSubmit({
      appointment_at: new Date(values.appointment_at).toISOString(),
      note: values.note,
    });
  }

  // Min date: tomorrow
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const minDatetime = tomorrow.toISOString().slice(0, 16);

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-4">
      <div className="space-y-1.5">
        <Label htmlFor="appointment_at">
          Ngày giờ hẹn <span className="text-destructive">*</span>
        </Label>
        <Input
          id="appointment_at"
          type="datetime-local"
          min={minDatetime}
          {...register("appointment_at")}
          className="min-h-[44px]"
        />
        {errors.appointment_at && (
          <p className="text-xs text-destructive">{errors.appointment_at.message}</p>
        )}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="note">Ghi chú</Label>
        <Textarea
          id="note"
          {...register("note")}
          placeholder="Lý do hẹn, triệu chứng..."
          rows={3}
        />
      </div>

      <div className="flex gap-2">
        {onCancel && (
          <Button type="button" variant="outline" className="flex-1" onClick={onCancel}>
            Huỷ
          </Button>
        )}
        <Button type="submit" disabled={isLoading} className="flex-1 min-h-[44px]">
          {isLoading ? "Đang đặt lịch..." : "Xác nhận đặt lịch"}
        </Button>
      </div>
    </form>
  );
}
