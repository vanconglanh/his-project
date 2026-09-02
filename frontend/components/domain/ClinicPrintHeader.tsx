"use client";

/**
 * Header chuẩn diaB dùng chung cho MỌI trang in HTML (encounter print, cls-print...).
 *
 * Đồng bộ 1:1 với header PDF backend (ReportPdfCommon.RenderLetterhead — nền teal
 * Brand #01645A, logo trắng bên trái, thông tin phòng khám bên phải chữ trắng) để
 * phiếu in HTML và PDF trông cùng một bộ nhận diện diaB, thay vì mỗi nơi tự vẽ
 * header khác nhau (trước đây: encounter/print không có header nào, cls-print chỉ
 * có text đen không logo, không màu thương hiệu).
 *
 * Dữ liệu lấy từ GET /tenants/me/letterhead (lib/api/tenant-letterhead.ts) — khi
 * chưa tải xong hoặc tenant chưa cấu hình, fallback chữ "diaB" + tên mặc định.
 */
import type { ClinicLetterheadData } from "@/lib/api/tenant-letterhead";

interface Props {
  letterhead?: ClinicLetterheadData | null;
  /** Cột phải tuỳ trang (vd "Mã lượt khám: ...", "Ngày chỉ định: ..."). */
  meta?: React.ReactNode;
}

export function ClinicPrintHeader({ letterhead, meta }: Props) {
  return (
    <div
      className="flex items-center gap-3 rounded-t-sm px-4 py-3 print:rounded-none"
      style={{ background: "var(--print-header)" }}
    >
      {/* Logo — chip trắng để logo teal không chìm trên nền teal, giống PDF backend */}
      <div className="flex h-11 w-14 shrink-0 items-center justify-center rounded-sm bg-white p-1">
        {/* eslint-disable-next-line @next/next/no-img-element */}
        <img src="/brand/diab-logo.svg" alt="diaB" className="h-full w-full object-contain" />
      </div>

      <div className="min-w-0 flex-1" style={{ color: "var(--print-header-foreground)" }}>
        <p className="truncate text-sm font-bold uppercase tracking-wide">
          {letterhead?.clinic_name || "Phòng khám đa khoa diaB"}
        </p>
        {letterhead?.company_name && (
          <p className="truncate text-xs font-semibold opacity-90">{letterhead.company_name}</p>
        )}
        <p className="mt-0.5 truncate text-[11px] opacity-80">
          {[
            letterhead?.cskcb_code ? `Mã CSKCB: ${letterhead.cskcb_code}` : null,
            letterhead?.address || null,
          ]
            .filter(Boolean)
            .join(" · ") || "Hệ thống quản lý phòng khám"}
        </p>
        {(letterhead?.phone || letterhead?.email) && (
          <p className="truncate text-[11px] opacity-80">
            {[letterhead?.phone ? `ĐT: ${letterhead.phone}` : null, letterhead?.email || null]
              .filter(Boolean)
              .join(" · ")}
          </p>
        )}
      </div>

      {meta && (
        <div
          className="shrink-0 text-right text-[11px] leading-relaxed opacity-90"
          style={{ color: "var(--print-header-foreground)" }}
        >
          {meta}
        </div>
      )}
    </div>
  );
}
