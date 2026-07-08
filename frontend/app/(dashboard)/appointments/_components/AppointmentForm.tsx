"use client";

import { useState } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Search, X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { FieldGroup } from "@/components/ui/field-group";
import { cn } from "@/lib/utils";
import { useDoctorOptions, useSearchPatients } from "@/lib/hooks/use-appointments";
import { APPOINTMENT_SOURCE_LABEL } from "./AppointmentStatusBadge";
import type {
  AppointmentResponse,
  AppointmentSource,
  AppointmentUpsertRequest,
} from "@/lib/api/appointments";

const SOURCES: AppointmentSource[] = ["WALK_IN", "PHONE", "WEB", "API", "APP"];

const schema = z
  .object({
    patient_ref: z.string().optional(),
    patient_name_temp: z.string().optional(),
    patient_phone: z.string().optional(),
    doctor_ref: z.string().optional(),
    appointment_at: z.string().min(1, "Vui lòng chọn ngày giờ hẹn"),
    duration_minutes: z
      .number({ message: "Nhập thời lượng" })
      .min(5, "Tối thiểu 5 phút")
      .max(480, "Tối đa 480 phút"),
    source: z.enum(["WALK_IN", "PHONE", "WEB", "API", "APP"]),
    note: z.string().optional(),
  })
  .refine((v) => Boolean(v.patient_ref) || Boolean(v.patient_name_temp?.trim()), {
    message: "Chọn bệnh nhân đã có hồ sơ hoặc nhập tên khách vãng lai",
    path: ["patient_name_temp"],
  });

type FormValues = z.infer<typeof schema>;

interface Props {
  formId: string;
  defaultValues?: AppointmentResponse | null;
  onSubmit: (body: AppointmentUpsertRequest) => void;
}

/** Chuyển ISO datetime -> giá trị cho input[type=datetime-local] (giờ local) */
function toLocalInputValue(iso: string): string {
  const d = new Date(iso);
  if (isNaN(d.getTime())) return "";
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(
    d.getHours()
  )}:${pad(d.getMinutes())}`;
}

export function AppointmentForm({ formId, defaultValues, onSubmit }: Props) {
  // Patient search combobox state
  const [mode, setMode] = useState<"search" | "manual">(
    defaultValues && !defaultValues.patient_ref ? "manual" : "search"
  );
  const [patientQuery, setPatientQuery] = useState("");
  const [patientLabel, setPatientLabel] = useState(defaultValues?.patient_name ?? "");
  const [showPatientList, setShowPatientList] = useState(false);

  const { data: doctorOptions = [] } = useDoctorOptions();
  const { data: patientOptions = [] } = useSearchPatients(patientQuery);

  const {
    register,
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      patient_ref: defaultValues?.patient_ref ?? "",
      patient_name_temp: defaultValues?.patient_ref ? "" : defaultValues?.patient_name ?? "",
      patient_phone: defaultValues?.patient_phone ?? "",
      doctor_ref: defaultValues?.doctor_ref ?? "",
      appointment_at: defaultValues ? toLocalInputValue(defaultValues.appointment_at) : "",
      duration_minutes: defaultValues?.duration_minutes ?? 30,
      source: defaultValues?.source ?? "WALK_IN",
      note: defaultValues?.note ?? "",
    },
  });

  const source = watch("source");
  const patientRef = watch("patient_ref");

  function handleSelectPatient(id: string, label: string, phone?: string | null) {
    setValue("patient_ref", id, { shouldValidate: true });
    setValue("patient_name_temp", "", { shouldValidate: false });
    setValue("patient_phone", phone ?? "", { shouldValidate: false });
    setPatientLabel(label);
    setPatientQuery("");
    setShowPatientList(false);
  }

  function handleClearPatient() {
    setValue("patient_ref", "", { shouldValidate: false });
    setPatientLabel("");
    setPatientQuery("");
  }

  function handleSwitchMode(next: "search" | "manual") {
    setMode(next);
    setValue("patient_ref", "", { shouldValidate: false });
    setValue("patient_name_temp", "", { shouldValidate: false });
    setValue("patient_phone", "", { shouldValidate: false });
    setPatientLabel("");
    setPatientQuery("");
  }

  function handleSubmitForm(values: FormValues) {
    onSubmit({
      patient_ref: values.patient_ref || undefined,
      patient_name_temp: values.patient_ref ? undefined : values.patient_name_temp,
      patient_phone: values.patient_phone || undefined,
      doctor_ref: values.doctor_ref || undefined,
      appointment_at: new Date(values.appointment_at).toISOString(),
      duration_minutes: values.duration_minutes,
      source: values.source,
      note: values.note || undefined,
    });
  }

  return (
    <form id={formId} onSubmit={handleSubmit(handleSubmitForm)} className="space-y-6">
      <div className="rounded-lg border bg-card p-6 space-y-4 max-w-2xl">
        <div>
          <h2 className="text-base font-semibold">Thông tin lịch hẹn</h2>
          <p className="text-sm text-muted-foreground mt-0.5">
            Chọn bệnh nhân, bác sĩ và thời gian hẹn khám.
          </p>
        </div>
        <Separator />

        {/* Bệnh nhân */}
        <div className="space-y-1.5">
          <div className="flex items-center justify-between">
            <label className="text-sm font-medium">
              Bệnh nhân <span className="text-destructive">*</span>
            </label>
            <div className="flex gap-1 text-xs">
              <button
                type="button"
                className={cn(
                  "rounded px-2 py-0.5",
                  mode === "search" ? "bg-primary/10 text-primary font-medium" : "text-muted-foreground"
                )}
                onClick={() => handleSwitchMode("search")}
              >
                Tìm hồ sơ
              </button>
              <button
                type="button"
                className={cn(
                  "rounded px-2 py-0.5",
                  mode === "manual" ? "bg-primary/10 text-primary font-medium" : "text-muted-foreground"
                )}
                onClick={() => handleSwitchMode("manual")}
              >
                Khách vãng lai
              </button>
            </div>
          </div>

          {mode === "search" ? (
            <div className="relative">
              {patientRef && patientLabel ? (
                <div className="flex items-center justify-between rounded-lg border px-3 py-2 text-sm bg-muted/40">
                  <span className="font-medium">{patientLabel}</span>
                  <button
                    type="button"
                    onClick={handleClearPatient}
                    className="rounded-sm hover:bg-destructive/20 p-0.5"
                    aria-label="Bỏ chọn bệnh nhân"
                  >
                    <X className="h-3.5 w-3.5" />
                  </button>
                </div>
              ) : (
                <>
                  <div className="relative">
                    <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Tìm theo tên, SĐT..."
                      className="pl-9"
                      value={patientQuery}
                      onChange={(e) => {
                        setPatientQuery(e.target.value);
                        setShowPatientList(true);
                      }}
                      onFocus={() => patientQuery.length >= 2 && setShowPatientList(true)}
                      autoComplete="off"
                    />
                  </div>
                  {showPatientList && patientOptions.length > 0 && (
                    <div className="absolute z-50 left-0 right-0 top-full mt-1 border rounded-lg bg-background shadow-lg max-h-48 overflow-auto">
                      {patientOptions.map((p) => (
                        <button
                          key={p.value}
                          type="button"
                          className="w-full text-left px-3 py-2 text-sm hover:bg-accent flex items-center justify-between"
                          onClick={() => handleSelectPatient(p.value, p.label, p.phone)}
                        >
                          <span className="font-medium">{p.label}</span>
                          {p.phone && (
                            <span className="text-muted-foreground text-xs">{p.phone}</span>
                          )}
                        </button>
                      ))}
                    </div>
                  )}
                </>
              )}
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-3">
              <Input
                placeholder="Tên khách vãng lai"
                {...register("patient_name_temp")}
              />
              <Input placeholder="Số điện thoại" {...register("patient_phone")} />
            </div>
          )}
          {errors.patient_name_temp && (
            <p className="text-xs text-destructive">{errors.patient_name_temp.message}</p>
          )}
        </div>

        {/* Bác sĩ + Ngày giờ */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FieldGroup label="Bác sĩ" htmlFor="doctor-select">
            <Controller
              control={control}
              name="doctor_ref"
              render={({ field }) => (
                <Select
                  items={Object.fromEntries(doctorOptions.map((d) => [d.value, d.label]))}
                  value={field.value || undefined}
                  onValueChange={field.onChange}
                >
                  <SelectTrigger id="doctor-select">
                    <SelectValue placeholder="Không chỉ định" />
                  </SelectTrigger>
                  <SelectContent>
                    {doctorOptions.map((d) => (
                      <SelectItem key={d.value} value={d.value}>
                        {d.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FieldGroup>

          <FieldGroup
            label="Ngày giờ hẹn"
            required
            htmlFor="appointment_at"
            error={errors.appointment_at?.message}
          >
            <Input
              id="appointment_at"
              type="datetime-local"
              {...register("appointment_at")}
            />
          </FieldGroup>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <FieldGroup
            label="Thời lượng (phút)"
            htmlFor="duration_minutes"
            error={errors.duration_minutes?.message}
          >
            <Input
              id="duration_minutes"
              type="number"
              min={5}
              max={480}
              step={5}
              {...register("duration_minutes", { valueAsNumber: true })}
            />
          </FieldGroup>

          <FieldGroup label="Nguồn" htmlFor="source-select">
            <Select
              items={Object.fromEntries(SOURCES.map((s) => [s, APPOINTMENT_SOURCE_LABEL[s]]))}
              value={source}
              onValueChange={(v) => setValue("source", v as AppointmentSource)}
            >
              <SelectTrigger id="source-select">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {SOURCES.map((s) => (
                  <SelectItem key={s} value={s}>
                    {APPOINTMENT_SOURCE_LABEL[s]}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </FieldGroup>
        </div>

        <FieldGroup label="Ghi chú" htmlFor="note">
          <Textarea id="note" rows={3} placeholder="Lý do khám, lưu ý..." {...register("note")} />
        </FieldGroup>
      </div>
    </form>
  );
}
