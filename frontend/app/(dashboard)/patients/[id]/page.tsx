"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { HisStatusBadge, type HisStatusVariant } from "@/components/ui/status-badge";
import { cn } from "@/lib/utils";
import { usePatient, useUpdateReceptionNote, usePatientEncounters } from "@/lib/hooks/use-patients";
import { PatientAvatar } from "@/components/domain/PatientAvatar";
import { AllergyList } from "@/components/domain/AllergyList";
import { BhytForm } from "@/components/domain/BhytForm";
import { EmergencyContactList } from "@/components/domain/EmergencyContactList";
import { ConsentList } from "@/components/domain/ConsentList";
import { ClsUploadList } from "@/components/domain/ClsUploadList";
import { formatDate, formatDateTime } from "@/lib/utils/format";
import type { Gender } from "@/lib/api/types";

const GENDER_LABELS: Record<Gender, string> = {
  MALE: "Nam",
  FEMALE: "Nữ",
  OTHER: "Khác",
};

const TABS = [
  { id: "profile", label: "Hồ sơ" },
  { id: "bhyt", label: "BHYT" },
  { id: "allergy", label: "Dị ứng" },
  { id: "emergency", label: "Liên hệ khẩn cấp" },
  { id: "consent", label: "Đồng ý" },
  { id: "history", label: "Lịch sử khám" },
  { id: "cls", label: "Kết quả CLS" },
];

export default function PatientDetailPage() {
  const params = useParams();
  const router = useRouter();
  const id = params.id as string;
  const [activeTab, setActiveTab] = useState("profile");
  const [noteValue, setNoteValue] = useState<string | null>(null);
  const [noteSaving, setNoteSaving] = useState(false);

  const { data: patient, isLoading, error } = usePatient(id);
  const updateNoteMutation = useUpdateReceptionNote(id);

  const currentNote = noteValue ?? patient?.reception_note ?? "";

  const handleNoteBlur = async () => {
    if (patient && currentNote !== (patient.reception_note ?? "")) {
      setNoteSaving(true);
      try {
        await updateNoteMutation.mutateAsync(currentNote);
      } finally {
        setNoteSaving(false);
      }
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-48" />
        <div className="flex items-center gap-4">
          <Skeleton className="h-24 w-24 rounded-full" />
          <div className="space-y-2">
            <Skeleton className="h-7 w-48" />
            <Skeleton className="h-5 w-32" />
          </div>
        </div>
        <Skeleton className="h-10 w-full" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (error || !patient) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-4 text-muted-foreground">
        <AlertTriangle className="h-10 w-10" />
        <p>Không tìm thấy bệnh nhân</p>
        <Button variant="outline" onClick={() => router.back()}>Quay lại</Button>
      </div>
    );
  }

  const isBhytActive = patient.bhyt_valid_to
    ? new Date(patient.bhyt_valid_to) >= new Date()
    : false;

  return (
    <div className="space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2">
        <Button variant="ghost" size="sm" onClick={() => router.back()} className="gap-1.5 -ml-2">
          <ArrowLeft className="h-4 w-4" />
          Danh sách bệnh nhân
        </Button>
      </div>

      {/* Reception note alert */}
      {patient.reception_note && (
        <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 p-3 dark:border-amber-800 dark:bg-amber-950/30">
          <AlertTriangle className="h-4 w-4 text-amber-600 mt-0.5 shrink-0" />
          <div>
            <p className="text-sm font-medium text-amber-800 dark:text-amber-200">Ghi chú tiếp đón</p>
            <p className="text-sm text-amber-700 dark:text-amber-300">{patient.reception_note}</p>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
        <PatientAvatar
          patientId={id}
          avatarUrl={patient.avatar_url}
          fullName={patient.full_name}
          size="lg"
          editable
        />
        <div className="flex-1 min-w-0 space-y-1">
          <div className="flex items-center gap-2 flex-wrap">
            <h1 className="text-xl font-bold">{patient.full_name}</h1>
            {patient.bhyt_card_no && (
              <Badge variant={isBhytActive ? "default" : "secondary"}>
                BHYT {isBhytActive ? "còn hạn" : "hết hạn"}
              </Badge>
            )}
            <Badge variant="outline">{patient.status === "ACTIVE" ? "Hoạt động" : patient.status}</Badge>
          </div>
          <p className="text-muted-foreground text-sm">
            {patient.code}
            {patient.gender ? ` • ${GENDER_LABELS[patient.gender]}` : ""}
            {patient.age ? ` • ${patient.age} tuổi` : ""}
            {patient.date_of_birth ? ` (${formatDate(patient.date_of_birth)})` : ""}
          </p>
          <p className="text-sm text-muted-foreground">
            {patient.phone}
            {patient.phone && patient.email ? " • " : ""}
            {patient.email}
          </p>
          {patient.allergies_summary && (
            <p className="text-xs text-destructive font-medium">
              Dị ứng: {patient.allergies_summary}
            </p>
          )}
        </div>
      </div>

      <Separator />

      {/* Tab nav */}
      <div className="flex border-b overflow-x-auto gap-1 -mb-px">
        {TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            onClick={() => setActiveTab(tab.id)}
            className={cn(
              "px-4 py-2 text-sm font-medium border-b-2 whitespace-nowrap transition-colors",
              activeTab === tab.id
                ? "border-primary text-primary"
                : "border-transparent text-muted-foreground hover:text-foreground"
            )}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Tab content */}
      <div className="min-h-[300px]">
        {activeTab === "profile" && (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                Thông tin cá nhân
              </h3>
              {[
                { label: "Mã bệnh nhân", value: patient.code },
                { label: "Ngày sinh", value: formatDate(patient.date_of_birth) },
                { label: "Giới tính", value: patient.gender ? GENDER_LABELS[patient.gender] : "" },
                { label: "CMND/CCCD", value: patient.id_number },
                { label: "Nghề nghiệp", value: patient.occupation },
                { label: "Dân tộc", value: patient.ethnicity },
                { label: "Nhóm máu", value: patient.blood_type?.replace("_POS", "+").replace("_NEG", "-") },
              ].map(
                ({ label, value }) =>
                  value && (
                    <div key={label} className="flex justify-between text-sm">
                      <span className="text-muted-foreground">{label}</span>
                      <span className="font-medium">{value}</span>
                    </div>
                  )
              )}
              {patient.address?.street && (
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Địa chỉ</span>
                  <span className="font-medium text-right max-w-[60%]">{patient.address.street}</span>
                </div>
              )}
            </div>

            <div className="space-y-3">
              <h3 className="text-sm font-semibold text-muted-foreground uppercase tracking-wide">
                Ghi chú tiếp đón
              </h3>
              <div className="space-y-1">
                <textarea
                  className="w-full min-h-[120px] rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring resize-none"
                  placeholder="Nhập ghi chú tiếp đón cho bác sĩ..."
                  value={currentNote}
                  onChange={(e) => setNoteValue(e.target.value)}
                  onBlur={handleNoteBlur}
                  maxLength={2000}
                  aria-label="Ghi chú tiếp đón"
                />
                <p className="text-xs text-muted-foreground">
                  {noteSaving ? "Đang lưu..." : "Tự động lưu khi rời ô nhập"}
                </p>
              </div>

              <div className="pt-2">
                <p className="text-sm text-muted-foreground">
                  Cập nhật lúc: {formatDateTime(patient.updated_at)}
                </p>
              </div>
            </div>
          </div>
        )}

        {activeTab === "bhyt" && <BhytForm patientId={id} />}
        {activeTab === "allergy" && <AllergyList patientId={id} />}
        {activeTab === "emergency" && <EmergencyContactList patientId={id} />}
        {activeTab === "consent" && <ConsentList patientId={id} />}
        {activeTab === "cls" && <ClsUploadList patientId={id} />}
        {activeTab === "history" && <EncounterHistory patientId={id} />}
      </div>
    </div>
  );
}

function EncounterHistory({ patientId }: { patientId: string }) {
  const { data, isLoading } = usePatientEncounters(patientId);

  if (isLoading) {
    return <div className="space-y-2">{[1, 2, 3].map((i) => <Skeleton key={i} className="h-16 w-full" />)}</div>;
  }

  const encounters = data?.data ?? [];

  if (encounters.length === 0) {
    return (
      <div className="text-center py-12 text-muted-foreground">
        <p className="text-sm">Chưa có lịch sử khám</p>
      </div>
    );
  }

  return (
    <div className="space-y-2">
      {encounters.map((enc: { id: string; encounter_no: string; encounter_date: string; doctor_name?: string; room_name?: string; chief_complaint?: string; diagnosis_icd10?: string[]; status: string }) => (
        <div key={enc.id} className="border rounded-lg p-3 space-y-1">
          <div className="flex items-center justify-between">
            <span className="font-medium text-sm">
              {enc.encounter_no || `#${enc.id.slice(0, 8)}`}
              {enc.diagnosis_icd10 && enc.diagnosis_icd10.length > 0 && (
                <span className="ml-2 text-xs text-muted-foreground">[{enc.diagnosis_icd10.join(", ")}]</span>
              )}
            </span>
            <span className="text-xs text-muted-foreground">{formatDateTime(enc.encounter_date)}</span>
          </div>
          <div className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <span>{enc.doctor_name}{enc.room_name ? ` • ${enc.room_name}` : ""}</span>
            <span>•</span>
            <HisStatusBadge
              variant={
                enc.status === "DONE" || enc.status === "COMPLETED"
                  ? "done"
                  : enc.status === "WAITING"
                    ? "waiting"
                    : enc.status === "CANCELLED"
                      ? "critical"
                      : ("progress" as HisStatusVariant)
              }
            >
              {enc.status === "DONE" || enc.status === "COMPLETED"
                ? "Hoàn tất"
                : enc.status === "WAITING"
                  ? "Chờ khám"
                  : enc.status === "CANCELLED"
                    ? "Đã huỷ"
                    : enc.status === "IN_PROGRESS"
                      ? "Đang khám"
                      : enc.status}
            </HisStatusBadge>
          </div>
          {enc.chief_complaint && (
            <p className="text-xs line-clamp-1">{enc.chief_complaint}</p>
          )}
        </div>
      ))}
    </div>
  );
}
