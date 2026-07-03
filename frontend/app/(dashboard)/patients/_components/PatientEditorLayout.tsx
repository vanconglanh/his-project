"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter } from "next/navigation";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { ArrowLeft, Save, X, AlertCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { cn } from "@/lib/utils";
import { patientSchema, type PatientFormValues } from "./patient-schema";
import { PatientGeneralTab } from "./PatientGeneralTab";
import { PatientBhytTab } from "./PatientBhytTab";
import { PatientEmergencyTab } from "./PatientEmergencyTab";
import { PatientAllergiesTab } from "./PatientAllergiesTab";
import type { CreatePatientRequest, PatientResponse } from "@/lib/api/types";

// Tab định nghĩa
const EDITOR_TABS = [
  { id: "general", label: "Thông tin chung", fields: ["full_name", "gender", "date_of_birth", "phone", "email", "id_number", "blood_type", "occupation", "ethnicity", "province_code", "district_code", "ward_code", "street", "id_card_issued_date", "id_card_issued_place", "nationality", "patient_type", "marital_status", "visit_type"] as (keyof PatientFormValues)[] },
  { id: "bhyt", label: "Bảo hiểm y tế", fields: [] as (keyof PatientFormValues)[] },
  { id: "emergency", label: "Liên hệ khẩn cấp", fields: [] as (keyof PatientFormValues)[] },
  { id: "allergies", label: "Dị ứng", fields: [] as (keyof PatientFormValues)[] },
];

export interface PatientEditorLayoutProps {
  mode: "create" | "edit";
  defaultValues?: Partial<PatientResponse>;
  onSubmit: (data: CreatePatientRequest) => Promise<void>;
  onCancel: () => void;
  isLoading?: boolean;
  title?: string;
}

function buildPayload(values: PatientFormValues): CreatePatientRequest {
  const { province_code, district_code, ward_code, street, email, ...rest } = values;
  return {
    ...rest,
    email: email || undefined,
    address:
      province_code || district_code || ward_code || street
        ? { province_code, district_code, ward_code, street }
        : undefined,
  };
}

function buildDefaultValues(src?: Partial<PatientResponse>): Partial<PatientFormValues> {
  if (!src) return {};
  return {
    full_name: src.full_name ?? "",
    gender: src.gender,
    date_of_birth: src.date_of_birth ?? "",
    phone: src.phone ?? "",
    email: src.email ?? "",
    id_number: src.id_number ?? "",
    blood_type: src.blood_type,
    occupation: src.occupation ?? "",
    ethnicity: src.ethnicity ?? "",
    province_code: src.address?.province_code ?? "",
    district_code: src.address?.district_code ?? "",
    ward_code: src.address?.ward_code ?? "",
    street: src.address?.street ?? "",
    id_card_issued_date: src.id_card_issued_date ?? "",
    id_card_issued_place: src.id_card_issued_place ?? "",
    nationality: src.nationality ?? "VN",
    patient_type: src.patient_type ?? "SERVICE",
    marital_status: src.marital_status ?? undefined,
    visit_type: src.visit_type ?? "FIRST_VISIT",
  };
}

export function PatientEditorLayout({
  mode,
  defaultValues,
  onSubmit,
  onCancel,
  isLoading,
  title,
}: PatientEditorLayoutProps) {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState("general");
  const [submitError, setSubmitError] = useState<string | null>(null);

  const pageTitle =
    title ??
    (mode === "create"
      ? "Tạo bệnh nhân mới"
      : `Chỉnh sửa bệnh nhân${defaultValues?.full_name ? ` — ${defaultValues.full_name}` : ""}`);

  const {
    register,
    handleSubmit,
    setValue,
    watch,
    formState: { errors, isDirty },
    reset,
    trigger,
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  } = useForm<PatientFormValues, any, PatientFormValues>({
    resolver: zodResolver(patientSchema) as any,
    defaultValues: buildDefaultValues(defaultValues),
  });

  // Reset form khi defaultValues thay đổi (edit mode load data async)
  useEffect(() => {
    if (defaultValues) {
      reset(buildDefaultValues(defaultValues));
    }
  }, [defaultValues, reset]);

  // Unsaved changes warning
  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (isDirty) {
        e.preventDefault();
        e.returnValue = "";
      }
    };
    window.addEventListener("beforeunload", handler);
    return () => window.removeEventListener("beforeunload", handler);
  }, [isDirty]);

  // Keyboard shortcuts
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === "s") {
        e.preventDefault();
        handleSubmit(handleFormSubmit)();
      }
      if (e.key === "Escape") {
        handleCancel();
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isDirty]);

  const handleCancel = useCallback(() => {
    if (isDirty) {
      if (!window.confirm("Bạn có thay đổi chưa lưu. Rời trang sẽ mất dữ liệu. Tiếp tục?")) {
        return;
      }
    }
    onCancel();
  }, [isDirty, onCancel]);

  const handleFormSubmit = async (values: PatientFormValues) => {
    setSubmitError(null);
    try {
      await onSubmit(buildPayload(values));
    } catch (err: unknown) {
      setSubmitError(err instanceof Error ? err.message : "Có lỗi xảy ra, vui lòng thử lại.");
    }
  };

  // Đếm lỗi theo tab
  const getTabErrorCount = (tabId: string): number => {
    const tab = EDITOR_TABS.find((t) => t.id === tabId);
    if (!tab || tab.fields.length === 0) return 0;
    return tab.fields.filter((f) => !!errors[f]).length;
  };

  // Khi submit thất bại validation → nhảy tab có lỗi đầu tiên
  const onInvalid = () => {
    for (const tab of EDITOR_TABS) {
      const hasError = tab.fields.some((f) => !!errors[f]);
      if (hasError) {
        setActiveTab(tab.id);
        break;
      }
    }
  };

  return (
    <div className="min-h-screen flex flex-col bg-background">
      {/* Header sticky */}
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex h-14 items-center gap-4 px-4 lg:px-6">
          {/* Quay lại */}
          <button
            type="button"
            onClick={handleCancel}
            className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
            aria-label="Quay lại"
          >
            <ArrowLeft className="h-4 w-4" />
            <span className="hidden sm:inline">Quay lại</span>
          </button>

          {/* Title center */}
          <h1 className="flex-1 text-center text-base font-semibold truncate">
            {pageTitle}
          </h1>

          {/* Actions */}
          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleCancel}
              disabled={isLoading}
              className="hidden sm:inline-flex"
            >
              <X className="h-4 w-4 mr-1" />
              Huỷ
            </Button>
            <Button
              type="button"
              size="sm"
              onClick={handleSubmit(handleFormSubmit, onInvalid)}
              disabled={isLoading}
            >
              <Save className="h-4 w-4 mr-1" />
              {isLoading ? "Đang lưu..." : mode === "create" ? "Tạo bệnh nhân" : "Lưu thay đổi"}
            </Button>
          </div>
        </div>
      </header>

      {/* Body: sidebar + content */}
      <div className="flex flex-1">
        {/* Sidebar tab — hidden on mobile, shown on lg+ */}
        <aside className="hidden lg:flex w-52 shrink-0 flex-col border-r pt-6 pb-6 px-3 sticky top-14 h-[calc(100vh-3.5rem)] overflow-y-auto">
          <nav className="space-y-1" aria-label="Nhóm thông tin bệnh nhân">
            {EDITOR_TABS.map((tab) => {
              const errCount = getTabErrorCount(tab.id);
              return (
                <button
                  key={tab.id}
                  type="button"
                  onClick={() => setActiveTab(tab.id)}
                  className={cn(
                    "w-full flex items-center justify-between rounded-md px-3 py-2 text-sm font-medium transition-colors text-left",
                    activeTab === tab.id
                      ? "bg-primary/10 text-primary"
                      : "text-muted-foreground hover:bg-accent hover:text-foreground"
                  )}
                >
                  <span>{tab.label}</span>
                  {errCount > 0 && (
                    <Badge variant="destructive" className="h-5 px-1.5 text-xs">
                      {errCount}
                    </Badge>
                  )}
                </button>
              );
            })}
          </nav>
        </aside>

        {/* Mobile tab bar */}
        <div className="lg:hidden fixed bottom-16 left-0 right-0 z-30 border-t bg-background px-2 flex overflow-x-auto gap-1 py-2">
          {EDITOR_TABS.map((tab) => {
            const errCount = getTabErrorCount(tab.id);
            return (
              <button
                key={tab.id}
                type="button"
                onClick={() => setActiveTab(tab.id)}
                className={cn(
                  "flex items-center gap-1 whitespace-nowrap rounded-md px-3 py-1.5 text-xs font-medium transition-colors shrink-0",
                  activeTab === tab.id
                    ? "bg-primary text-primary-foreground"
                    : "bg-muted text-muted-foreground hover:bg-accent"
                )}
              >
                {tab.label}
                {errCount > 0 && (
                  <span className="bg-destructive text-destructive-foreground rounded-full w-4 h-4 flex items-center justify-center text-[10px]">
                    {errCount}
                  </span>
                )}
              </button>
            );
          })}
        </div>

        {/* Content area */}
        <main className="flex-1 overflow-y-auto">
          <form
            id="patient-editor-form"
            onSubmit={handleSubmit(handleFormSubmit, onInvalid)}
            noValidate
          >
            <div className="w-full px-4 lg:px-8 py-8 pb-32 lg:pb-8">
              {/* Submit error */}
              {submitError && (
                <div className="mb-4 flex items-start gap-2 rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-3 text-sm text-destructive">
                  <AlertCircle className="h-4 w-4 mt-0.5 shrink-0" />
                  {submitError}
                </div>
              )}

              {/* Tab content */}
              {activeTab === "general" && (
                <PatientGeneralTab
                  register={register}
                  errors={errors}
                  watch={watch}
                  setValue={setValue}
                  autoFocus
                />
              )}
              {activeTab === "bhyt" && (
                <PatientBhytTab
                  register={register}
                  errors={errors}
                  watch={watch}
                  setValue={setValue}
                />
              )}
              {activeTab === "emergency" && (
                <PatientEmergencyTab
                  register={register}
                  errors={errors}
                  watch={watch}
                  setValue={setValue}
                />
              )}
              {activeTab === "allergies" && (
                <PatientAllergiesTab
                  register={register}
                  errors={errors}
                  watch={watch}
                  setValue={setValue}
                />
              )}
            </div>
          </form>
        </main>
      </div>

      {/* Footer sticky bottom (desktop) */}
      <footer className="hidden lg:flex sticky bottom-0 z-30 border-t bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60 px-6 py-3 items-center justify-between">
        <div className="flex items-center gap-2 text-xs text-muted-foreground">
          <kbd className="border rounded px-1 py-0.5 font-mono">Ctrl+S</kbd>
          <span>lưu</span>
          <span className="mx-1">·</span>
          <kbd className="border rounded px-1 py-0.5 font-mono">Esc</kbd>
          <span>quay lại</span>
        </div>
        <div className="flex items-center gap-2">
          <Button type="button" variant="outline" size="sm" onClick={handleCancel} disabled={isLoading}>
            Huỷ
          </Button>
          <Button
            type="button"
            size="sm"
            onClick={handleSubmit(handleFormSubmit, onInvalid)}
            disabled={isLoading}
          >
            <Save className="h-4 w-4 mr-1" />
            {isLoading ? "Đang lưu..." : mode === "create" ? "Tạo bệnh nhân" : "Lưu thay đổi"}
          </Button>
        </div>
      </footer>
    </div>
  );
}
