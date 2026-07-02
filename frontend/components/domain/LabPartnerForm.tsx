"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
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
import type { LabPartner, LabPartnerCreateRequest, LabPartnerUpdateRequest } from "@/lib/api/lab-partners";

const createSchema = z.object({
  code: z.string().min(1, "Mã không được để trống").max(20),
  name: z.string().min(1, "Tên không được để trống"),
  endpoint_url: z.string().url("URL không hợp lệ"),
  auth_type: z.enum(["NONE", "API_KEY", "BEARER"]),
  api_key: z.string().optional(),
  bearer_token: z.string().nullable().optional(),
  transport: z.enum(["REST", "HL7_MLLP"]),
  contact_email: z.string().email("Email không hợp lệ").nullable().optional(),
  contact_phone: z.string().nullable().optional(),
});

type FormValues = z.infer<typeof createSchema>;

interface LabPartnerFormProps {
  existing?: LabPartner;
  onSubmit: (data: LabPartnerCreateRequest | LabPartnerUpdateRequest) => Promise<void>;
  onCancel?: () => void;
  isSubmitting?: boolean;
}

export function LabPartnerForm({ existing, onSubmit, onCancel, isSubmitting }: LabPartnerFormProps) {
  const isEdit = !!existing;

  const form = useForm<FormValues>({
    resolver: zodResolver(createSchema),
    defaultValues: {
      code: existing?.code ?? "",
      name: existing?.name ?? "",
      endpoint_url: existing?.endpoint_url ?? "",
      auth_type: existing?.auth_type ?? "NONE",
      api_key: "",
      bearer_token: "",
      transport: existing?.transport ?? "REST",
      contact_email: existing?.contact_email ?? "",
      contact_phone: existing?.contact_phone ?? "",
    },
  });

  const authType = form.watch("auth_type");

  async function handleSubmit(values: FormValues) {
    await onSubmit(values);
  }

  return (
    <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        {!isEdit && (
          <div className="space-y-1.5">
            <Label htmlFor="lp-code">Mã đối tác *</Label>
            <Input
              id="lp-code"
              {...form.register("code")}
              placeholder="MEDLATEC, DIAG..."
              aria-invalid={!!form.formState.errors.code}
            />
            {form.formState.errors.code && (
              <p className="text-xs text-destructive">{form.formState.errors.code.message}</p>
            )}
          </div>
        )}

        <div className="space-y-1.5 col-span-2">
          <Label htmlFor="lp-name">Tên đối tác *</Label>
          <Input id="lp-name" {...form.register("name")} placeholder="Medlatec Lab" />
          {form.formState.errors.name && (
            <p className="text-xs text-destructive">{form.formState.errors.name.message}</p>
          )}
        </div>

        <div className="space-y-1.5 col-span-2">
          <Label htmlFor="lp-url">Endpoint URL *</Label>
          <Input id="lp-url" {...form.register("endpoint_url")} placeholder="https://api.partner.vn/v1" />
          {form.formState.errors.endpoint_url && (
            <p className="text-xs text-destructive">{form.formState.errors.endpoint_url.message}</p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lp-transport">Giao thức *</Label>
          <Select
            defaultValue={form.getValues("transport")}
            onValueChange={(v) => form.setValue("transport", v as "REST" | "HL7_MLLP")}
          >
            <SelectTrigger id="lp-transport">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="REST">REST</SelectItem>
              <SelectItem value="HL7_MLLP">HL7 MLLP</SelectItem>
            </SelectContent>
          </Select>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lp-auth">Kiểu xác thực *</Label>
          <Select
            defaultValue={form.getValues("auth_type")}
            onValueChange={(v) => form.setValue("auth_type", v as "NONE" | "API_KEY" | "BEARER")}
          >
            <SelectTrigger id="lp-auth">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="NONE">Không có</SelectItem>
              <SelectItem value="API_KEY">API Key</SelectItem>
              <SelectItem value="BEARER">Bearer Token</SelectItem>
            </SelectContent>
          </Select>
        </div>

        {authType === "API_KEY" && (
          <div className="space-y-1.5 col-span-2">
            <Label htmlFor="lp-apikey">API Key</Label>
            <Input id="lp-apikey" type="password" {...form.register("api_key")} placeholder="sk_..." />
          </div>
        )}

        {authType === "BEARER" && (
          <div className="space-y-1.5 col-span-2">
            <Label htmlFor="lp-bearer">Bearer Token</Label>
            <Input id="lp-bearer" type="password" {...form.register("bearer_token")} />
          </div>
        )}

        <div className="space-y-1.5">
          <Label htmlFor="lp-email">Email liên hệ</Label>
          <Input id="lp-email" type="email" {...form.register("contact_email")} />
          {form.formState.errors.contact_email && (
            <p className="text-xs text-destructive">{form.formState.errors.contact_email.message}</p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="lp-phone">SĐT liên hệ</Label>
          <Input id="lp-phone" {...form.register("contact_phone")} />
        </div>
      </div>

      <div className="flex justify-end gap-2 pt-2">
        {onCancel && (
          <Button type="button" variant="outline" onClick={onCancel} disabled={isSubmitting}>
            Huỷ
          </Button>
        )}
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Đang lưu..." : isEdit ? "Cập nhật" : "Tạo đối tác"}
        </Button>
      </div>
    </form>
  );
}
