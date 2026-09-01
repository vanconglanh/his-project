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
import { useCodeItems } from "@/lib/hooks/use-codes";
import {
  GENDER,
  BLOOD_TYPE,
  NATIONALITY,
  MARITAL_STATUS,
  PATIENT_TYPE,
  VISIT_TYPE,
} from "@/lib/constants/code-labels";

interface PatientGeneralTabProps {
  register: UseFormRegister<PatientFormValues>;
  errors: FieldErrors<PatientFormValues>;
  watch: UseFormWatch<PatientFormValues>;
  setValue: UseFormSetValue<PatientFormValues>;
  autoFocus?: boolean;
}

function computeAge(dob: string | undefined): string {
  if (!dob) return "";
  const birth = new Date(dob);
  if (isNaN(birth.getTime())) return "";
  const today = new Date();
  let age = today.getFullYear() - birth.getFullYear();
  const m = today.getMonth() - birth.getMonth();
  if (m < 0 || (m === 0 && today.getDate() < birth.getDate())) age--;
  return age >= 0 ? `${age} tuổi` : "";
}

export function PatientGeneralTab({
  register,
  errors,
  watch,
  setValue,
  autoFocus,
}: PatientGeneralTabProps) {
  const dob = watch("date_of_birth");
  const age = computeAge(dob);

  // Danh muc DB-driven (API /codes/{groupId}), fallback ve hang so code-labels.ts
  // khi dang tai hoac loi (offline/SSR) de UI khong vo.
  const genderItems = useCodeItems("GENDER", GENDER);
  const bloodTypeItems = useCodeItems("BLOOD_TYPE", BLOOD_TYPE);
  const nationalityItems = useCodeItems("NATIONALITY", NATIONALITY);
  const maritalStatusItems = useCodeItems("MARITAL_STATUS", MARITAL_STATUS);
  const patientTypeItems = useCodeItems("PATIENT_TYPE", PATIENT_TYPE);
  const visitTypeItems = useCodeItems("VISIT_TYPE", VISIT_TYPE);

  return (
    <div className="space-y-6">
      {/* Thông tin cơ bản */}
      <div>
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide mb-3">
          Thông tin cá nhân
        </p>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Họ và tên – full width */}
          <div className="col-span-1 md:col-span-2 lg:col-span-4 space-y-1">
            <Label htmlFor="full_name">
              Họ và tên <span className="text-destructive">*</span>
            </Label>
            <Input
              id="full_name"
              {...register("full_name")}
              placeholder="Nguyễn Văn An"
              aria-invalid={!!errors.full_name}
              autoFocus={autoFocus}
            />
            {errors.full_name && (
              <p className="text-xs text-destructive">{errors.full_name.message}</p>
            )}
          </div>

          {/* Giới tính */}
          <div className="space-y-1">
            <Label htmlFor="gender">Giới tính</Label>
            <Select
              items={genderItems}
              value={watch("gender") ?? ""}
              onValueChange={(v) =>
                setValue("gender", v as "MALE" | "FEMALE" | "OTHER", { shouldDirty: true })
              }
            >
              <SelectTrigger id="gender">
                <SelectValue placeholder="Chọn giới tính" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(genderItems).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Ngày sinh + tuổi */}
          <div className="space-y-1">
            <Label htmlFor="date_of_birth">
              Ngày sinh
              {age && (
                <span className="ml-2 text-xs font-normal text-muted-foreground">
                  ({age})
                </span>
              )}
            </Label>
            <Input
              id="date_of_birth"
              type="date"
              {...register("date_of_birth")}
              max={new Date().toISOString().split("T")[0]}
              aria-invalid={!!errors.date_of_birth}
            />
            {errors.date_of_birth && (
              <p className="text-xs text-destructive">{errors.date_of_birth.message}</p>
            )}
          </div>

          {/* Số điện thoại */}
          <div className="space-y-1">
            <Label htmlFor="phone">Số điện thoại</Label>
            <Input
              id="phone"
              {...register("phone")}
              placeholder="0912345678"
              aria-invalid={!!errors.phone}
            />
            {errors.phone && (
              <p className="text-xs text-destructive">{errors.phone.message}</p>
            )}
          </div>

          {/* Email */}
          <div className="space-y-1">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              {...register("email")}
              placeholder="email@example.com"
              aria-invalid={!!errors.email}
            />
            {errors.email && (
              <p className="text-xs text-destructive">{errors.email.message}</p>
            )}
          </div>

          {/* Nhóm máu */}
          <div className="space-y-1">
            <Label htmlFor="blood_type">Nhóm máu</Label>
            <Select
              items={bloodTypeItems}
              value={watch("blood_type") ?? ""}
              onValueChange={(v) =>
                setValue("blood_type", v as PatientFormValues["blood_type"], { shouldDirty: true })
              }
            >
              <SelectTrigger id="blood_type">
                <SelectValue placeholder="Chọn nhóm máu" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(bloodTypeItems).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Nghề nghiệp */}
          <div className="space-y-1">
            <Label htmlFor="occupation">Nghề nghiệp</Label>
            <Input
              id="occupation"
              {...register("occupation")}
              placeholder="Kỹ sư, giáo viên..."
            />
          </div>

          {/* Dân tộc */}
          <div className="space-y-1">
            <Label htmlFor="ethnicity">Dân tộc</Label>
            <Input
              id="ethnicity"
              {...register("ethnicity")}
              placeholder="Kinh, Tày..."
            />
          </div>
        </div>
      </div>

      {/* Giấy tờ tùy thân */}
      <div className="space-y-3 pt-2 border-t">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Giấy tờ tùy thân
        </p>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* CMND/CCCD */}
          <div className="space-y-1">
            <Label htmlFor="id_number">CMND/CCCD</Label>
            <Input
              id="id_number"
              {...register("id_number")}
              placeholder="9 hoặc 12 số"
              aria-invalid={!!errors.id_number}
            />
            {errors.id_number && (
              <p className="text-xs text-destructive">{errors.id_number.message}</p>
            )}
          </div>

          {/* Ngày cấp */}
          <div className="space-y-1">
            <Label htmlFor="id_card_issued_date">Ngày cấp CMND/CCCD</Label>
            <Input
              id="id_card_issued_date"
              type="date"
              {...register("id_card_issued_date")}
              max={new Date().toISOString().split("T")[0]}
              aria-invalid={!!errors.id_card_issued_date}
            />
            {errors.id_card_issued_date && (
              <p className="text-xs text-destructive">{errors.id_card_issued_date.message}</p>
            )}
          </div>

          {/* Nơi cấp */}
          <div className="col-span-1 md:col-span-2 space-y-1">
            <Label htmlFor="id_card_issued_place">Nơi cấp</Label>
            <Input
              id="id_card_issued_place"
              {...register("id_card_issued_place")}
              placeholder="Cục Cảnh sát QLHC về TTXH - Bộ Công An"
              aria-invalid={!!errors.id_card_issued_place}
            />
            {errors.id_card_issued_place && (
              <p className="text-xs text-destructive">{errors.id_card_issued_place.message}</p>
            )}
          </div>
        </div>
      </div>

      {/* Thông tin hành chính */}
      <div className="space-y-3 pt-2 border-t">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Thông tin hành chính
        </p>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {/* Quốc tịch */}
          <div className="space-y-1">
            <Label htmlFor="nationality">Quốc tịch</Label>
            <Select
              items={nationalityItems}
              value={watch("nationality") ?? "VN"}
              onValueChange={(v) => setValue("nationality", v ?? "VN", { shouldDirty: true })}
            >
              <SelectTrigger id="nationality">
                <SelectValue placeholder="Chọn quốc tịch" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(nationalityItems).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Tình trạng hôn nhân */}
          <div className="space-y-1">
            <Label htmlFor="marital_status">Tình trạng hôn nhân</Label>
            <Select
              items={maritalStatusItems}
              value={watch("marital_status") ?? ""}
              onValueChange={(v) =>
                setValue(
                  "marital_status",
                  v as PatientFormValues["marital_status"],
                  { shouldDirty: true }
                )
              }
            >
              <SelectTrigger id="marital_status">
                <SelectValue placeholder="Chọn tình trạng" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(maritalStatusItems).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Đối tượng */}
          <div className="space-y-1">
            <Label htmlFor="patient_type">Đối tượng</Label>
            <Select
              items={patientTypeItems}
              value={watch("patient_type") ?? "SERVICE"}
              onValueChange={(v) =>
                setValue(
                  "patient_type",
                  v as PatientFormValues["patient_type"],
                  { shouldDirty: true }
                )
              }
            >
              <SelectTrigger id="patient_type">
                <SelectValue placeholder="Chọn đối tượng" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(patientTypeItems).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Loại khám */}
          <div className="space-y-1">
            <Label htmlFor="visit_type">Loại khám</Label>
            <Select
              items={visitTypeItems}
              value={watch("visit_type") ?? "FIRST_VISIT"}
              onValueChange={(v) =>
                setValue(
                  "visit_type",
                  v as PatientFormValues["visit_type"],
                  { shouldDirty: true }
                )
              }
            >
              <SelectTrigger id="visit_type">
                <SelectValue placeholder="Chọn loại khám" />
              </SelectTrigger>
              <SelectContent>
                {Object.entries(visitTypeItems).map(([value, label]) => (
                  <SelectItem key={value} value={value}>{label}</SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        </div>
      </div>

      {/* Địa chỉ */}
      <div className="space-y-3 pt-2 border-t">
        <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
          Địa chỉ
        </p>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="space-y-1">
            <Label htmlFor="province_code">Mã tỉnh/thành</Label>
            <Input id="province_code" {...register("province_code")} placeholder="01" />
          </div>
          <div className="space-y-1">
            <Label htmlFor="district_code">Mã quận/huyện</Label>
            <Input id="district_code" {...register("district_code")} placeholder="001" />
          </div>
          <div className="space-y-1">
            <Label htmlFor="ward_code">Mã phường/xã</Label>
            <Input id="ward_code" {...register("ward_code")} placeholder="00001" />
          </div>
          <div className="space-y-1">
            <Label htmlFor="street">Số nhà, đường</Label>
            <Input id="street" {...register("street")} placeholder="12 Ngõ 45 Hàng Bài" />
          </div>
        </div>
      </div>
    </div>
  );
}
