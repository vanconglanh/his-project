"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { useCreateDrug, useUpdateDrug } from "@/lib/hooks/use-drugs";
import type { DrugMasterResponse, DrugForm as DrugFormType } from "@/lib/api/drugs";

const DRUG_FORMS: { value: DrugFormType; label: string }[] = [
  { value: "TABLET", label: "Viên nén" },
  { value: "CAPSULE", label: "Viên nang" },
  { value: "SYRUP", label: "Syrup" },
  { value: "INJ", label: "Tiêm" },
  { value: "CREAM", label: "Kem" },
  { value: "OINTMENT", label: "Mỡ" },
  { value: "DROP", label: "Nhỏ giọt" },
  { value: "INHALER", label: "Hít" },
  { value: "POWDER", label: "Bột" },
  { value: "SUPPOSITORY", label: "Đặt" },
  { value: "OTHER", label: "Khác" },
];

const schema = z.object({
  code: z.string().min(1, "Bắt buộc"),
  name_vi: z.string().min(1, "Bắt buộc"),
  name_en: z.string().optional(),
  generic_name: z.string().optional(),
  atc_code: z.string().optional(),
  strength: z.string().optional(),
  unit: z.string().min(1, "Bắt buộc"),
  form: z.enum(["TABLET", "CAPSULE", "SYRUP", "INJ", "CREAM", "OINTMENT", "DROP", "INHALER", "POWDER", "SUPPOSITORY", "OTHER"]),
  manufacturer: z.string().optional(),
  country: z.string().optional(),
  price: z.coerce.number().optional(),
  requires_prescription: z.boolean().optional(),
  is_psychotropic: z.boolean().optional(),
  is_narcotic: z.boolean().optional(),
  dtqg_drug_code: z.string().optional(),
  status: z.enum(["ACTIVE", "INACTIVE"]).optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  drug?: DrugMasterResponse;
  onSuccess?: () => void;
  onCancel?: () => void;
}

export function DrugForm({ drug, onSuccess, onCancel }: Props) {
  const createDrug = useCreateDrug();
  const updateDrug = useUpdateDrug(drug?.id ?? "");

  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: drug
      ? {
          ...drug,
          price: drug.price ?? undefined,
        }
      : {
          form: "TABLET",
          requires_prescription: true,
          is_psychotropic: false,
          is_narcotic: false,
          status: "ACTIVE",
        },
  });

  const requiresPrescription = watch("requires_prescription");
  const isPsychotropic = watch("is_psychotropic");
  const isNarcotic = watch("is_narcotic");

  async function onSubmit(data: FormData) {
    if (drug) {
      await updateDrug.mutateAsync(data);
    } else {
      await createDrug.mutateAsync(data);
    }
    onSuccess?.();
  }

  const isPending = createDrug.isPending || updateDrug.isPending;

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label htmlFor="code">Mã thuốc <span className="text-destructive">*</span></Label>
          <Input id="code" {...register("code")} aria-invalid={!!errors.code} />
          {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="name_vi">Tên thuốc (Tiếng Việt) <span className="text-destructive">*</span></Label>
          <Input id="name_vi" {...register("name_vi")} aria-invalid={!!errors.name_vi} />
          {errors.name_vi && <p className="text-xs text-destructive">{errors.name_vi.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="name_en">Tên thuốc (Tiếng Anh)</Label>
          <Input id="name_en" {...register("name_en")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="generic_name">Hoạt chất</Label>
          <Input id="generic_name" {...register("generic_name")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="atc_code">Mã ATC</Label>
          <Input id="atc_code" {...register("atc_code")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="strength">Hàm lượng</Label>
          <Input id="strength" placeholder="VD: 500mg" {...register("strength")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="unit">Đơn vị <span className="text-destructive">*</span></Label>
          <Input id="unit" placeholder="viên, ống, ml..." {...register("unit")} aria-invalid={!!errors.unit} />
          {errors.unit && <p className="text-xs text-destructive">{errors.unit.message}</p>}
        </div>
        <div className="space-y-1">
          <Label>Dạng bào chế <span className="text-destructive">*</span></Label>
          <Select
            items={Object.fromEntries(DRUG_FORMS.map((f) => [f.value, f.label]))}
            defaultValue={drug?.form ?? "TABLET"}
            onValueChange={(v) => setValue("form", (v ?? "TABLET") as FormData["form"])}
          >
            <SelectTrigger><SelectValue /></SelectTrigger>
            <SelectContent>
              {DRUG_FORMS.map((f) => (
                <SelectItem key={f.value} value={f.value}>{f.label}</SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
        <div className="space-y-1">
          <Label htmlFor="manufacturer">Nhà sản xuất</Label>
          <Input id="manufacturer" {...register("manufacturer")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="country">Nước sản xuất</Label>
          <Input id="country" {...register("country")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="price">Giá (VND)</Label>
          <Input id="price" type="number" min={0} {...register("price")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="dtqg_drug_code">Mã thuốc ĐTQG</Label>
          <Input id="dtqg_drug_code" {...register("dtqg_drug_code")} />
        </div>
      </div>

      <div className="flex gap-6 flex-wrap">
        <label className="flex items-center gap-2 cursor-pointer">
          <Switch
            checked={!!requiresPrescription}
            onCheckedChange={(v) => setValue("requires_prescription", v)}
            aria-label="Kê đơn bắt buộc"
          />
          <span className="text-sm">Kê đơn bắt buộc</span>
        </label>
        <label className="flex items-center gap-2 cursor-pointer">
          <Switch
            checked={!!isPsychotropic}
            onCheckedChange={(v) => setValue("is_psychotropic", v)}
            aria-label="Hướng thần"
          />
          <span className="text-sm">Hướng thần</span>
        </label>
        <label className="flex items-center gap-2 cursor-pointer">
          <Switch
            checked={!!isNarcotic}
            onCheckedChange={(v) => setValue("is_narcotic", v)}
            aria-label="Gây nghiện"
          />
          <span className="text-sm">Gây nghiện</span>
        </label>
      </div>

      <div className="flex justify-end gap-2">
        {onCancel && <Button type="button" variant="ghost" onClick={onCancel}>Hủy</Button>}
        <Button type="submit" disabled={isPending}>
          {isPending ? "Đang lưu..." : drug ? "Cập nhật thuốc" : "Tạo thuốc"}
        </Button>
      </div>
    </form>
  );
}
