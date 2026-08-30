"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import type { BranchRequest, BranchResponse } from "@/lib/api/branches";

const schema = z.object({
  code: z.string().min(1, "Bắt buộc"),
  name: z.string().min(1, "Bắt buộc"),
  address: z.string().optional(),
  phone: z.string().optional(),
  email: z.string().email("Email không hợp lệ").optional().or(z.literal("")),
  timezone: z.string().optional(),
  cskcb_code: z.string().optional(),
  hospital_rank: z.string().optional(),
  kcb_tuyen: z.string().optional(),
  bhyt_contract_code: z.string().optional(),
  bhyt_contract_valid_from: z.string().optional(),
  bhyt_contract_valid_to: z.string().optional(),
  bhyt_enabled: z.boolean().optional(),
  dtqg_enabled: z.boolean().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  /** id gắn vào <form> để FullPageFormShell trigger submit từ ngoài */
  formId?: string;
  branch?: BranchResponse;
  onSubmit: (data: BranchRequest) => void;
  isPending?: boolean;
}

function toDateInputValue(v?: string | null): string {
  if (!v) return "";
  return v.slice(0, 10);
}

export function BranchForm({ formId, branch, onSubmit, isPending }: Props) {
  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: branch
      ? {
          code: branch.code,
          name: branch.name,
          address: branch.address ?? "",
          phone: branch.phone ?? "",
          email: branch.email ?? "",
          timezone: branch.timezone ?? "",
          cskcb_code: branch.cskcb_code ?? "",
          hospital_rank: branch.hospital_rank ?? "",
          kcb_tuyen: branch.kcb_tuyen ?? "",
          bhyt_contract_code: branch.bhyt_contract_code ?? "",
          bhyt_contract_valid_from: toDateInputValue(branch.bhyt_contract_valid_from),
          bhyt_contract_valid_to: toDateInputValue(branch.bhyt_contract_valid_to),
          bhyt_enabled: branch.bhyt_enabled ?? false,
          dtqg_enabled: branch.dtqg_enabled ?? false,
        }
      : {
          code: "",
          name: "",
          address: "",
          phone: "",
          email: "",
          timezone: "",
          cskcb_code: "",
          hospital_rank: "",
          kcb_tuyen: "",
          bhyt_contract_code: "",
          bhyt_contract_valid_from: "",
          bhyt_contract_valid_to: "",
          bhyt_enabled: false,
          dtqg_enabled: false,
        },
  });

  const bhytEnabled = watch("bhyt_enabled");
  const dtqgEnabled = watch("dtqg_enabled");

  return (
    <form
      id={formId}
      onSubmit={handleSubmit((data) =>
        onSubmit({
          ...data,
          email: data.email || undefined,
        })
      )}
      className="space-y-6"
    >
      <div className="grid grid-cols-2 gap-4">
        <div className="space-y-1">
          <Label htmlFor="code">
            Mã chi nhánh <span className="text-destructive">*</span>
          </Label>
          <Input id="code" {...register("code")} aria-invalid={!!errors.code} />
          {errors.code && <p className="text-xs text-destructive">{errors.code.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="name">
            Tên chi nhánh <span className="text-destructive">*</span>
          </Label>
          <Input id="name" {...register("name")} aria-invalid={!!errors.name} />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="phone">Điện thoại</Label>
          <Input id="phone" {...register("phone")} />
        </div>
        <div className="space-y-1">
          <Label htmlFor="email">Email</Label>
          <Input id="email" type="email" {...register("email")} aria-invalid={!!errors.email} />
          {errors.email && <p className="text-xs text-destructive">{errors.email.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="timezone">Múi giờ</Label>
          <Input id="timezone" placeholder="Asia/Ho_Chi_Minh" {...register("timezone")} />
        </div>
        <div className="col-span-2 space-y-1">
          <Label htmlFor="address">Địa chỉ</Label>
          <Input id="address" {...register("address")} />
        </div>
      </div>

      <div className="space-y-4 rounded-lg border bg-muted/30 p-4">
        <h3 className="text-sm font-semibold">Thông tin BHYT / CSKCB</h3>
        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1">
            <Label htmlFor="cskcb_code">Mã CSKCB</Label>
            <Input id="cskcb_code" {...register("cskcb_code")} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="hospital_rank">Hạng bệnh viện</Label>
            <Input id="hospital_rank" {...register("hospital_rank")} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="kcb_tuyen">Tuyến KCB</Label>
            <Input id="kcb_tuyen" {...register("kcb_tuyen")} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="bhyt_contract_code">Số hợp đồng BHYT</Label>
            <Input id="bhyt_contract_code" {...register("bhyt_contract_code")} />
          </div>
          <div className="space-y-1">
            <Label htmlFor="bhyt_contract_valid_from">Hợp đồng hiệu lực từ</Label>
            <Input
              id="bhyt_contract_valid_from"
              type="date"
              {...register("bhyt_contract_valid_from")}
            />
          </div>
          <div className="space-y-1">
            <Label htmlFor="bhyt_contract_valid_to">Hợp đồng hiệu lực đến</Label>
            <Input
              id="bhyt_contract_valid_to"
              type="date"
              {...register("bhyt_contract_valid_to")}
            />
          </div>
        </div>

        <div className="flex gap-6 flex-wrap pt-2">
          <label className="flex items-center gap-2 cursor-pointer">
            <Switch
              checked={!!bhytEnabled}
              onCheckedChange={(v) => setValue("bhyt_enabled", v)}
              aria-label="Khám BHYT"
            />
            <span className="text-sm">Khám BHYT</span>
          </label>
          <label className="flex items-center gap-2 cursor-pointer">
            <Switch
              checked={!!dtqgEnabled}
              onCheckedChange={(v) => setValue("dtqg_enabled", v)}
              aria-label="Kết nối Đơn thuốc Quốc gia"
            />
            <span className="text-sm">Kết nối ĐTQG</span>
          </label>
        </div>
      </div>

      {!formId && (
        <div className="flex justify-end gap-2">
          <Button type="submit" disabled={isPending}>
            {isPending ? "Đang lưu..." : branch ? "Cập nhật" : "Tạo chi nhánh"}
          </Button>
        </div>
      )}
    </form>
  );
}
