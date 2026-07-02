"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
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
import { cn } from "@/lib/utils";
import type { CreatePatientRequest, PatientResponse } from "@/lib/api/types";

const PHONE_VN = /^(\+84|0)\d{9,10}$/;
const ID_NUMBER = /^\d{9}$|^\d{12}$/;

const patientSchema = z.object({
  full_name: z.string().min(2, "Họ tên tối thiểu 2 ký tự").max(200),
  gender: z.enum(["MALE", "FEMALE", "OTHER"]).optional(),
  date_of_birth: z
    .string()
    .optional()
    .refine((v) => !v || new Date(v) < new Date(), {
      message: "Ngày sinh phải trước hôm nay",
    }),
  id_number: z
    .string()
    .optional()
    .refine((v) => !v || ID_NUMBER.test(v), {
      message: "CMND 9 số hoặc CCCD 12 số",
    }),
  phone: z
    .string()
    .optional()
    .refine((v) => !v || PHONE_VN.test(v), {
      message: "Số điện thoại VN không hợp lệ",
    }),
  email: z.string().email("Email không hợp lệ").optional().or(z.literal("")),
  occupation: z.string().optional(),
  ethnicity: z.string().optional(),
  blood_type: z
    .enum(["A_POS", "A_NEG", "B_POS", "B_NEG", "AB_POS", "AB_NEG", "O_POS", "O_NEG", "UNKNOWN"])
    .optional(),
  province_code: z.string().optional(),
  district_code: z.string().optional(),
  ward_code: z.string().optional(),
  street: z.string().optional(),
});

type PatientFormValues = z.infer<typeof patientSchema>;

const TABS = [
  { id: "general", label: "Thông tin chung" },
  { id: "bhyt", label: "BHYT" },
  { id: "emergency", label: "Liên hệ khẩn cấp" },
  { id: "allergy", label: "Dị ứng" },
];

interface PatientFormProps {
  open: boolean;
  onClose: () => void;
  onSubmit: (data: CreatePatientRequest) => void;
  isLoading?: boolean;
  defaultValues?: Partial<PatientResponse>;
  title?: string;
}

export function PatientForm({
  open,
  onClose,
  onSubmit,
  isLoading,
  defaultValues,
  title = "Tạo bệnh nhân mới",
}: PatientFormProps) {
  const [activeTab, setActiveTab] = useState("general");

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
    reset,
  } = useForm<PatientFormValues>({
    resolver: zodResolver(patientSchema),
    defaultValues: {
      full_name: defaultValues?.full_name ?? "",
      gender: defaultValues?.gender,
      date_of_birth: defaultValues?.date_of_birth ?? "",
      phone: defaultValues?.phone ?? "",
      email: defaultValues?.email ?? "",
      occupation: defaultValues?.occupation ?? "",
      ethnicity: defaultValues?.ethnicity ?? "",
      blood_type: defaultValues?.blood_type,
      street: defaultValues?.address?.street ?? "",
    },
  });

  const handleClose = () => {
    reset();
    setActiveTab("general");
    onClose();
  };

  const handleFormSubmit = (values: PatientFormValues) => {
    const { province_code, district_code, ward_code, street, email, ...rest } = values;
    const payload: CreatePatientRequest = {
      ...rest,
      email: email || undefined,
      address:
        province_code || district_code || ward_code || street
          ? { province_code, district_code, ward_code, street }
          : undefined,
    };
    onSubmit(payload);
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="!max-w-6xl sm:!max-w-6xl w-[95vw] h-[90vh] max-h-[90vh] overflow-hidden flex flex-col !p-0 !gap-0">
        <DialogHeader className="px-6 pt-5 pb-3 border-b shrink-0">
          <DialogTitle className="text-xl">{title}</DialogTitle>
        </DialogHeader>

        {/* Tab bar */}
        <div className="flex border-b gap-1 px-6 shrink-0">
          {TABS.map((tab) => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setActiveTab(tab.id)}
              className={cn(
                "px-4 py-2 text-sm font-medium border-b-2 transition-colors",
                activeTab === tab.id
                  ? "border-primary text-primary"
                  : "border-transparent text-muted-foreground hover:text-foreground"
              )}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <form
          id="patient-form"
          onSubmit={handleSubmit(handleFormSubmit)}
          className="flex-1 overflow-y-auto"
        >
          <div className="p-6 space-y-6">
            {activeTab === "general" && (
              <>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  <div className="md:col-span-2 lg:col-span-4 space-y-1">
                    <Label htmlFor="full_name">
                      Họ và tên <span className="text-destructive">*</span>
                    </Label>
                    <Input
                      id="full_name"
                      {...register("full_name")}
                      placeholder="Nguyễn Văn An"
                      aria-invalid={!!errors.full_name}
                    />
                    {errors.full_name && (
                      <p className="text-xs text-destructive">{errors.full_name.message}</p>
                    )}
                  </div>

                  <div className="space-y-1">
                    <Label htmlFor="gender">Giới tính</Label>
                    <Select
                      value={watch("gender") ?? ""}
                      onValueChange={(v) =>
                        setValue("gender", v as "MALE" | "FEMALE" | "OTHER")
                      }
                    >
                      <SelectTrigger id="gender">
                        <SelectValue placeholder="Chọn giới tính" />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="MALE">Nam</SelectItem>
                        <SelectItem value="FEMALE">Nữ</SelectItem>
                        <SelectItem value="OTHER">Khác</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-1">
                    <Label htmlFor="date_of_birth">Ngày sinh</Label>
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

                  <div className="space-y-1">
                    <Label htmlFor="blood_type">Nhóm máu</Label>
                    <Select
                      value={watch("blood_type") ?? ""}
                      onValueChange={(v) => setValue("blood_type", v as PatientFormValues["blood_type"])}
                    >
                      <SelectTrigger id="blood_type">
                        <SelectValue placeholder="Chọn nhóm máu" />
                      </SelectTrigger>
                      <SelectContent>
                        {["A_POS", "A_NEG", "B_POS", "B_NEG", "AB_POS", "AB_NEG", "O_POS", "O_NEG", "UNKNOWN"].map((bt) => (
                          <SelectItem key={bt} value={bt}>
                            {bt.replace("_POS", "+").replace("_NEG", "-").replace("UNKNOWN", "Chưa xác định")}
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>
                  </div>

                  <div className="space-y-1">
                    <Label htmlFor="occupation">Nghề nghiệp</Label>
                    <Input id="occupation" {...register("occupation")} placeholder="Kỹ sư, giáo viên..." />
                  </div>

                  <div className="space-y-1">
                    <Label htmlFor="ethnicity">Dân tộc</Label>
                    <Input id="ethnicity" {...register("ethnicity")} placeholder="Kinh, Tày..." />
                  </div>
                </div>

                <div className="space-y-2 pt-2 border-t">
                  <p className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">Địa chỉ</p>
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
              </>
            )}

            {activeTab === "bhyt" && (
              <div className="py-8 text-center text-muted-foreground text-sm">
                Thông tin BHYT có thể thêm sau khi tạo bệnh nhân.
              </div>
            )}

            {activeTab === "emergency" && (
              <div className="py-8 text-center text-muted-foreground text-sm">
                Liên hệ khẩn cấp có thể thêm sau khi tạo bệnh nhân.
              </div>
            )}

            {activeTab === "allergy" && (
              <div className="py-8 text-center text-muted-foreground text-sm">
                Thông tin dị ứng có thể thêm sau khi tạo bệnh nhân.
              </div>
            )}
          </div>
        </form>

        <DialogFooter className="!m-0 border-t px-6 py-4 shrink-0 bg-muted/30 rounded-b-xl">
          <Button variant="outline" onClick={handleClose} disabled={isLoading} size="lg">
            Huỷ
          </Button>
          <Button type="submit" form="patient-form" disabled={isLoading} size="lg">
            {isLoading ? "Đang lưu..." : "Tạo bệnh nhân"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
