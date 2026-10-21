"use client";

import type {
  UseFormRegister,
  FieldErrors,
  UseFormWatch,
  UseFormSetValue,
} from "react-hook-form";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import type { PatientFormValues } from "./patient-schema";
import { RELATIONSHIP } from "@/lib/constants/code-labels";

interface PatientEmergencyTabProps {
  register: UseFormRegister<PatientFormValues>;
  errors: FieldErrors<PatientFormValues>;
  watch: UseFormWatch<PatientFormValues>;
  setValue: UseFormSetValue<PatientFormValues>;
  /** Chế độ sửa: backend chưa trả danh sách người giám hộ -> chỉ hiển thị hướng dẫn. */
  mode?: "create" | "edit";
}

/**
 * BM-89: Mục "Liên hệ khẩn cấp / Người giám hộ" nằm cuối tab "Thông tin chung".
 * Không bắt buộc; nếu nhập một trường thì cần đủ họ tên + số điện thoại (xem patient-schema).
 */
export function PatientEmergencyTab({
  register,
  errors,
  watch,
  setValue,
  mode = "create",
}: PatientEmergencyTabProps) {
  return (
    <div className="space-y-3 pt-2 border-t" id="section-emergency">
      <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
        Liên hệ khẩn cấp / Người giám hộ
        <span className="ml-2 text-xs font-normal normal-case">(không bắt buộc)</span>
      </p>

      {mode === "edit" ? (
        <p className="text-sm text-muted-foreground">
          Thông tin liên hệ khẩn cấp của bệnh nhân đã tạo được quản lý trong hồ sơ bệnh nhân.
        </p>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="space-y-1">
            <Label htmlFor="guardian_full_name">Họ tên người liên hệ</Label>
            <Input
              id="guardian_full_name"
              {...register("guardian_full_name")}
              placeholder="Nguyễn Văn B"
              aria-invalid={!!errors.guardian_full_name}
            />
            {errors.guardian_full_name && (
              <p className="text-xs text-destructive">{errors.guardian_full_name.message}</p>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="guardian_relationship">Quan hệ</Label>
            <Select
              items={RELATIONSHIP}
              value={watch("guardian_relationship") ?? ""}
              onValueChange={(v) =>
                setValue("guardian_relationship", v ?? undefined, { shouldDirty: true })
              }
            >
              <SelectTrigger id="guardian_relationship">
                <SelectValue placeholder="Chọn quan hệ" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(RELATIONSHIP).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          <div className="space-y-1">
            <Label htmlFor="guardian_phone">Số điện thoại</Label>
            <Input
              id="guardian_phone"
              {...register("guardian_phone")}
              placeholder="0912345678"
              aria-invalid={!!errors.guardian_phone}
            />
            {errors.guardian_phone && (
              <p className="text-xs text-destructive">{errors.guardian_phone.message}</p>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="guardian_id_number">CMND/CCCD người giám hộ</Label>
            <Input
              id="guardian_id_number"
              {...register("guardian_id_number")}
              placeholder="9 hoặc 12 số"
              aria-invalid={!!errors.guardian_id_number}
            />
            {errors.guardian_id_number && (
              <p className="text-xs text-destructive">{errors.guardian_id_number.message}</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
