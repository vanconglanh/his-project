"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, Edit2, Trash2 } from "lucide-react";
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
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import {
  useInsurance,
  useAddInsurance,
  useUpdateInsurance,
  useDeleteInsurance,
} from "@/lib/hooks/use-patients";
import type { InsuranceType, InsuranceResponse } from "@/lib/api/types";
import { formatDate } from "@/lib/utils/format";

const BHYT_CARD_REGEX = /^[A-Z0-9]{15}$/;

const insuranceSchema = z.object({
  type: z.enum(["BHYT", "PRIVATE", "OTHER"]),
  card_no: z.string().min(1, "Số thẻ không được để trống").refine(
    (v) => v.length < 15 || BHYT_CARD_REGEX.test(v),
    "Thẻ BHYT phải đúng 15 ký tự chữ cái và số"
  ),
  valid_from: z.string().min(1, "Ngày hiệu lực không được để trống"),
  valid_to: z.string().min(1, "Ngày hết hạn không được để trống"),
  hospital_code: z.string().optional(),
  coverage_percent: z.number().min(0).max(100).optional(),
});

type InsuranceFormValues = z.infer<typeof insuranceSchema>;

const TYPE_LABELS: Record<InsuranceType, string> = {
  BHYT: "Bảo hiểm y tế (BHYT)",
  PRIVATE: "Bảo hiểm tư nhân",
  OTHER: "Khác",
};

interface BhytFormProps {
  patientId: string;
}

export function BhytForm({ patientId }: BhytFormProps) {
  const [showForm, setShowForm] = useState(false);
  const [editItem, setEditItem] = useState<InsuranceResponse | null>(null);
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const { data: insurances, isLoading } = useInsurance(patientId);
  const addMutation = useAddInsurance(patientId);
  const updateMutation = useUpdateInsurance(patientId);
  const deleteMutation = useDeleteInsurance(patientId);

  const { register, handleSubmit, setValue, watch, reset, formState: { errors } } = useForm<InsuranceFormValues>({
    resolver: zodResolver(insuranceSchema),
    defaultValues: { type: "BHYT", coverage_percent: 80 },
  });

  const startEdit = (ins: InsuranceResponse) => {
    setEditItem(ins);
    setShowForm(true);
    setValue("type", ins.type);
    setValue("card_no", ins.card_no);
    setValue("valid_from", ins.valid_from);
    setValue("valid_to", ins.valid_to);
    setValue("hospital_code", ins.hospital_code ?? "");
    setValue("coverage_percent", ins.coverage_percent ?? 80);
  };

  const onSubmit = async (values: InsuranceFormValues) => {
    const body = {
      ...values,
      coverage_percent: values.coverage_percent ?? undefined,
    };
    if (editItem) {
      await updateMutation.mutateAsync({ insuranceId: editItem.id, body });
    } else {
      await addMutation.mutateAsync(body);
    }
    reset({ type: "BHYT", coverage_percent: 80 });
    setShowForm(false);
    setEditItem(null);
  };

  const closeForm = () => {
    reset({ type: "BHYT", coverage_percent: 80 });
    setShowForm(false);
    setEditItem(null);
  };

  if (isLoading) {
    return <div className="space-y-2">{[1].map((i) => <Skeleton key={i} className="h-16 w-full" />)}</div>;
  }

  const isExpired = (validTo: string) => new Date(validTo) < new Date();

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Thẻ bảo hiểm</h3>
        <Button size="sm" variant="outline" onClick={() => setShowForm(!showForm)} className="gap-1 h-8">
          <Plus className="h-3 w-3" />
          Thêm thẻ BHYT
        </Button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="border rounded-lg p-4 space-y-3 bg-muted/20">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Loại *</Label>
              <Select value={watch("type")} onValueChange={(v) => setValue("type", v as InsuranceType)}>
                <SelectTrigger><SelectValue /></SelectTrigger>
                <SelectContent>
                  {Object.entries(TYPE_LABELS).map(([k, label]) => (
                    <SelectItem key={k} value={k}>{label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>Số thẻ *</Label>
              <Input {...register("card_no")} placeholder="HC4010112345678 (15 ký tự)" className="uppercase" />
              {errors.card_no && <p className="text-xs text-destructive">{errors.card_no.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Từ ngày *</Label>
              <Input type="date" {...register("valid_from")} />
              {errors.valid_from && <p className="text-xs text-destructive">{errors.valid_from.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Đến ngày *</Label>
              <Input type="date" {...register("valid_to")} />
              {errors.valid_to && <p className="text-xs text-destructive">{errors.valid_to.message}</p>}
            </div>
            <div className="space-y-1">
              <Label>Mã bệnh viện đăng ký</Label>
              <Input {...register("hospital_code")} placeholder="01001" />
            </div>
            <div className="space-y-1">
              <Label>Mức hưởng (%)</Label>
              <Input type="number" min={0} max={100} {...register("coverage_percent", { valueAsNumber: true })} />
            </div>
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={addMutation.isPending || updateMutation.isPending}>
              {addMutation.isPending || updateMutation.isPending ? "Đang lưu..." : editItem ? "Cập nhật" : "Lưu"}
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={closeForm}>Huỷ</Button>
          </div>
        </form>
      )}

      {!insurances || insurances.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground text-sm">Chưa có thẻ bảo hiểm</div>
      ) : (
        <div className="space-y-2">
          {insurances.map((ins) => (
            <div key={ins.id} className="flex items-center justify-between p-3 border rounded-lg">
              <div>
                <div className="flex items-center gap-2">
                  <span className="font-medium text-sm font-mono">{ins.card_no}</span>
                  <Badge variant={isExpired(ins.valid_to) ? "secondary" : "default"}>
                    {isExpired(ins.valid_to) ? "Hết hạn" : `${ins.coverage_percent ?? ""}%`}
                  </Badge>
                </div>
                <p className="text-xs text-muted-foreground">
                  {TYPE_LABELS[ins.type]} • HSD: {formatDate(ins.valid_to)}
                  {ins.hospital_code ? ` • ${ins.hospital_code}` : ""}
                </p>
              </div>
              <div className="flex gap-1">
                <Button variant="ghost" size="icon" className="h-7 w-7" onClick={() => startEdit(ins)}>
                  <Edit2 className="h-3.5 w-3.5" />
                </Button>
                <Button variant="ghost" size="icon" className="h-7 w-7 hover:text-destructive" onClick={() => setDeleteId(ins.id)}>
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      <ConfirmDialog
        open={!!deleteId}
        onOpenChange={(open) => { if (!open) setDeleteId(null); }}
        title="Xoá thẻ bảo hiểm"
        description="Bạn có chắc muốn xoá thẻ bảo hiểm này không?"
        onConfirm={() => { if (deleteId) deleteMutation.mutate(deleteId); setDeleteId(null); }}
        isLoading={deleteMutation.isPending}
        variant="destructive"
      />
    </div>
  );
}
