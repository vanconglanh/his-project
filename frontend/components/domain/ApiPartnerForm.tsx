"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Checkbox } from "@/components/ui/checkbox";
import type { ApiPartnerResponse, ApiPartnerCreateRequest } from "@/lib/api/api-partners";
import { ALL_SCOPES } from "@/lib/api/api-partners";

const SCOPE_LABELS: Record<string, string> = {
  "public.patient.read": "Đọc bệnh nhân",
  "public.patient.write": "Tạo bệnh nhân",
  "public.appointment.read": "Đọc lịch hẹn",
  "public.appointment.write": "Đặt lịch hẹn",
  "public.catalog.read": "Xem danh mục",
  "public.visit.lookup": "Tra cứu lượt khám",
};

const schema = z.object({
  name: z.string().min(2, "Tên tối thiểu 2 ký tự"),
  contact_email: z.string().email("Email không hợp lệ").optional().or(z.literal("")),
  scopes: z.array(z.string()).min(1, "Chọn ít nhất 1 scope"),
  rate_limit_per_min: z.number().int().min(1).max(10000),
  daily_quota: z.number().int().min(1).max(10_000_000),
  expires_at: z.string().optional().or(z.literal("")),
});

type FormValues = z.infer<typeof schema>;

interface ApiPartnerFormProps {
  defaultValues?: Partial<ApiPartnerResponse>;
  onSubmit: (data: ApiPartnerCreateRequest) => void;
  isLoading?: boolean;
  submitLabel?: string;
}

export function ApiPartnerForm({
  defaultValues,
  onSubmit,
  isLoading,
  submitLabel = "Tạo đối tác",
}: ApiPartnerFormProps) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(schema) as any,
    defaultValues: {
      name: defaultValues?.name ?? "",
      contact_email: defaultValues?.contact_email ?? "",
      scopes: defaultValues?.scopes ?? [],
      rate_limit_per_min: defaultValues?.rate_limit_per_min ?? 60,
      daily_quota: defaultValues?.daily_quota ?? 10000,
      expires_at: defaultValues?.expires_at
        ? new Date(defaultValues.expires_at).toISOString().split("T")[0]
        : "",
    },
  });

  const selectedScopes = watch("scopes");

  function toggleScope(scope: string) {
    const current = selectedScopes ?? [];
    if (current.includes(scope)) {
      setValue(
        "scopes",
        current.filter((s) => s !== scope),
        { shouldValidate: true }
      );
    } else {
      setValue("scopes", [...current, scope], { shouldValidate: true });
    }
  }

  function handleFormSubmit(values: unknown) {
    const v = values as FormValues;
    onSubmit({
      name: v.name,
      contact_email: v.contact_email || undefined,
      scopes: v.scopes,
      rate_limit_per_min: v.rate_limit_per_min,
      daily_quota: v.daily_quota,
      expires_at: v.expires_at || null,
    });
  }

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-5">
      <div className="space-y-1.5">
        <Label htmlFor="name">
          Tên đối tác <span className="text-destructive">*</span>
        </Label>
        <Input id="name" {...register("name")} placeholder="Website phòng khám A" />
        {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="contact_email">Email liên hệ</Label>
        <Input
          id="contact_email"
          type="email"
          {...register("contact_email")}
          placeholder="partner@example.com"
        />
        {errors.contact_email && (
          <p className="text-xs text-destructive">{errors.contact_email.message}</p>
        )}
      </div>

      <div className="space-y-2">
        <Label>
          Scopes (quyền truy cập) <span className="text-destructive">*</span>
        </Label>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {ALL_SCOPES.map((scope) => (
            <label
              key={scope}
              className="flex cursor-pointer items-center gap-2.5 rounded-md border p-2.5 hover:bg-muted/50"
            >
              <Checkbox
                checked={selectedScopes?.includes(scope) ?? false}
                onCheckedChange={() => toggleScope(scope)}
                id={`scope-${scope}`}
              />
              <span className="text-sm">
                <span className="font-medium">{SCOPE_LABELS[scope] ?? scope}</span>
                <br />
                <span className="text-xs text-muted-foreground font-mono">{scope}</span>
              </span>
            </label>
          ))}
        </div>
        {errors.scopes && <p className="text-xs text-destructive">{errors.scopes.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1.5">
          <Label htmlFor="rate_limit_per_min">Rate limit (req/phút)</Label>
          <Input id="rate_limit_per_min" type="number" {...register("rate_limit_per_min", { valueAsNumber: true })} />
          {errors.rate_limit_per_min && (
            <p className="text-xs text-destructive">{errors.rate_limit_per_min.message}</p>
          )}
        </div>
        <div className="space-y-1.5">
          <Label htmlFor="daily_quota">Hạn mức / ngày</Label>
          <Input id="daily_quota" type="number" {...register("daily_quota", { valueAsNumber: true })} />
          {errors.daily_quota && (
            <p className="text-xs text-destructive">{errors.daily_quota.message}</p>
          )}
        </div>
      </div>

      <div className="space-y-1.5">
        <Label htmlFor="expires_at">Ngày hết hạn (để trống = không hết hạn)</Label>
        <Input id="expires_at" type="date" {...register("expires_at")} />
      </div>

      <Button type="submit" disabled={isLoading} className="w-full">
        {isLoading ? "Đang xử lý..." : submitLabel}
      </Button>
    </form>
  );
}
