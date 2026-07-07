"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Plus, CheckCircle, XCircle } from "lucide-react";
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
import { useConsents, useAddConsent } from "@/lib/hooks/use-patients";
import type { ConsentType } from "@/lib/api/types";
import { formatDateTime } from "@/lib/utils/format";

const CONSENT_LABELS: Record<ConsentType, string> = {
  TREATMENT: "Đồng ý điều trị",
  DATA_PROCESSING: "Xử lý dữ liệu",
  MARKETING: "Tiếp thị",
  SURGERY: "Phẫu thuật",
  RESEARCH: "Nghiên cứu",
};

const consentSchema = z.object({
  consent_type: z.enum(["TREATMENT", "DATA_PROCESSING", "MARKETING", "SURGERY", "RESEARCH"]),
  signed_by: z.string().optional(),
});

type ConsentFormValues = z.infer<typeof consentSchema>;

interface ConsentListProps {
  patientId: string;
}

export function ConsentList({ patientId }: ConsentListProps) {
  const [showForm, setShowForm] = useState(false);
  const { data: consents, isLoading } = useConsents(patientId);
  const addMutation = useAddConsent(patientId);

  const { register, handleSubmit, setValue, watch, reset } = useForm<ConsentFormValues>({
    resolver: zodResolver(consentSchema),
    defaultValues: { consent_type: "TREATMENT" },
  });

  const onSubmit = async (values: ConsentFormValues) => {
    await addMutation.mutateAsync(values);
    reset({ consent_type: "TREATMENT" });
    setShowForm(false);
  };

  if (isLoading) {
    return <div className="space-y-2">{[1, 2].map((i) => <Skeleton key={i} className="h-12 w-full" />)}</div>;
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-medium">Văn bản đồng ý</h3>
        <Button size="sm" variant="outline" onClick={() => setShowForm(!showForm)} className="gap-1 h-8">
          <Plus className="h-3 w-3" />
          Thêm đồng ý
        </Button>
      </div>

      {showForm && (
        <form onSubmit={handleSubmit(onSubmit)} className="border rounded-lg p-4 space-y-3 bg-muted/20">
          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label>Loại đồng ý *</Label>
              <Select
                items={CONSENT_LABELS}
                value={watch("consent_type")}
                onValueChange={(v) => setValue("consent_type", v as ConsentType)}
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {Object.entries(CONSENT_LABELS).map(([k, label]) => (
                    <SelectItem key={k} value={k}>{label}</SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
            <div className="space-y-1">
              <Label>Người ký</Label>
              <Input {...register("signed_by")} placeholder="Tên người ký" />
            </div>
          </div>
          <div className="flex gap-2">
            <Button type="submit" size="sm" disabled={addMutation.isPending}>
              {addMutation.isPending ? "Đang lưu..." : "Lưu"}
            </Button>
            <Button type="button" size="sm" variant="outline" onClick={() => { reset(); setShowForm(false); }}>Huỷ</Button>
          </div>
        </form>
      )}

      {!consents || consents.length === 0 ? (
        <div className="text-center py-8 text-muted-foreground text-sm">Chưa có văn bản đồng ý</div>
      ) : (
        <div className="space-y-2">
          {consents.map((c) => (
            <div key={c.id} className="flex items-center justify-between p-3 border rounded-lg">
              <div className="flex items-center gap-2">
                {c.revoked_at ? (
                  <XCircle className="h-4 w-4 text-muted-foreground" />
                ) : (
                  <CheckCircle className="h-4 w-4 text-green-600" />
                )}
                <div>
                  <p className="text-sm font-medium">{CONSENT_LABELS[c.consent_type]}</p>
                  <p className="text-xs text-muted-foreground">
                    {c.signed_by ? `Ký bởi: ${c.signed_by} • ` : ""}
                    {formatDateTime(c.signed_at)}
                    {c.revoked_at ? ` • Đã thu hồi: ${formatDateTime(c.revoked_at)}` : ""}
                  </p>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
