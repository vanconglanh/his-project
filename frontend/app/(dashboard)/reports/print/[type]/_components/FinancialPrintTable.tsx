import type { DoctorKpi } from "@/lib/api/reports";

interface FinancialPrintTableProps {
  rows: DoctorKpi[];
}

const vnd = (n: number | null | undefined) =>
  (typeof n === "number" && Number.isFinite(n) ? n : 0).toLocaleString("vi-VN");

const TH = "bg-teal-50 text-gray-900 font-semibold text-[11pt] py-2 px-3 border border-gray-300";
const TD = "py-1.5 px-3 text-[11pt] border border-gray-300";

/**
 * Bảng doanh thu theo bác sĩ dùng cho báo cáo in Financial.
 * Cột: STT (5%) / Bác sĩ (15%) / Lượt khám (25%) / Doanh thu (35%) / TB/lượt (20%).
 */
export function FinancialPrintTable({ rows }: FinancialPrintTableProps) {
  const total = rows.reduce((s, r) => s + r.revenue, 0);

  if (rows.length === 0) {
    return (
      <p className="text-center text-gray-500 text-[11pt] py-8">
        Không có dữ liệu trong khoảng thời gian đã chọn.
      </p>
    );
  }

  return (
    <table className="w-full border-collapse text-left" role="table">
      <colgroup>
        <col style={{ width: "5%" }} />
        <col style={{ width: "15%" }} />
        <col style={{ width: "25%" }} />
        <col style={{ width: "35%" }} />
        <col style={{ width: "20%" }} />
      </colgroup>
      <thead>
        <tr>
          <th scope="col" className={TH}>STT</th>
          <th scope="col" className={TH}>Số HĐ / Mã BS</th>
          <th scope="col" className={TH}>Bác sĩ</th>
          <th scope="col" className={`${TH} text-right`}>Doanh thu (₫)</th>
          <th scope="col" className={`${TH} text-right`}>TB/lượt (₫)</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row, idx) => (
          <tr key={row.doctor_id ?? `row-${idx}`} className={idx % 2 === 1 ? "bg-gray-50" : ""}>
            <td className={`${TD} text-center`}>{idx + 1}</td>
            <td className={`${TD} font-mono text-[10pt]`}>{row.doctor_id}</td>
            <td className={TD}>{row.name}</td>
            <td className={`${TD} font-mono text-right`}>{vnd(row.revenue)}</td>
            <td className={`${TD} font-mono text-right`}>{vnd(row.avg_revenue_per_encounter)}</td>
          </tr>
        ))}
      </tbody>
      <tfoot>
        <tr className="border-t-2 border-gray-400 bg-teal-50">
          <td colSpan={3} className={`${TD} font-bold`}>Tổng cộng:</td>
          <td className={`${TD} font-mono font-bold text-right`}>{vnd(total)}</td>
          <td className={TD} />
        </tr>
      </tfoot>
    </table>
  );
}
