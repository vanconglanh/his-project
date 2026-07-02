import { type ReactNode } from "react";
import { ClinicLetterhead, type ClinicLetterheadProps } from "./ClinicLetterhead";
import { ReportBarcode } from "./ReportBarcode";

interface ReportMeta {
  fromDate: string;
  toDate: string;
  exportedBy: string;
  exportedAt: string;
  clinicName: string;
}

interface PrintableReportA4Props {
  title: string;
  reportCode: string;
  letterhead: ClinicLetterheadProps;
  meta: ReportMeta;
  children: ReactNode;
  signerLabel?: string;
}

/**
 * Khung báo cáo A4 dọc dùng chung cho Financial / Clinical / Pharmacy.
 * Layout: ClinicLetterhead → tiêu đề → barcode → meta block → nội dung → footer.
 */
export function PrintableReportA4({
  title,
  reportCode,
  letterhead,
  meta,
  children,
  signerLabel = "NGƯỜI LẬP BÁO CÁO",
}: PrintableReportA4Props) {
  const today = new Date();
  const day = String(today.getDate()).padStart(2, "0");
  const month = String(today.getMonth() + 1).padStart(2, "0");
  const year = today.getFullYear();

  return (
    <div className="w-[210mm] min-h-[297mm] bg-white mx-auto shadow-lg print:shadow-none p-[15mm] flex flex-col font-sans text-gray-800">
      {/* Header letterhead */}
      <ClinicLetterhead {...letterhead} />

      {/* Tiêu đề báo cáo */}
      <h1 className="text-center text-[14pt] font-bold uppercase tracking-wide mt-6 mb-2 text-gray-900">
        {title}
      </h1>

      {/* Barcode mã báo cáo */}
      <ReportBarcode code={reportCode} />

      {/* Meta block */}
      <div className="grid grid-cols-2 gap-x-6 gap-y-1 text-[10pt] text-gray-600 mt-3 mb-4">
        <div>
          <span className="font-semibold text-gray-800">Kỳ báo cáo:</span>{" "}
          {formatDate(meta.fromDate)} – {formatDate(meta.toDate)}
        </div>
        <div>
          <span className="font-semibold text-gray-800">Phòng khám:</span>{" "}
          {meta.clinicName}
        </div>
        <div>
          <span className="font-semibold text-gray-800">Người xuất:</span>{" "}
          {meta.exportedBy}
        </div>
        <div>
          <span className="font-semibold text-gray-800">Ngày xuất:</span>{" "}
          {formatDate(meta.exportedAt)}
        </div>
        <div className="col-span-2">
          <span className="font-semibold text-gray-800">Mã báo cáo:</span>{" "}
          <span className="font-mono">{reportCode}</span>
        </div>
      </div>

      <hr className="border-gray-300 mb-4" />

      {/* Nội dung bảng */}
      <div className="flex-1">{children}</div>

      {/* Footer */}
      <div className="mt-8 pt-4 border-t border-gray-200">
        <div className="flex justify-end">
          <div className="text-center min-w-[160px]">
            <p className="text-[10pt] text-gray-600 italic mb-1">
              Ngày {day} tháng {month} năm {year}
            </p>
            <p className="text-[11pt] font-bold uppercase text-gray-900">
              {signerLabel}
            </p>
            <p className="text-[9pt] italic text-gray-500 mt-0.5">
              (Ký, ghi rõ họ tên)
            </p>
            <div className="h-16" />
          </div>
        </div>
      </div>
    </div>
  );
}

function formatDate(dateStr: string): string {
  if (!dateStr) return "";
  const [year, month, day] = dateStr.slice(0, 10).split("-");
  if (!year || !month || !day) return dateStr;
  return `${day}/${month}/${year}`;
}
