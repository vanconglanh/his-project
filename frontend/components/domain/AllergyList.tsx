"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Trash2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { useAllergies, useAddAllergy, useDeleteAllergy } from "@/lib/hooks/use-patients";
import type { AllergySeverity } from "@/lib/api/types";
import { cn } from "@/lib/utils";

const severityConfig: Record<AllergySeverity, { label: string; className: string }> = {
  MILD: { label: "Nhẹ", className: "bg-green-100 text-green-800" },
  MODERATE: { label: "Trung bình", className: "bg-yellow-100 text-yellow-800" },
  SEVERE: { label: "Nặng", className: "bg-orange-100 text-orange-800" },
  LIFE_THREATENING: { label: "Nguy hiểm tính mạng", className: "bg-red-100 text-red-800" },
};

const allergySchema = z.object({
  allergen: z.string().min(1, "Tên chất gây dị ứng không được để trống"),
  reaction: z.string().optional(),
  severity: z.enum(["MILD", "MODERATE", "SEVERE", "LIFE_THREATENING"]),
  onset_date: z.string().optional(),
  note: z.string().optional(),
});

type AllergyFormValues = z.infer<typeof allergySchema>;

interface AllergyListProps {
  patientId: string;
}

export function AllergyList({ patientId }: AllergyListProps) {
  const [showForm, setShowForm] = useState(false);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: allergies, isLoading } = useAllergies(patientId);
  const addMutation = useAddAllergy(patientId);
  const deleteMutation = useDeleteAllergy(patientId);

  const { register, handleSubmit, setValue, watch, reset, formState: { errors } } = useForm<AllergyFormValues>({
    resolver: zodResolver(allergySchema),
    defaultValues: { severity: "MILD" },
  });

  const onSubmit = async (values: AllergyFormValues) => {
    await addMutation.mutateAsync(values);
    reset();
    setShowForm(false);
  };

  if (isLoading) {
    return (
      <div className="space-y-2">
        {[1, 2].map((i) => <Skeleton key={i} className="h-12 w-full" />)}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Danh sách dị ứng</h3>
        <Button
          size="sm"
          variant="outline"
          onClick={() => setShowForm(!showForm)}
          className="gap-1 h-8"
        >
          <Plus className="h-3 w-3" />
          Thêm dị ứng
        </Button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="border rounded-lg p-4 space-y-3 bg-muted/20">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="allergen">Chất gây dị ứng *</Label>
              <Input id="allergen" {...register("allergen")} placeholder="Penicillin, Hải sản..." />
              {errors.allergen && <p className="text-xs text-destructive">{errors.allergen.message}</p>}
            </div>
            <div className="space-y-1">
              <Label htmlFor="severity">Mức độ *</Label>
              <Select
                value={watch("severity")}
                onValueChange={(v) => setValue("severity", v as AllergySeverity)}
              >
                <SelectTrigger id="severity">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(severityConfig).map(([k, v]) => (
                    <SelectItem key={k} value={k}>{v.label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="col-span-2 space-y-1">
              <Label htmlFor="reaction">Phản ứng</Label>
              <Input id="reaction" {...register("reaction")} placeholder="Nổi mề đay, khó thở..." />
            </div>
            <div className="space-y-1">
              <Label htmlFor="onset_date">Ngày khởi phát</Label>
              <Input id="onset_date" type="date" {...register("onset_date")} />
            </div>
            <div className="space-y-1">
              <Label htmlFor="allergy_note">Ghi chú</Label>
              <Input id="allergy_note" {...register("note")} placeholder="Đã từng nhập viện..." />
            </div>
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={addMutation.isPending}>
              {addMutation.isPending ? "Đang lưu..." : "Lưu"}
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={() => { reset(); setShowForm(false); }}>
              Huỷ
            </Button>
          </div>
        </form>
      )}

      {!allergies || allergies.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground text-sm">
          Không có thông tin dị ứng
        </div>
      ) : (
        <div className="space-y-2">
          {allergies.map((a) => (
            <div key={a.id} className="flex items-start justify-between p-3 border rounded-lg">
              <div className="space-y-1">
                <div className="flex items-center gap-2">
                  <span className="font-medium text-sm">{a.allergen}</span>
                  <span
                    className={cn(
                      "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
                      severityConfig[a.severity]?.className
                    )}
                  >
                    {severityConfig[a.severity]?.label}
                  </span>
                </div>
                {a.reaction && <p className="text-xs text-muted-foreground">{a.reaction}</p>}
                {a.note && <p className="text-xs text-muted-foreground italic">{a.note}</p>}
              </div>
              <Button
                variant="ghost"
                size="icon"
                className="h-7 w-7 text-muted-foreground hover:text-destructive"
                onClick={() => setDeleteId(a.id)}
                aria-label="Xoá dị ứng"
              >
                <Trash2 className="h-3.5 w-3.5" />
              </Button>
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null); }}
        title="Xoá thông tin dị ứng"
        description="Bạn có chắc muốn xoá thông tin dị ứng này không?"
        onConfirm={() => {
          if (deleteId) deleteMutation.mutate(deleteId);
          setDeleteId(null);
        }}
        isLoading={deleteMutation.isPending}
        variant="destructive"
      />
    </div>
  );
}
