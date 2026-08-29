"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import type { BranchRequest, BranchResponse } from "@/lib/api/branches";

const schema = z.object({
  code: z.string().min(1, "Bắt buộc"),
  name: z.string().min(1, "Bắt buộc"),
  address: z.string().optional(),
  phone: z.string().optional(),
});

type FormData = z.infer<typeof schema>;

interface Props {
  /** id gắn vào <form> để FullPageFormShell trigger submit từ ngoài */
  formId?: string;
  branch?: BranchResponse;
  onSubmit: (data: BranchRequest) => void;
  isPending?: boolean;
}

export function BranchForm({ formId, branch, onSubmit, isPending }: Props) {
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<FormData>({
    resolver: zodResolver(schema) as any,
    defaultValues: branch
      ? {
          code: branch.code,
          name: branch.name,
          address: branch.address ?? "",
          phone: branch.phone ?? "",
        }
      : { code: "", name: "", address: "", phone: "" },
  });

  return (
    <form id={formId} onSubmit={handleSubmit((data) => onSubmit(data))} className="space-y-4">
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
        <div className="col-span-2 space-y-1">
          <Label htmlFor="address">Địa chỉ</Label>
          <Input id="address" {...register("address")} />
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
