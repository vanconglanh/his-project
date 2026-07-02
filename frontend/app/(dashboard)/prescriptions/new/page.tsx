"use client";

import { useState, useCallback, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useForm, useFieldArray, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Separator } from "@/components/ui/separator";
import { ArrowLeft, Plus, Trash2, Search, X } from "lucide-react";
import { usePatientSearch } from "@/lib/hooks/use-patients";
import { useUsers } from "@/lib/hooks/use-users";
import { useIcd10Search } from "@/lib/hooks/use-icd10";
import { useCreatePrescription } from "@/lib/hooks/use-prescriptions";
import { cn } from "@/lib/utils";
import type { PrescriptionDiagnosis } from "@/lib/api/prescriptions";

// ─── Schema ──────────────────────────────────────────────────────────────────

const prescriptionSchema = z.object({
  patient_id: z.string().min(1, "Vui lòng chọn bệnh nhân"),
  doctor_id: z.string().min(1, "Vui lòng chọn bác sĩ kê đơn"),
  diagnoses: z
    .array(
      z.object({
        icd10_code: z.string(),
        icd10_name: z.string(),
        is_primary: z.boolean(),
      })
    )
    .min(1, "Vui lòng chọn ít nhất 1 chẩn đoán ICD-10"),
  note: z.string().optional(),
});

type PrescriptionFormValues = z.infer<typeof prescriptionSchema>;

// ─── Component ───────────────────────────────────────────────────────────────

export default function NewPrescriptionPage() {
  const router = useRouter();
  const createPrescription = useCreatePrescription();

  // Patient search state
  const [patientSearch, setPatientSearch] = useState("");
  const [patientLabel, setPatientLabel] = useState("");
  const [showPatientList, setShowPatientList] = useState(false);

  // ICD-10 search state
  const [icdSearch, setIcdSearch] = useState("");
  const [showIcdList, setShowIcdList] = useState(false);

  const { data: patientsData } = usePatientSearch(
    { q: patientSearch, page_size: 8 },
    patientSearch.length >= 2
  );
  const patients = patientsData?.data ?? [];

  const { data: usersData } = useUsers({ role: "BACSI", page_size: 100 });
  const doctors = (usersData?.data ?? []).filter((u) =>
    u.roles.some((r) => r.code === "BACSI")
  );

  const { data: icd10Data } = useIcd10Search({
    q: icdSearch,
    limit: 10,
    billable_only: true,
  });
  const icd10Results = (icd10Data ?? []) as Array<{ code: string; name_vi: string; name_en: string }>;

  const {
    register,
    control,
    handleSubmit,
    setValue,
    watch,
    formState: { errors },
  } = useForm<PrescriptionFormValues>({
    resolver: zodResolver(prescriptionSchema),
    defaultValues: {
      patient_id: "",
      doctor_id: "",
      diagnoses: [],
      note: "",
    },
  });

  const { fields: diagFields, append: appendDiag, remove: removeDiag } =
    useFieldArray({ control, name: "diagnoses" });

  const diagnoses = watch("diagnoses");

  function handleSelectPatient(id: string, name: string) {
    setValue("patient_id", id, { shouldValidate: true });
    setPatientLabel(name);
    setPatientSearch(name);
    setShowPatientList(false);
  }

  function handleAddDiagnosis(item: { code: string; name: string }) {
    const already = diagnoses.some((d) => d.icd10_code === item.code);
    if (already) return;
    const isPrimary = diagnoses.length === 0;
    appendDiag({ icd10_code: item.code, icd10_name: item.name, is_primary: isPrimary });
    setIcdSearch("");
    setShowIcdList(false);
  }

  // Keyboard shortcuts
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        handleSubmit(handleSubmitForm)();
      }
      if (e.key === "Escape") {
        router.back();
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  function handleSubmitForm(values: PrescriptionFormValues) {
    createPrescription.mutate(
      {
        patient_id: values.patient_id,
        doctor_id: values.doctor_id,
        diagnoses: values.diagnoses as PrescriptionDiagnosis[],
        note: values.note || undefined,
        items: [],
      },
      {
        onSuccess: (res) => {
          router.push(`/prescriptions/${res.id}`);
        },
      }
    );
  }

  return (
    <div className="min-h-screen flex flex-col">
      {/* Sticky Header */}
      <header className="sticky top-0 z-40 h-14 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 flex items-center px-4 lg:px-6 gap-3">
        <Link
          href="/prescriptions"
          className="flex items-center gap-1 text-sm text-muted-foreground hover:text-foreground"
          aria-label="Quay lại danh sách đơn thuốc"
        >
          <ArrowLeft className="h-4 w-4" />
          Kê đơn thuốc
        </Link>
        <span className="text-muted-foreground">/</span>
        <span className="text-sm font-medium">Tạo đơn thuốc mới</span>

        <div className="ml-auto flex items-center gap-2">
          <Button
            type="button"
            variant="outline"
            size="sm"
            onClick={() => router.back()}
          >
            Huỷ
          </Button>
          <Button
            type="submit"
            form="prescription-form"
            size="sm"
            disabled={createPrescription.isPending}
          >
            {createPrescription.isPending ? "Đang lưu..." : "Tạo đơn thuốc"}
          </Button>
        </div>
      </header>

      {/* Content */}
      <main className="flex-1 max-w-5xl mx-auto w-full px-4 lg:px-8 py-8 pb-24 lg:pb-8">
        <form id="prescription-form" onSubmit={handleSubmit(handleSubmitForm)} className="space-y-6">

          {/* Section 1: Thông tin bệnh nhân & bác sĩ */}
          <div className="rounded-lg border bg-card p-6 space-y-4">
            <div>
              <h2 className="text-base font-semibold">Thông tin đơn thuốc</h2>
              <p className="text-sm text-muted-foreground mt-0.5">
                Bệnh nhân, bác sĩ kê đơn và chẩn đoán — bắt buộc theo TT 27/2021
              </p>
            </div>
            <Separator />

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* Bệnh nhân */}
              <div className="space-y-1 relative">
                <Label htmlFor="patient-search">
                  Bệnh nhân <span className="text-destructive">*</span>
                </Label>
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    id="patient-search"
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
                    aria-describedby={errors.patient_id ? "patient-error" : undefined}
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
                  <p id="patient-error" className="text-xs text-destructive">
                    {errors.patient_id.message}
                  </p>
                )}
                {patientLabel && (
                  <p className="text-xs text-green-600">Đã chọn: {patientLabel}</p>
                )}
                <input type="hidden" {...register("patient_id")} />
              </div>

              {/* Bác sĩ */}
              <div className="space-y-1">
                <Label htmlFor="doctor-select">
                  Bác sĩ kê đơn <span className="text-destructive">*</span>
                </Label>
                <Controller
                  control={control}
                  name="doctor_id"
                  render={({ field }) => (
                    <Select
                      value={field.value}
                      onValueChange={(v) => field.onChange(v)}
                    >
                      <SelectTrigger
                        id="doctor-select"
                        className={cn(errors.doctor_id && "border-destructive")}
                        aria-invalid={!!errors.doctor_id}
                        aria-describedby={errors.doctor_id ? "doctor-error" : undefined}
                      >
                        <SelectValue placeholder="Chọn bác sĩ..." />
                      </SelectTrigger>
                      <SelectContent>
                        {doctors.length === 0 ? (
                          <SelectItem value="_empty" disabled>
                            Không có bác sĩ
                          </SelectItem>
                        ) : (
                          doctors.map((d) => (
                            <SelectItem key={d.id} value={d.id}>
                              {d.full_name}
                            </SelectItem>
                          ))
                        )}
                      </SelectContent>
                    </Select>
                  )}
                />
                {errors.doctor_id && (
                  <p id="doctor-error" className="text-xs text-destructive">
                    {errors.doctor_id.message}
                  </p>
                )}
              </div>
            </div>
          </div>

          {/* Section 2: Chẩn đoán ICD-10 */}
          <div className="rounded-lg border bg-card p-6 space-y-4">
            <div>
              <h2 className="text-base font-semibold">Chẩn đoán ICD-10</h2>
              <p className="text-sm text-muted-foreground mt-0.5">
                Bắt buộc ít nhất 1. Chẩn đoán đầu tiên sẽ là chẩn đoán chính.
              </p>
            </div>
            <Separator />

            {/* Danh sách đã chọn */}
            {diagFields.length > 0 && (
              <div className="flex flex-wrap gap-2">
                {diagFields.map((field, idx) => (
                  <Badge
                    key={field.id}
                    variant={diagnoses[idx]?.is_primary ? "default" : "secondary"}
                    className="flex items-center gap-1.5 pr-1 py-1 text-sm"
                  >
                    <span className="font-mono text-xs">{diagnoses[idx]?.icd10_code}</span>
                    <span>{diagnoses[idx]?.icd10_name}</span>
                    {diagnoses[idx]?.is_primary && (
                      <span className="text-xs opacity-70">(chính)</span>
                    )}
                    <button
                      type="button"
                      onClick={() => removeDiag(idx)}
                      className="ml-1 rounded-sm hover:bg-destructive/20 p-0.5"
                      aria-label={`Xoá ${diagnoses[idx]?.icd10_code}`}
                    >
                      <X className="h-3 w-3" />
                    </button>
                  </Badge>
                ))}
              </div>
            )}

            {/* Search ICD-10 */}
            <div className="relative max-w-md">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Tìm mã hoặc tên ICD-10..."
                className={cn("pl-9", errors.diagnoses && diagFields.length === 0 && "border-destructive")}
                value={icdSearch}
                onChange={(e) => {
                  setIcdSearch(e.target.value);
                  setShowIcdList(true);
                }}
                onFocus={() => icdSearch.length >= 1 && setShowIcdList(true)}
                autoComplete="off"
                aria-label="Tìm chẩn đoán ICD-10"
              />
              {showIcdList && icd10Results.length > 0 && (
                <div className="absolute z-50 left-0 right-0 top-full mt-1 border rounded-lg bg-background shadow-lg max-h-48 overflow-auto">
                  {icd10Results.map((item) => (
                    <button
                      key={item.code}
                      type="button"
                      className="w-full text-left px-3 py-2 text-sm hover:bg-accent flex items-start gap-2"
                      onClick={() => handleAddDiagnosis({ code: item.code, name: item.name_vi })}
                    >
                      <span className="font-mono text-xs text-muted-foreground w-16 shrink-0 pt-0.5">
                        {item.code}
                      </span>
                      <span>{item.name_vi}</span>
                    </button>
                  ))}
                </div>
              )}
            </div>

            {errors.diagnoses && (
              <p className="text-xs text-destructive">
                {typeof errors.diagnoses.message === "string"
                  ? errors.diagnoses.message
                  : "Vui lòng chọn ít nhất 1 chẩn đoán"}
              </p>
            )}
          </div>

          {/* Section 3: Ghi chú */}
          <div className="rounded-lg border bg-card p-6 space-y-4">
            <h2 className="text-base font-semibold">Ghi chú đơn</h2>
            <Separator />
            <div className="space-y-1">
              <Label htmlFor="note">Ghi chú</Label>
              <Textarea
                id="note"
                placeholder="Lưu ý đặc biệt, hướng dẫn thêm..."
                rows={3}
                {...register("note")}
              />
            </div>
          </div>
        </form>
      </main>

      {/* Sticky Footer */}
      <footer className="hidden lg:flex sticky bottom-0 z-30 h-14 border-t bg-background/95 backdrop-blur px-6 items-center justify-between">
        <span className="text-xs text-muted-foreground">Ctrl+S lưu · Esc quay lại</span>
        <div className="flex items-center gap-2">
          <Button type="button" variant="outline" size="sm" onClick={() => router.back()}>
            Huỷ
          </Button>
          <Button
            type="submit"
            form="prescription-form"
            size="sm"
            disabled={createPrescription.isPending}
          >
            {createPrescription.isPending ? "Đang lưu..." : "Tạo đơn thuốc"}
          </Button>
        </div>
      </footer>
    </div>
  );
}
