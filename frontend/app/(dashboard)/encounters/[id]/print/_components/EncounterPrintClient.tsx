"use client";

import { useEncounter } from "@/lib/hooks/use-encounters";
import apiClient from "@/lib/api/client";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { Printer, Download, ArrowLeft } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";

const ENCOUNTER_TYPE_LABELS: Record<string, string> = {
  FIRST_VISIT: "Khám mới",
  FOLLOW_UP: "Tái khám",
  EMERGENCY: "Cấp cứu",
  CONSULTATION: "Hội chẩn",
};

const DIAGNOSIS_TYPE_LABELS: Record<string, string> = {
  PRIMARY: "Chính",
  SECONDARY: "Kèm theo",
};

interface Props {
  encounterId: string;
}

export default function EncounterPrintClient({ encounterId }: Props) {
  const { data: encounter, isLoading } = useEncounter(encounterId);

  if (isLoading) {
    return (
      <div className="p-6 space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-full" />
        <Skeleton className="h-4 w-3/4" />
        <Skeleton className="h-40 w-full" />
      </div>
    );
  }

  if (!encounter) {
    return (
      <div className="p-6 text-center">
        <p className="text-muted-foreground">Không tìm thấy lượt khám</p>
        <Link href="/encounters">
          <Button variant="outline" className="mt-4 gap-2">
            <ArrowLeft className="h-4 w-4" />
            Quay lại
          </Button>
        </Link>
      </div>
    );
  }

  const patient = encounter.patient_summary;
  const vital = encounter.vital_signs_latest as Record<string, number> | null | undefined;

  return (
    <>
      {/* Print controls — hidden when printing */}
      <div className="print:hidden flex items-center gap-3 px-6 py-4 border-b bg-background sticky top-0 z-10">
        <Link href={`/encounters/${encounterId}`}>
          <Button variant="outline" size="sm" className="gap-2">
            <ArrowLeft className="h-4 w-4" />
            Quay lại
          </Button>
        </Link>
        <Button
          size="sm"
          className="gap-2"
          onClick={() => window.print()}
        >
          <Printer className="h-4 w-4" />
          In phiếu khám
        </Button>
        <Button
          variant="outline"
          size="sm"
          className="gap-2"
          onClick={async () => {
            const { printPdfBlob } = await import("@/lib/utils/printPdfBlob");
            const url = `${apiClient.defaults.baseURL}/encounters/${encounterId}/emr/pdf`;
            try {
              await printPdfBlob(url);
            } catch {
              toast.error("Tải phiếu khám PDF thất bại");
            }
          }}
        >
          <Download className="h-4 w-4" />
          Tải PDF
        </Button>
      </div>

      {/* A4 print content */}
      <div className="print-page mx-auto max-w-[794px] p-[15mm] bg-white text-sm leading-relaxed print:p-0 print:max-w-none">
        {/* Clinic header */}
        <div className="text-center mb-6">
          <h1 className="text-xl font-bold uppercase tracking-wide">
            PHIẾU KHÁM BỆNH
          </h1>
          <p className="text-xs text-gray-500 mt-1">
            Mã lượt khám: {encounter.id}
          </p>
        </div>

        <hr className="border-gray-300 mb-5" />

        {/* Patient info */}
        <section className="mb-5">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-600 mb-2">
            Thông tin bệnh nhân
          </h2>
          <div className="grid grid-cols-2 gap-x-8 gap-y-1">
            <InfoRow label="Họ và tên" value={patient?.full_name} />
            <InfoRow label="Năm sinh" value={patient?.year_of_birth?.toString()} />
            <InfoRow label="Giới tính" value={patient?.gender} />
            <InfoRow label="Điện thoại" value={patient?.phone} />
          </div>
        </section>

        {/* Encounter info */}
        <section className="mb-5">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-600 mb-2">
            Thông tin lượt khám
          </h2>
          <div className="grid grid-cols-2 gap-x-8 gap-y-1">
            <InfoRow
              label="Ngày khám"
              value={
                encounter.started_at
                  ? format(new Date(encounter.started_at), "HH:mm, dd/MM/yyyy", { locale: vi })
                  : "—"
              }
            />
            <InfoRow label="Bác sĩ khám" value={encounter.doctor_name ?? "Chưa phân công"} />
            <InfoRow label="Phòng khám" value={encounter.room_name ?? "—"} />
            <InfoRow
              label="Loại khám"
              value={ENCOUNTER_TYPE_LABELS[encounter.encounter_type] ?? encounter.encounter_type}
            />
            <InfoRow label="Lý do khám" value={encounter.reason_for_visit} />
            {encounter.chief_complaint && (
              <InfoRow label="Lý do chính" value={encounter.chief_complaint} />
            )}
          </div>
        </section>

        {/* Vital signs */}
        {vital && Object.keys(vital).length > 0 && (
          <section className="mb-5">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-600 mb-2">
              Sinh hiệu
            </h2>
            <div className="grid grid-cols-3 gap-x-8 gap-y-1">
              {vital.heart_rate != null && (
                <InfoRow label="Mạch" value={`${vital.heart_rate} lần/phút`} />
              )}
              {vital.systolic_bp != null && vital.diastolic_bp != null && (
                <InfoRow label="Huyết áp" value={`${vital.systolic_bp}/${vital.diastolic_bp} mmHg`} />
              )}
              {vital.temperature != null && (
                <InfoRow label="Nhiệt độ" value={`${vital.temperature} °C`} />
              )}
              {vital.weight != null && (
                <InfoRow label="Cân nặng" value={`${vital.weight} kg`} />
              )}
              {vital.height != null && (
                <InfoRow label="Chiều cao" value={`${vital.height} cm`} />
              )}
              {vital.spo2 != null && (
                <InfoRow label="SpO2" value={`${vital.spo2}%`} />
              )}
            </div>
          </section>
        )}

        {/* Diagnoses */}
        {encounter.diagnoses && encounter.diagnoses.length > 0 && (
          <section className="mb-5">
            <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-600 mb-2">
              Chẩn đoán
            </h2>
            <table className="w-full text-sm border-collapse">
              <thead>
                <tr className="border-b border-gray-200">
                  <th className="text-left py-1 pr-4 font-medium text-gray-600 w-24">Loại</th>
                  <th className="text-left py-1 pr-4 font-medium text-gray-600 w-24">Mã ICD-10</th>
                  <th className="text-left py-1 font-medium text-gray-600">Tên bệnh</th>
                </tr>
              </thead>
              <tbody>
                {encounter.diagnoses.map((d) => (
                  <tr key={d.id} className="border-b border-gray-100">
                    <td className="py-1 pr-4 text-gray-600">
                      {DIAGNOSIS_TYPE_LABELS[d.type] ?? d.type}
                    </td>
                    <td className="py-1 pr-4 font-mono">{d.icd10_code}</td>
                    <td className="py-1">{d.name}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </section>
        )}

        {/* Footer */}
        <div className="mt-10 pt-4 border-t border-gray-200">
          <div className="flex justify-end">
            <div className="text-center">
              <p className="text-xs text-gray-500">
                In lúc {format(new Date(), "HH:mm, dd/MM/yyyy", { locale: vi })}
              </p>
              <p className="text-xs text-gray-400 mt-1">Pro-Diab HIS — Hệ thống quản lý phòng khám</p>
            </div>
          </div>
        </div>
      </div>

      <style>{`
        @media print {
          @page { size: A4 portrait; margin: 15mm; }
          body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
        }
      `}</style>
    </>
  );
}

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex gap-2 py-0.5">
      <span className="text-gray-500 min-w-[110px] flex-shrink-0">{label}:</span>
      <span className="font-medium">{value ?? "—"}</span>
    </div>
  );
}
