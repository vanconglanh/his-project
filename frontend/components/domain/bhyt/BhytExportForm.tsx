"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useCreateBhytExport } from "@/lib/hooks/use-bhyt-export";
import { toast } from "sonner";

const schema = z.object({
  period_month: z
    .string()
    .regex(/^\d{4}-\d{2}$/, "Nhập đúng định dạng YYYY-MM")
    .min(1, "Bắt buộc"),
  encounter_type: z.string().optional(),
  room: z.string().optional(),
  note: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function BhytExportForm({ open, onOpenChange }: Props) {
  const create = useCreateBhytExport();

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { period_month: "", encounter_type: "", room: "", note: "" },
  });

  function onSubmit(values: FormValues) {
    create.mutate(
      {
        period_month: values.period_month,
        scope_filter: {
          ...(values.encounter_type ? { encounter_type: values.encounter_type } : {}),
          ...(values.room ? { room: values.room } : {}),
        },
        note: values.note,
      },
      {
        onSuccess: () => {
          toast.success("Tạo kỳ export BHYT thành công");
          form.reset();
          onOpenChange(false);
        },
        onError: () => {
          toast.error("Tạo kỳ export thất bại, vui lòng thử lại");
        },
      }
    );
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Tạo kỳ export BHYT mới</DialogTitle>
        </DialogHeader>

        <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="period_month">
              Kỳ (YYYY-MM) <span className="text-destructive">*</span>
            </Label>
            <Input
              id="period_month"
              placeholder="2026-05"
              aria-invalid={!!form.formState.errors.period_month}
              {...form.register("period_month")}
            />
            {form.formState.errors.period_month && (
              <p className="text-xs text-destructive">{form.formState.errors.period_month.message}</p>
            )}
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="encounter_type">Loại khám (tuỳ chọn)</Label>
            <Select
              items={{ OUTPATIENT: "Ngoại trú", INPATIENT: "Nội trú", EMERGENCY: "Cấp cứu" }}
              value={form.watch("encounter_type")}
              onValueChange={(v) => form.setValue("encounter_type", v ?? undefined)}
            >
              <SelectTrigger id="encounter_type">
                <SelectValue placeholder="Tất cả loại khám" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="OUTPATIENT">Ngoại trú</SelectItem>
                <SelectItem value="INPATIENT">Nội trú</SelectItem>
                <SelectItem value="EMERGENCY">Cấp cứu</SelectItem>
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="room">Phòng (tuỳ chọn)</Label>
            <Input
              id="room"
              placeholder="Mã phòng khám"
              {...form.register("room")}
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="note">Ghi chú</Label>
            <Input
              id="note"
              placeholder="Ghi chú kỳ export"
              {...form.register("note")}
            />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)}>
              Huỷ
            </Button>
            <Button type="submit" disabled={create.isPending}>
              {create.isPending ? "Đang tạo..." : "Tạo kỳ"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
