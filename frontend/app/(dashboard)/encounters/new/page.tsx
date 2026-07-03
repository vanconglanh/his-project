"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { Search, User, Stethoscope } from "lucide-react";
import { usePatientSearch } from "@/lib/hooks/use-patients";
import { useUsers } from "@/lib/hooks/use-users";
import { useCreateEncounter } from "@/lib/hooks/use-encounters";
import { cn } from "@/lib/utils";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FieldGroup } from "@/components/ui/field-group";
import type { EncounterType } from "@/lib/api/types";

// ─── Schema ──────────────────────────────────────────────────────────────────

const schema = z.object({
  patient_id: z.string().min(1, "Vui lòng chọn bệnh nhân"),
  doctor_id: z.string().optional(),
  encounter_type: z.enum(["FIRST_VISIT", "FOLLOW_UP", "EMERGENCY", "CONSULTATION"]),
  reason_for_visit: z.string().min(1, "Vui lòng nhập lý do khám"),
  chief_complaint: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const TYPE_LABELS: Record<EncounterType, string> = {
  FIRST_VISIT: "Khám mới",
  FOLLOW_UP: "Tái khám",
  EMERGENCY: "Cấp cứu",
  CONSULTATION: "Hội chẩn",
};

const FORM_ID = "encounter-form";

// ─── Component ───────────────────────────────────────────────────────────────

export default function NewEncounterPage() {
  const router = useRouter();
  const createEncounter = useCreateEncounter();

  const [patientSearch, setPatientSearch] = useState("");
  const [patientLabel, setPatientLabel] = useState("");
  const [showPatientList, setShowPatientList] = useState(false);

  const { data: patientsData } = usePatientSearch(
    { q: patientSearch, page_size: 8 },
    patientSearch.length >= 2
  );
  const patients = patientsData?.data ?? [];

  const { data: usersData } = useUsers({ role: "BACSI", page_size: 100 });
  const doctors = usersData?.data ?? [];

  const {
    register,
    control,
    handleSubmit,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      patient_id: "",
      doctor_id: "",
      encounter_type: "FIRST_VISIT",
      reason_for_visit: "",
      chief_complaint: "",
    },
  });

  function handleSelectPatient(id: string, name: string) {
    setValue("patient_id", id, { shouldValidate: true });
    setPatientLabel(name);
    setPatientSearch(name);
    setShowPatientList(false);
  }

  function onSubmit(values: FormValues) {
    createEncounter.mutate(
      {
        patient_id: values.patient_id,
        doctor_id: values.doctor_id || undefined,
        encounter_type: values.encounter_type as EncounterType,
        reason_for_visit: values.reason_for_visit,
        chief_complaint: values.chief_complaint || undefined,
      },
      {
        onSuccess: (res) => {
          if (res?.id) router.push(`/encounters/${res.id}`);
        },
      }
    );
  }

  return (
    <FullPageFormShell
      title="Tạo lượt khám mới"
      description="Bệnh nhân, bác sĩ phụ trách và lý do khám"
      backHref="/encounters"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo lượt khám"
      isSubmitting={createEncounter.isPending}
    >
      <form id={FORM_ID} onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Section 1: Bệnh nhân & Bác sĩ */}
        <div className="rounded-lg border bg-card p-6 space-y-4">
          <div className="flex items-start gap-3">
            <User className="h-5 w-5 text-muted-foreground mt-0.5" />
            <div>
              <h2 className="text-base font-semibold">Bệnh nhân & Bác sĩ</h2>
              <p className="text-sm text-muted-foreground mt-0.5">
                Chọn bệnh nhân và bác sĩ phụ trách lượt khám
              </p>
            </div>
          </div>
          <Separator />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Bệnh nhân */}
            <div className="space-y-1 relative">
              <Label htmlFor="enc-patient-search">
                Bệnh nhân <span className="text-destructive">*</span>
              </Label>
              <div className="relative">
                <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                <Input
                  id="enc-patient-search"
                  placeholder="Tìm theo tên, SĐT..."
                  className={cn("pl-9", errors.patient_id && "border-destructive")}
                  value={patientSearch}
                  onChange={(e) => {
                    setPatientSearch(e.target.value);
                    setPatientLabel("");
                    setValue("patient_id", "", { shouldValidate: false });
                    setShowPatientList(true);
                  }}
                  onFocus={() => patientSearch.length >= 2 && setShowPatientList(true)}
                  autoComplete="off"
                  aria-invalid={!!errors.patient_id}
                  aria-describedby={errors.patient_id ? "enc-patient-error" : undefined}
                />
              </div>
              {showPatientList && patients.length > 0 && !patientLabel && (
                <div className="absolute z-50 left-0 right-0 top-full mt-1 border rounded-lg bg-background shadow-lg max-h-48 overflow-auto">
                  {patients.map((p) => (
                    <button
                      key={p.id}
                      type="button"
                      className="w-full text-left px-3 py-2 text-sm hover:bg-accent flex items-center justify-between"
                      onClick={() => handleSelectPatient(p.id, p.full_name)}
                    >
                      <span className="font-medium">{p.full_name}</span>
                      {p.phone && (
                        <span className="text-muted-foreground text-xs">{p.phone}</span>
                      )}
                    </button>
                  ))}
                </div>
              )}
              {errors.patient_id && (
                <p id="enc-patient-error" className="text-xs text-destructive">
                  {errors.patient_id.message}
                </p>
              )}
              {patientLabel && (
                <p className="text-xs text-green-600">Đã chọn: {patientLabel}</p>
              )}
              <input type="hidden" {...register("patient_id")} />
            </div>

            {/* Bác sĩ */}
            <FieldGroup label="Bác sĩ phụ trách" htmlFor="enc-doctor-select">
              <Controller
                control={control}
                name="doctor_id"
                render={({ field }) => (
                  <Select value={field.value ?? ""} onValueChange={(v) => field.onChange(v)}>
                    <SelectTrigger id="enc-doctor-select">
                      <SelectValue placeholder="Chọn bác sĩ (tuỳ chọn)..." />
                    </SelectTrigger>
                    <SelectContent>
                      <SelectItem value="">Chưa phân công</SelectItem>
                      {doctors.map((d) => (
                        <SelectItem key={d.id} value={d.id}>
                          {d.full_name}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FieldGroup>
          </div>
        </div>

        {/* Section 2: Loại khám & Lý do */}
        <div className="rounded-lg border bg-card p-6 space-y-4">
          <div className="flex items-start gap-3">
            <Stethoscope className="h-5 w-5 text-muted-foreground mt-0.5" />
            <div>
              <h2 className="text-base font-semibold">Thông tin khám</h2>
              <p className="text-sm text-muted-foreground mt-0.5">
                Loại khám và mô tả triệu chứng ban đầu
              </p>
            </div>
          </div>
          <Separator />

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {/* Loại khám */}
            <FieldGroup label="Loại khám" htmlFor="enc-type">
              <Controller
                control={control}
                name="encounter_type"
                render={({ field }) => (
                  <Select value={field.value} onValueChange={(v) => field.onChange(v as EncounterType)}>
                    <SelectTrigger id="enc-type">
                      <SelectValue />
                    </SelectTrigger>
                    <SelectContent>
                      {(Object.keys(TYPE_LABELS) as EncounterType[]).map((t) => (
                        <SelectItem key={t} value={t}>
                          {TYPE_LABELS[t]}
                        </SelectItem>
                      ))}
                    </SelectContent>
                  </Select>
                )}
              />
            </FieldGroup>

            {/* Lý do khám */}
            <FieldGroup
              label="Lý do khám"
              required
              htmlFor="enc-reason"
              error={errors.reason_for_visit?.message}
            >
              <Input
                id="enc-reason"
                placeholder="Vd: Đái tháo đường tái khám, sốt 3 ngày..."
                className={cn(errors.reason_for_visit && "border-destructive")}
                {...register("reason_for_visit")}
              />
            </FieldGroup>
          </div>

          {/* Chief complaint */}
          <FieldGroup label="Triệu chứng chính" htmlFor="enc-complaint">
            <Textarea
              id="enc-complaint"
              placeholder="Mô tả chi tiết triệu chứng bệnh nhân tự khai..."
              rows={4}
              style={{ minHeight: "120px" }}
              {...register("chief_complaint")}
            />
          </FieldGroup>
        </div>
      </form>
    </FullPageFormShell>
  );
}
