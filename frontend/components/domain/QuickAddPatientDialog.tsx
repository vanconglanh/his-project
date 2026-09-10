"use client";

// BM-03 (Lark Bug-Feedback): Panel "Tiếp đón bệnh nhân" trước giờ chỉ cho tìm bệnh nhân
// CŨ - đăng ký bệnh nhân mới phải điều hướng sang /patients/new (mất ngữ cảnh, chậm giờ
// cao điểm). PO xác nhân (2026-09-07): BE đã sẵn sàng (POST /patients có đủ validate +
// check trùng), chỉ cần modal "Đăng ký nhanh" ngay tại panel, field theo đúng thứ tự
// ticket: Họ tên/Ngày sinh/SĐT/Giới tính (bắt buộc), Ngày khám mặc định hôm nay (không
// bắt buộc, có thể chọn ngày tương lai), địa chỉ/lý do khám/nguồn/phòng khám (không
// bắt buộc). Quyết định luồng "Ngày khám": hôm nay -> tạo xong vào HÀNG ĐỢI tiếp đón
// ngay (cần chọn phòng khám); ngày tương lai -> tạo LỊCH HẸN (Appointment), KHÔNG vào
// hàng đợi. Sau khi tạo xong luôn nhắc "Hoàn thiện hồ sơ" (CCCD/địa chỉ/BHYT... để dành
// mở rộng sau, phòng khám hiện chưa dùng BHYT).

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Loader2, UserPlus } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { toast } from "sonner";
import { useCreatePatient } from "@/lib/hooks/use-patients";
import { useCheckIn, useRooms } from "@/lib/hooks/use-reception";
import { useCreateAppointment } from "@/lib/hooks/use-appointments";
import { isPossibleDuplicateResult } from "@/lib/api/types";
import type { PatientResponse } from "@/lib/api/types";
import { format } from "date-fns";

const quickAddSchema = z
  .object({
    full_name: z.string().min(1, "Nhập họ và tên"),
    date_of_birth: z.string().min(1, "Chọn ngày sinh"),
    phone: z
      .string()
      .min(9, "Số điện thoại không hợp lệ")
      .regex(/^[0-9+][0-9 ]*$/, "Số điện thoại không hợp lệ"),
    gender: z.enum(["MALE", "FEMALE", "OTHER"], { message: "Chọn giới tính" }),
    visit_date: z.string().min(1),
    room_id: z.string().optional(),
    reason_for_visit: z.string().optional(),
  })
  .superRefine((data, ctx) => {
    const today = format(new Date(), "yyyy-MM-dd");
    if (data.visit_date === today && !data.room_id) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        path: ["room_id"],
        message: "Chọn phòng khám để vào hàng đợi ngay hôm nay",
      });
    }
  });

type QuickAddFormValues = z.infer<typeof quickAddSchema>;

interface QuickAddPatientDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Khi tạo xong VÀ vào hàng đợi ngay (visit_date = hôm nay) - để panel bên ngoài tự chọn bệnh nhân này. */
  onCheckedIn?: (patient: PatientResponse) => void;
}

export function QuickAddPatientDialog({ open, onOpenChange, onCheckedIn }: QuickAddPatientDialogProps) {
  const router = useRouter();
  const today = format(new Date(), "yyyy-MM-dd");
  const { data: rooms } = useRooms();
  const createPatient = useCreatePatient();
  const checkIn = useCheckIn();
  const createAppointment = useCreateAppointment();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<QuickAddFormValues>({
    resolver: zodResolver(quickAddSchema),
    defaultValues: { visit_date: today },
  });

  const isToday = watch("visit_date") === today;

  function close() {
    reset({ visit_date: today });
    onOpenChange(false);
  }

  async function onSubmit(values: QuickAddFormValues) {
    setIsSubmitting(true);
    try {
      const patientBody = {
        full_name: values.full_name.trim(),
        date_of_birth: values.date_of_birth,
        phone: values.phone.trim(),
        gender: values.gender,
      };
      let result = await createPatient.mutateAsync(patientBody);

      // FR-101: nghi trùng - hỏi lại lễ tân truoc khi tạo thật (giống /patients/new)
      if (isPossibleDuplicateResult(result)) {
        const names = result.duplicate_candidates
          .map((c) => `- ${c.full_name} (${c.code}${c.date_of_birth ? `, sinh ${c.date_of_birth}` : ""})`)
          .join("\n");
        const confirmed = window.confirm(
          `Phát hiện hồ sơ có thể trùng:\n${names}\n\nBạn có chắc muốn tạo bệnh nhân mới không?`
        );
        if (!confirmed) {
          setIsSubmitting(false);
          return;
        }
        result = await createPatient.mutateAsync({
          ...patientBody,
          confirm_create_despite_duplicate: true,
        });
        if (isPossibleDuplicateResult(result)) {
          setIsSubmitting(false);
          return; // an toàn, không nên xảy ra
        }
      }

      const patient = result;

      if (values.visit_date === today) {
        // Hôm nay -> tạo xong vào HÀNG ĐỢI tiếp đón ngay (như đăng ký thông thường).
        await checkIn.mutateAsync({
          patient_id: patient.id,
          room_id: values.room_id!,
          reason_for_visit: values.reason_for_visit || undefined,
          priority: "NORMAL",
        });
        onCheckedIn?.(patient);
      } else {
        // Ngày tương lai -> tạo LICH HEN (Appointment), KHONG vao hang doi hom nay.
        await createAppointment.mutateAsync({
          patient_ref: patient.id,
          patient_name_temp: patient.full_name,
          patient_phone: patient.phone ?? values.phone,
          appointment_at: `${values.visit_date}T08:00:00`,
          note: values.reason_for_visit || undefined,
        });
        toast.success(`Đã tạo lịch hẹn ngày ${values.visit_date} cho ${patient.full_name}`);
      }

      // Nhắc hoàn thiện hồ sơ (CCCD, địa chỉ, BHYT... chưa nhập trong đăng ký nhanh)
      toast.info("Hồ sơ mới đang thiếu thông tin — nhớ hoàn thiện hồ sơ bệnh nhân", {
        action: {
          label: "Hoàn thiện ngay",
          onClick: () => router.push(`/patients/${patient.id}`),
        },
        duration: 8000,
      });

      close();
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <Dialog open={open} onOpenChange={(v) => (v ? onOpenChange(v) : close())}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <UserPlus className="h-5 w-5" aria-hidden="true" />
            Đăng ký nhanh bệnh nhân mới
          </DialogTitle>
          <DialogDescription>
            Chỉ cần thông tin tối thiểu — có thể bổ sung đầy đủ hồ sơ sau.
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="space-y-3">
          <div className="space-y-1">
            <Label htmlFor="qa-full-name">
              Họ và tên <span className="text-destructive">*</span>
            </Label>
            <Input id="qa-full-name" autoFocus {...register("full_name")} placeholder="Nguyễn Văn A" />
            {errors.full_name && <p className="text-xs text-destructive">{errors.full_name.message}</p>}
          </div>

          <div className="grid grid-cols-2 gap-3">
            <div className="space-y-1">
              <Label htmlFor="qa-dob">
                Ngày sinh <span className="text-destructive">*</span>
              </Label>
              <Input id="qa-dob" type="date" {...register("date_of_birth")} />
              {errors.date_of_birth && (
                <p className="text-xs text-destructive">{errors.date_of_birth.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <Label htmlFor="qa-phone">
                Số điện thoại <span className="text-destructive">*</span>
              </Label>
              <Input id="qa-phone" {...register("phone")} placeholder="09xxxxxxxx" />
              {errors.phone && <p className="text-xs text-destructive">{errors.phone.message}</p>}
            </div>
          </div>

          <div className="space-y-1">
            <Label>
              Giới tính <span className="text-destructive">*</span>
            </Label>
            <Select
              items={{ MALE: "Nam", FEMALE: "Nữ", OTHER: "Khác" }}
              value={watch("gender")}
              onValueChange={(v) => v && setValue("gender", v as QuickAddFormValues["gender"])}
            >
              <SelectTrigger>
                <SelectValue placeholder="Chọn giới tính" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="MALE">Nam</SelectItem>
                <SelectItem value="FEMALE">Nữ</SelectItem>
                <SelectItem value="OTHER">Khác</SelectItem>
              </SelectContent>
            </Select>
            {errors.gender && <p className="text-xs text-destructive">{errors.gender.message}</p>}
          </div>

          <div className="space-y-1">
            <Label htmlFor="qa-visit-date">Ngày khám</Label>
            <Input id="qa-visit-date" type="date" min={today} {...register("visit_date")} />
            <p className="text-xs text-muted-foreground">
              {isToday
                ? "Vào hàng đợi tiếp đón ngay sau khi tạo."
                : "Ngày tương lai — sẽ tạo lịch hẹn, chưa vào hàng đợi hôm nay."}
            </p>
          </div>

          {isToday && (
            <div className="space-y-1">
              <Label>
                Phòng khám <span className="text-destructive">*</span>
              </Label>
              <Select
                items={Object.fromEntries((rooms ?? []).map((r) => [r.id, r.name]))}
                value={watch("room_id") ?? ""}
                onValueChange={(v) => setValue("room_id", v ?? "")}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Chọn phòng khám" />
                </SelectTrigger>
                <SelectContent>
                  {(rooms ?? []).map((r) => (
                    <SelectItem key={r.id} value={r.id}>
                      {r.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {errors.room_id && <p className="text-xs text-destructive">{errors.room_id.message}</p>}
            </div>
          )}

          <div className="space-y-1">
            <Label htmlFor="qa-reason">Lý do khám</Label>
            <Input id="qa-reason" {...register("reason_for_visit")} placeholder="Đau đầu, sốt 3 ngày..." />
          </div>

          <DialogFooter>
            <Button type="button" variant="outline" onClick={close} disabled={isSubmitting}>
              Huỷ
            </Button>
            <Button type="submit" disabled={isSubmitting} className="gap-2">
              {isSubmitting && <Loader2 className="h-4 w-4 animate-spin" aria-hidden="true" />}
              {isToday ? "Đăng ký & vào hàng đợi" : "Đăng ký & tạo lịch hẹn"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}
