"use client";

import { useEffect, useState, use } from "react";
import { PrintableReportA4 } from "@/components/print/PrintableReportA4";
import { getClinicLetterhead, type ClinicLetterheadData } from "@/lib/api/tenant-letterhead";
import {
  getTopDoctorsReport,
  getTopDiagnoses,
  getTopPharmacyDrugs,
  reserveReportCode,
} from "@/lib/api/reports";
import { FinancialPrintTable } from "./FinancialPrintTable";
import { ClinicalPrintTable } from "./ClinicalPrintTable";
import { PharmacyPrintTable } from "./PharmacyPrintTable";
import { PrintToolbar } from "./PrintToolbar";
import { ErrorCard } from "./ErrorCard";

type ReportType = "financial" | "clinical" | "pharmacy";

const REPORT_META: Record<ReportType, { title: string }> = {
  financial: { title: "BÁO CÁO DOANH THU" },
  clinical: { title: "BÁO CÁO LƯỢT KHÁM" },
  pharmacy: { title: "BÁO CÁO TỒN KHO DƯỢC" },
};

interface Props {
  paramsPromise: Promise<{ type: string }>;
  searchParamsPromise: Promise<{ from?: string; to?: string; clinicId?: string }>;
}

export default function ReportPrintClient({ paramsPromise, searchParamsPromise }: Props) {
  const { type } = use(paramsPromise);
  const { from, to, clinicId } = use(searchParamsPromise);

  const reportType = type as ReportType;
  const fromDate = from ?? new Date(Date.now() - 30 * 86400_000).toISOString().slice(0, 10);
  const toDate = to ?? new Date().toISOString().slice(0, 10);

  const [loading, setLoading] = useState(true);
  const [letterheadErr, setLetterheadErr] = useState(false);
  const [dataErr, setDataErr] = useState(false);
  const [letterhead, setLetterhead] = useState<ClinicLetterheadData | null>(null);
  const [reportCode, setReportCode] = useState("");
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const [rows, setRows] = useState<any[]>([]);

  useEffect(() => {
    if (!["financial", "clinical", "pharmacy"].includes(type)) return;
    let cancelled = false;
    (async () => {
      try {
        const lh = await getClinicLetterhead();
        if (cancelled) return;
        setLetterhead(lh);
      } catch {
        if (cancelled) return;
        setLetterheadErr(true);
        setLoading(false);
        return;
      }
      try {
        const [code, data] = await Promise.all([
          reserveReportCode(reportType).catch(() => ""),
          reportType === "financial"
            ? getTopDoctorsReport(fromDate, toDate)
            : reportType === "clinical"
            ? getTopDiagnoses(fromDate, toDate)
            : getTopPharmacyDrugs(fromDate, toDate),
        ]);
        if (cancelled) return;
        setReportCode(
          code ||
            `RPT-${reportType.slice(0, 3).toUpperCase()}-${new Date()
              .toISOString()
              .slice(0, 10)
              .replace(/-/g, "")}-0001`
        );
        setRows(data ?? []);
      } catch {
        if (cancelled) return;
        setDataErr(true);
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();
    return () => {
      cancelled = true;
    };
  }, [type, reportType, fromDate, toDate]);

  if (!["financial", "clinical", "pharmacy"].includes(type)) {
    return (
      <ErrorCard
        title="Loại báo cáo không hợp lệ"
        description="Chỉ hỗ trợ financial / clinical / pharmacy."
      />
    );
  }

  if (loading) {
    return (
      <div className="p-12 text-center text-sm text-muted-foreground">
        Đang tải báo cáo...
      </div>
    );
  }

  if (letterheadErr || !letterhead) {
    return (
      <ErrorCard
        title="Không tải được thông tin phòng khám"
        description="Vui lòng liên hệ quản trị viên hệ thống."
      />
    );
  }

  const meta = REPORT_META[reportType];
  const reportMeta = {
    fromDate,
    toDate,
    exportedBy: "Người dùng hiện tại",
    exportedAt: new Date().toISOString().slice(0, 10),
    clinicName: letterhead.clinic_name,
  };

  return (
    <>
      <PrintToolbar
        pdfPath={`/reports/${reportType}/pdf?from=${fromDate}&to=${toDate}${
          reportCode ? `&reportCode=${reportCode}` : ""
        }${clinicId ? `&clinicId=${clinicId}` : ""}`}
        fileName={`${reportCode || `bao-cao-${reportType}`}.pdf`}
      />
      <div className="bg-gray-100 min-h-screen py-8 print:bg-white print:py-0">
        <PrintableReportA4
          title={meta.title}
          reportCode={reportCode}
          letterhead={{
            clinicName: letterhead.clinic_name,
            companyName: letterhead.company_name,
            cskcbCode: letterhead.cskcb_code,
            address: letterhead.address,
            phone: letterhead.phone,
            email: letterhead.email,
            logoUrl: letterhead.logo_url,
          }}
          meta={reportMeta}
        >
          {dataErr ? (
            <div className="py-12 text-center text-sm text-gray-600">
              Không tải được dữ liệu báo cáo. Vui lòng thử lại.
            </div>
          ) : (
            <>
              {reportType === "financial" && <FinancialPrintTable rows={rows} />}
              {reportType === "clinical" && <ClinicalPrintTable rows={rows} />}
              {reportType === "pharmacy" && <PharmacyPrintTable rows={rows} />}
            </>
          )}
        </PrintableReportA4>
      </div>
    </>
  );
}
