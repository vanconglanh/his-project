import type { TopDrug } from "@/lib/api/reports";

interface PharmacyPrintTableProps {
  rows: TopDrug[];
}

const vnd = (n: number | null | undefined) =>
  (typeof n === "number" && Number.isFinite(n) ? n : 0).toLocaleString("vi-VN");

const TH = "bg-teal-50 text-gray-900 font-semibold text-[11pt] py-2 px-3 border border-gray-300";
const TD = "py-1.5 px-3 text-[11pt] border border-gray-300";

/**
 * Bảng tồn kho / thuốc bán chạy dùng cho báo cáo in Pharmacy.
 * Cột: Mã thuốc (12%) / Tên thuốc (30%) / Lô (12%) / HSD (14%) / Tồn (10%) / Đơn vị (12%) / Doanh thu (10%).
 * Không có summary row theo design spec.
 */
export function PharmacyPrintTable({ rows }: PharmacyPrintTableProps) {
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
        <col style={{ width: "12%" }} />
        <col style={{ width: "30%" }} />
        <col style={{ width: "12%" }} />
        <col style={{ width: "14%" }} />
        <col style={{ width: "10%" }} />
        <col style={{ width: "12%" }} />
        <col style={{ width: "10%" }} />
      </colgroup>
      <thead>
        <tr>
          <th scope="col" className={TH}>Mã thuốc</th>
          <th scope="col" className={TH}>Tên thuốc</th>
          <th scope="col" className={TH}>Lô</th>
          <th scope="col" className={TH}>HSD</th>
          <th scope="col" className={`${TH} text-right`}>Tồn</th>
          <th scope="col" className={TH}>Đơn vị</th>
          <th scope="col" className={`${TH} text-right`}>Doanh thu (₫)</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row, idx) => (
          <tr key={row.drug_id ?? `row-${idx}`} className={idx % 2 === 1 ? "bg-gray-50" : ""}>
            <td className={`${TD} font-mono text-[10pt]`}>{row.drug_code}</td>
            <td className={TD}>{row.drug_name}</td>
            <td className={`${TD} font-mono`}>—</td>
            <td className={`${TD} font-mono`}>—</td>
            <td className={`${TD} font-mono text-right`}>{row.quantity_sold}</td>
            <td className={TD}>Viên</td>
            <td className={`${TD} font-mono text-right`}>{vnd(row.revenue)}</td>
          </tr>
        ))}
      </tbody>
    </table>
  );
}
