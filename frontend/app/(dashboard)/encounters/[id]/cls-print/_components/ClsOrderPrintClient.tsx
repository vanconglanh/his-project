"use client";

import { useEncounter } from "@/lib/hooks/use-encounters";
import { useLabOrders, useRadOrders } from "@/lib/hooks/use-cls-orders";
import { useQuery } from "@tanstack/react-query";
import { getClinicLetterhead } from "@/lib/api/tenant-letterhead";
import { printLabOrdersPdf, printRadOrdersPdf } from "@/lib/api/cls-orders";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { Printer, ArrowLeft, FileText, ScanLine } from "lucide-react";
import Link from "next/link";
import { toast } from "sonner";

// Nhãn hiển thị tiếng Việt cho các enum của chỉ định CLS
const PRIORITY_LABELS: Record<string, string> = {
  NORMAL: "Thường",
  URGENT: "Ưu tiên",
  STAT: "Cấp cứu",
};

const MODALITY_LABELS: Record<string, string> = {
  XRAY: "X-quang",
  US: "Siêu âm",
  CT: "Chụp CT",
  MRI: "Chụp MRI",
  MAMMO: "Nhũ ảnh",
  ECG: "Điện tim",
  ENDO: "Nội soi",
};

const GENDER_LABELS: Record<string, string> = {
  MALE: "Nam",
  FEMALE: "Nữ",
  OTHER: "Khác",
};

interface OrderRow {
  kind: string;
  name: string;
  priority: string;
  department: string;
  note: string;
}

interface Props {
  encounterId: string;
}

export default function ClsOrderPrintClient({ encounterId }: Props) {
  const { data: encounter, isLoading } = useEncounter(encounterId);
  const { data: labOrders, isLoading: labLoading } = useLabOrders(encounterId);
  const { data: radOrders, isLoading: radLoading } = useRadOrders(encounterId);
  // Letterhead phòng khám (logo/tên/địa chỉ) — GET /tenants/me/letterhead (migration 0065)
  const { data: letterhead } = useQuery({
    queryKey: ["tenant", "letterhead"],
    queryFn: getClinicLetterhead,
    staleTime: 300_000,
  });

  if (isLoading || labLoading || radLoading) {
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
  const currentYear = new Date().getFullYear();
  const age =
    patient?.year_of_birth != null ? currentYear - patient.year_of_birth : null;

  // Chẩn đoán sơ bộ = danh sách chẩn đoán đã ghi trên lượt khám (ưu tiên bệnh chính lên trước)
  const diagnoses = [...(encounter.diagnoses ?? [])].sort((a, b) =>
    a.type === "PRIMARY" ? -1 : b.type === "PRIMARY" ? 1 : 0
  );
  const diagnosisText =
    diagnoses.map((d) => `${d.icd10_code} — ${d.name}`).join("; ") || "—";

  // Gộp chỉ định XN + CĐHA thành một bảng thống nhất
  const rows: OrderRow[] = [
    ...(labOrders ?? []).map<OrderRow>((o) => ({
      kind: "Xét nghiệm",
      name: o.test_name,
      priority: PRIORITY_LABELS[o.priority ?? "NORMAL"] ?? "Thường",
      department: "Khoa Xét nghiệm",
      note: [o.sample_type ? `Mẫu: ${o.sample_type}` : "", o.note ?? ""]
        .filter(Boolean)
        .join(" · "),
    })),
    ...(radOrders ?? []).map<OrderRow>((o) => ({
      kind: `CĐHA${o.modality ? ` · ${MODALITY_LABELS[o.modality] ?? o.modality}` : ""}`,
      name: o.procedure_name,
      priority: PRIORITY_LABELS[o.priority ?? "NORMAL"] ?? "Thường",
      department: "Khoa Chẩn đoán hình ảnh",
      note: [o.body_part ?? "", o.contrast ? "có thuốc cản quang" : "", o.note ?? ""]
        .filter(Boolean)
        .join(" · "),
    })),
  ];

  const orderedAt = encounter.started_at
    ? format(new Date(encounter.started_at), "HH:mm, dd/MM/yyyy", { locale: vi })
    : "—";

  return (
    <>
      {/* Thanh công cụ — ẩn khi in */}
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
          disabled={!labOrders || labOrders.length === 0}
          onClick={() =>
            printLabOrdersPdf(encounterId).catch(() =>
              toast.error("In phiếu chỉ định xét nghiệm thất bại")
            )
          }
        >
          <FileText className="h-4 w-4" />
          In phiếu chỉ định XN (PDF)
        </Button>
        <Button
          size="sm"
          className="gap-2"
          disabled={!radOrders || radOrders.length === 0}
          onClick={() =>
            printRadOrdersPdf(encounterId).catch(() =>
              toast.error("In phiếu chỉ định CĐHA thất bại")
            )
          }
        >
          <ScanLine className="h-4 w-4" />
          In phiếu chỉ định CĐHA (PDF)
        </Button>
        <Button variant="outline" size="sm" className="gap-2" onClick={() => window.print()}>
          <Printer className="h-4 w-4" />
          In gộp (HTML)
        </Button>
      </div>

      {/* Nội dung in khổ A4 */}
      <div className="print-page mx-auto max-w-[794px] p-[15mm] bg-white text-sm leading-relaxed print:p-0 print:max-w-none">
        {/* Letterhead lấy từ cấu hình tenant (GET /tenants/me/letterhead) */}
        <div className="flex items-start justify-between mb-3">
          <div className="text-xs leading-tight">
            <p className="font-bold uppercase">{letterhead?.clinic_name ?? "Phòng khám đa khoa Pro-Diab"}</p>
            {letterhead?.company_name && <p className="text-gray-600">{letterhead.company_name}</p>}
            {letterhead?.cskcb_code && <p className="text-gray-600">Mã CSKCB: {letterhead.cskcb_code}</p>}
            {letterhead?.address && <p className="text-gray-600">{letterhead.address}</p>}
            {letterhead?.phone && <p className="text-gray-600">ĐT: {letterhead.phone}</p>}
            {!letterhead && <p className="text-gray-600">Hệ thống quản lý phòng khám</p>}
          </div>
          <div className="text-right text-xs text-gray-500">
            <p>Mã lượt khám: {encounter.id}</p>
            <p>Ngày chỉ định: {orderedAt}</p>
          </div>
        </div>

        <div className="text-center mb-5">
          <h1 className="text-xl font-bold uppercase tracking-wide">
            Phiếu chỉ định cận lâm sàng
          </h1>
        </div>

        <hr className="border-gray-300 mb-5" />

        {/* Thông tin bệnh nhân */}
        <section className="mb-5">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-600 mb-2">
            Thông tin bệnh nhân
          </h2>
          <div className="grid grid-cols-2 gap-x-8 gap-y-1">
            <InfoRow label="Họ và tên" value={patient?.full_name} />
            <InfoRow
              label="Năm sinh"
              value={
                patient?.year_of_birth != null
                  ? `${patient.year_of_birth}${age != null ? ` (${age} tuổi)` : ""}`
                  : undefined
              }
            />
            <InfoRow
              label="Giới tính"
              value={patient?.gender ? GENDER_LABELS[patient.gender] ?? patient.gender : undefined}
            />
            <InfoRow label="Điện thoại" value={patient?.phone} />
            <InfoRow label="Bác sĩ chỉ định" value={encounter.doctor_name ?? "Chưa phân công"} />
            <InfoRow label="Phòng khám" value={encounter.room_name ?? "—"} />
          </div>
          <div className="mt-2">
            <InfoRow label="Chẩn đoán sơ bộ" value={diagnosisText} />
          </div>
        </section>

        {/* Bảng chỉ định */}
        <section className="mb-6">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-gray-600 mb-2">
            Danh mục chỉ định
          </h2>
          {rows.length === 0 ? (
            <p className="text-sm text-gray-500 italic">
              Chưa có chỉ định cận lâm sàng nào cho lượt khám này.
            </p>
          ) : (
            <table className="w-full text-sm border-collapse">
              <thead>
                <tr className="border-y border-gray-300 bg-gray-50">
                  <th className="text-left py-1.5 px-2 font-medium text-gray-600 w-8">STT</th>
                  <th className="text-left py-1.5 px-2 font-medium text-gray-600">Tên dịch vụ</th>
                  <th className="text-left py-1.5 px-2 font-medium text-gray-600 w-32">Loại</th>
                  <th className="text-left py-1.5 px-2 font-medium text-gray-600 w-24">Mức độ</th>
                  <th className="text-left py-1.5 px-2 font-medium text-gray-600 w-40">Khoa/Phòng</th>
                </tr>
              </thead>
              <tbody>
                {rows.map((r, i) => (
                  <tr key={i} className="border-b border-gray-200 align-top">
                    <td className="py-1.5 px-2 text-gray-600">{i + 1}</td>
                    <td className="py-1.5 px-2">
                      <span className="font-medium">{r.name}</span>
                      {r.note && <span className="block text-xs text-gray-500">{r.note}</span>}
                    </td>
                    <td className="py-1.5 px-2 text-gray-600">{r.kind}</td>
                    <td className="py-1.5 px-2">{r.priority}</td>
                    <td className="py-1.5 px-2 text-gray-600">{r.department}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        {/* Chữ ký bác sĩ chỉ định */}
        <div className="flex justify-end mb-8">
          <div className="text-center text-sm w-64">
            <p className="italic text-gray-600">
              Ngày {format(new Date(), "dd 'tháng' MM 'năm' yyyy", { locale: vi })}
            </p>
            <p className="font-semibold uppercase mt-1">Bác sĩ chỉ định</p>
            <p className="italic text-xs text-gray-500">(Ký, ghi rõ họ tên)</p>
            <p className="font-semibold mt-14">{encounter.doctor_name ?? ""}</p>
          </div>
        </div>

        {/* Phần dành cho phòng CLS */}
        <div className="border border-gray-300 rounded-md p-3 text-sm">
          <p className="font-semibold uppercase text-gray-600 mb-2 text-xs tracking-wide">
            Phần dành cho phòng cận lâm sàng
          </p>
          <div className="grid grid-cols-3 gap-x-6 gap-y-3">
            <FillRow label="Người nhận/lấy mẫu" />
            <FillRow label="Thời gian nhận" />
            <FillRow label="Dự kiến trả kết quả" />
          </div>
        </div>

        <p className="text-xs text-gray-500 italic mt-3">
          Phiếu có giá trị trong ngày. Đề nghị bệnh nhân mang phiếu đến đúng khoa/phòng được chỉ định.
        </p>

        {/* Footer */}
        <div className="mt-8 pt-3 border-t border-gray-200 flex justify-end">
          <div className="text-center">
            <p className="text-xs text-gray-500">
              In lúc {format(new Date(), "HH:mm, dd/MM/yyyy", { locale: vi })}
            </p>
            <p className="text-xs text-gray-400 mt-1">Pro-Diab HIS — Hệ thống quản lý phòng khám</p>
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
      <span className="text-gray-500 min-w-[120px] flex-shrink-0">{label}:</span>
      <span className="font-medium">{value ?? "—"}</span>
    </div>
  );
}

function FillRow({ label }: { label: string }) {
  return (
    <div>
      <span className="text-xs text-gray-500">{label}:</span>
      <div className="mt-6 border-b border-dotted border-gray-400" />
    </div>
  );
}
