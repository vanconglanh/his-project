"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { useCodeItems } from "@/lib/hooks/use-codes";
import { SERVICE_CATEGORY } from "@/lib/constants/code-labels";
import type {
  ServiceResponse,
  ServiceCategory,
  ServiceUpsertRequest,
} from "@/lib/api/services";

const schema = z.object({
  code: z.string().min(1, "Bắt buộc"),
  name: z.string().min(1, "Bắt buộc"),
  category: z.enum(["CONSULTATION", "PROCEDURE", "LAB", "RAD", "PHARMACY", "OTHER"]),
  price: z.number({ message: "Nhập giá tiền" }).min(0),
  vat_rate: z.number().refine((v) => [0, 5, 8, 10].includes(v), "VAT không hợp lệ"),
  bhyt_code: z.string().optional().nullable(),
  bhyt_max_amount: z.number().optional().nullable(),
  is_active: z.boolean(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  /** id gắn vào <form> để FullPageFormShell trigger submit từ ngoài */
  formId: string;
  editTarget?: ServiceResponse | null;
  onSubmit: (body: ServiceUpsertRequest) => void;
}

export function ServiceForm({ formId, editTarget, onSubmit }: Props) {
  // Danh muc DB-driven, fallback ve hang so SERVICE_CATEGORY neu chua tai duoc.
  const categoryItems = useCodeItems("SERVICE_CATEGORY", SERVICE_CATEGORY);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: editTarget
      ? {
          code: editTarget.code,
          name: editTarget.name,
          category: editTarget.category,
          price: editTarget.price,
          vat_rate: editTarget.vat_rate,
          bhyt_code: editTarget.bhyt_code,
          bhyt_max_amount: editTarget.bhyt_max_amount,
          is_active: editTarget.is_active,
        }
      : { category: "CONSULTATION", vat_rate: 0, is_active: true, price: 0 },
  });

  const isActive = watch("is_active");
  const category = watch("category");
  const vatRate = watch("vat_rate");

  function handleSubmitForm(values: FormData) {
    onSubmit({ ...values, vat_rate: values.vat_rate as 0 | 5 | 8 | 10 });
  }

  return (
    <form id={formId} onSubmit={handleSubmit(handleSubmitForm)} className="space-y-4">
      <div className="grid grid-cols-2 gap-3">
        <div>
          <Label htmlFor="svc_code">Mã dịch vụ *</Label>
          <Input id="svc_code" {...register("code")} className="mt-1" />
          {errors.code && <p className="text-xs text-destructive mt-1">{errors.code.message}</p>}
        </div>
        <div>
          <Label>Nhóm *</Label>
          <Select
            items={categoryItems}
            value={category}
            onValueChange={(v) => setValue("category", v as ServiceCategory)}
          >
            <SelectTrigger className="mt-1">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(categoryItems).map(([value, label]) => (
                <SelectItem key={value} value={value}>{label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div>
        <Label htmlFor="svc_name">Tên dịch vụ *</Label>
        <Input id="svc_name" {...register("name")} className="mt-1" />
        {errors.name && <p className="text-xs text-destructive mt-1">{errors.name.message}</p>}
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <Label htmlFor="svc_price">Giá (VND) *</Label>
          <Input
            id="svc_price"
            type="number"
            min={0}
            step={1000}
            {...register("price", { valueAsNumber: true })}
            className="mt-1"
          />
          {errors.price && <p className="text-xs text-destructive mt-1">{errors.price.message}</p>}
        </div>
        <div>
          <Label>VAT</Label>
          <Select
            items={{ "0": "0%", "5": "5%", "8": "8%", "10": "10%" }}
            value={String(vatRate)}
            onValueChange={(v) => setValue("vat_rate", Number(v) as 0 | 5 | 8 | 10)}
          >
            <SelectTrigger className="mt-1">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {[0, 5, 8, 10].map((v) => (
                <SelectItem key={v} value={String(v)}>{v}%</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-3">
        <div>
          <Label htmlFor="bhyt_code">Mã BHYT</Label>
          <Input id="bhyt_code" {...register("bhyt_code")} className="mt-1" />
        </div>
        <div>
          <Label htmlFor="bhyt_max">Mức tối đa BHYT (VND)</Label>
          <Input
            id="bhyt_max"
            type="number"
            min={0}
            {...register("bhyt_max_amount", { valueAsNumber: true })}
            className="mt-1"
          />
        </div>
      </div>

      <div className="flex items-center gap-3">
        <Switch
          id="is_active"
          checked={isActive}
          onCheckedChange={(v) => setValue("is_active", v)}
        />
        <Label htmlFor="is_active" className="cursor-pointer">
          {isActive ? "Đang hoạt động" : "Tạm ngưng"}
        </Label>
      </div>
    </form>
  );
}
