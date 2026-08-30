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
import type { CreatePatientRequest, PatientResponse, CccdDuplicateCheckResult } from "@/lib/api/types";
import { getErrorMessage } from "@/lib/utils/errors";
import { CccdQrScanner } from "@/components/domain/CccdQrScanner";
import { CccdMismatchDialog, type CccdFieldUpdateSelection } from "@/components/domain/CccdMismatchDialog";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { useCheckCccdDuplicate, useApplyCccdFields } from "@/lib/hooks/use-patients";
import type { CccdQrData } from "@/lib/utils/cccd-qr";
import { toast } from "sonner";

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
  /** Dữ liệu đã quét CCCD từ nơi khác (vd ReceptionCheckInForm) để tự điền + check trùng ngay khi mở trang. */
  initialCccdData?: CccdQrData | null;
}

function buildPayload(values: PatientFormValues): CreatePatientRequest {
  const { province_code, district_code, ward_code, street, email, ...rest } = values;
  // Chuẩn hoá: ô optional bỏ trống -> undefined (không gửi chuỗi rỗng "" khiến BE parse date/field lỗi 400)
  const emptyToUndef = <T,>(v: T): T | undefined =>
    typeof v === "string" && v.trim() === "" ? undefined : v;
  const normalized = Object.fromEntries(
    Object.entries(rest).map(([k, v]) => [k, emptyToUndef(v)]),
  ) as typeof rest;
  return {
    ...normalized,
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
  initialCccdData,
}: PatientEditorLayoutProps) {
  const router = useRouter();
  const [activeTab, setActiveTab] = useState("general");
  const [submitError, setSubmitError] = useState<string | null>(null);

  // ── Quét QR CCCD (US-QR-001..005) ──
  const [exactMatch, setExactMatch] = useState<CccdDuplicateCheckResult | null>(null);
  const [mismatch, setMismatch] = useState<CccdDuplicateCheckResult | null>(null);
  const checkCccdDuplicate = useCheckCccdDuplicate();
  const applyCccdFields = useApplyCccdFields(mismatch?.patient_id ?? "");

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

  // US-QR-001: điền form từ dữ liệu quét CCCD rồi check trùng (BR-DUP-001..005)
  const handleCccdScanned = useCallback(
    async (data: CccdQrData) => {
      if (data.full_name) setValue("full_name", data.full_name, { shouldDirty: true });
      if (data.gender) setValue("gender", data.gender, { shouldDirty: true });
      if (data.date_of_birth) setValue("date_of_birth", data.date_of_birth, { shouldDirty: true });
      if (data.id_number) setValue("id_number", data.id_number, { shouldDirty: true });
      if (data.address) setValue("street", data.address, { shouldDirty: true });
      if (data.issued_date) setValue("id_card_issued_date", data.issued_date, { shouldDirty: true });

      if (data.has_encoding_warning) {
        toast.warning("Có thể có lỗi encoding — vui lòng kiểm tra lại họ tên / địa chỉ");
      }

      if (!data.id_number) return; // BR-DUP-001: check trùng dựa trên số CCCD

      try {
        const result = await checkCccdDuplicate.mutateAsync({
          id_number: data.id_number,
          full_name: data.full_name ?? undefined,
          date_of_birth: data.date_of_birth ?? undefined,
          gender: data.gender ?? undefined,
          address: data.address ?? undefined,
        });
        if (result.case === "EXACT_MATCH") {
          setExactMatch(result);
        } else if (result.case === "FIELD_MISMATCH") {
          setMismatch(result);
        }
      } catch {
        // Loi check trung khong nen chan luong dien form (da toast trong hook)
      }
    },
    [setValue, checkCccdDuplicate]
  );

  const handleSaveMismatch = (fields: CccdFieldUpdateSelection[]) => {
    if (!mismatch?.patient_id) return;
    if (fields.length === 0) {
      setMismatch(null);
      return;
    }
    applyCccdFields.mutate(fields, {
      onSuccess: () => setMismatch(null),
    });
  };

  // Prefill từ nơi khác (vd quét CCCD tại quầy tiếp đón trước khi điều hướng sang đây)
  useEffect(() => {
    if (initialCccdData) {
      handleCccdScanned(initialCccdData);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [initialCccdData]);

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
      // BUG FIX (BUG-10): truoc day dung err.message truc tiep -> loi tieng Anh tho tu backend
      // (vd validation message goc). Dung getErrorMessage de uu tien error.response.data.error.message
      // tieng Viet tu backend, chi fallback ve err.message khi khong phai loi API.
      setSubmitError(getErrorMessage(err, "Có lỗi xảy ra, vui lòng thử lại."));
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
              disabled={isLoading || !!exactMatch}
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
                  <span className="bg-destructive text-destructive-foreground rounded-full w-4 h-4 flex items-center justify-center text-xs">
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
                <div className="space-y-6">
                  {mode === "create" && (
                    <CccdQrScanner onScanned={handleCccdScanned} className="max-w-2xl" />
                  )}
                  <PatientGeneralTab
                    register={register}
                    errors={errors}
                    watch={watch}
                    setValue={setValue}
                    autoFocus
                  />
                </div>
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
            disabled={isLoading || !!exactMatch}
          >
            <Save className="h-4 w-4 mr-1" />
            {isLoading ? "Đang lưu..." : mode === "create" ? "Tạo bệnh nhân" : "Lưu thay đổi"}
          </Button>
        </div>
      </footer>

      {/* US-QR-004 (Case 2 — BR-DUP-003): CCCD đã tồn tại, khớp hoàn toàn */}
      <Dialog open={!!exactMatch} onOpenChange={(o) => !o && setExactMatch(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="flex items-center gap-2">
              <AlertCircle className="h-4 w-4 text-amber-500" />
              Bệnh nhân đã có hồ sơ
            </DialogTitle>
            <DialogDescription>
              Số CCCD này đã được đăng ký trong hệ thống. Vui lòng mở hồ sơ cũ thay vì tạo mới.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-1 text-sm">
            <p>
              <span className="text-muted-foreground">Bệnh nhân: </span>
              <span className="font-medium">{exactMatch?.patient_full_name}</span>
            </p>
            <p>
              <span className="text-muted-foreground">Mã hồ sơ: </span>
              <span className="font-medium">{exactMatch?.patient_code}</span>
            </p>
            {exactMatch?.patient_date_of_birth && (
              <p>
                <span className="text-muted-foreground">Ngày sinh: </span>
                <span className="font-medium">{exactMatch.patient_date_of_birth}</span>
              </p>
            )}
          </div>
          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => setExactMatch(null)}>
              Thoát
            </Button>
            <Button
              type="button"
              onClick={() => exactMatch?.patient_id && router.push(`/patients/${exactMatch.patient_id}`)}
            >
              Mở hồ sơ cũ
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* US-QR-005 (Case 3 — BR-DUP-004/005): CCCD tồn tại, có trường lệch */}
      {mismatch && (
        <CccdMismatchDialog
          open={!!mismatch}
          onOpenChange={(o) => !o && setMismatch(null)}
          idNumber={watch("id_number") ?? ""}
          diffs={mismatch.field_diffs}
          isSaving={applyCccdFields.isPending}
          onSave={handleSaveMismatch}
        />
      )}
    </div>
  );
}
