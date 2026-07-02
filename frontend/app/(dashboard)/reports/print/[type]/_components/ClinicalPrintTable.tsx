import type { TopDiagnosis } from "@/lib/api/reports";

interface ClinicalPrintTableProps {
  rows: TopDiagnosis[];
}

const TH = "bg-teal-50 text-gray-900 font-semibold text-[11pt] py-2 px-3 border border-gray-300";
const TD = "py-1.5 px-3 text-[11pt] border border-gray-300";

/**
 * Bảng lượt khám / chẩn đoán dùng cho báo cáo in Clinical.
 * Cột: STT (5%) / Mã ICD-10 (25%) / Tên chẩn đoán (20%) / Bác sĩ (20%) / Lượt khám (15%) + % (15%).
 */
export function ClinicalPrintTable({ rows }: ClinicalPrintTableProps) {
  const total = rows.reduce((s, r) => s + r.count, 0);

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
        <col style={{ width: "25%" }} />
        <col style={{ width: "20%" }} />
        <col style={{ width: "20%" }} />
        <col style={{ width: "15%" }} />
        <col style={{ width: "15%" }} />
      </colgroup>
      <thead>
        <tr>
          <th scope="col" className={TH}>STT</th>
          <th scope="col" className={TH}>Bệnh nhân / Mã ICD</th>
          <th scope="col" className={TH}>Chẩn đoán</th>
          <th scope="col" className={TH}>Bác sĩ</th>
          <th scope="col" className={`${TH} text-right`}>Ngày khám</th>
          <th scope="col" className={`${TH} text-right`}>Lượt</th>
        </tr>
      </thead>
      <tbody>
        {rows.map((row, idx) => (
          <tr key={row.icd10_code ?? `row-${idx}`} className={idx % 2 === 1 ? "bg-gray-50" : ""}>
            <td className={`${TD} text-center`}>{idx + 1}</td>
            <td className={`${TD} font-mono text-[10pt]`}>{row.icd10_code}</td>
            <td className={TD}>{row.icd10_name}</td>
            <td className={TD}>—</td>
            <td className={`${TD} text-right`}>—</td>
            <td className={`${TD} font-mono text-right`}>{row.count}</td>
          </tr>
        ))}
      </tbody>
      <tfoot>
        <tr className="border-t-2 border-gray-400 bg-teal-50">
          <td colSpan={5} className={`${TD} font-bold`}>Tổng lượt:</td>
          <td className={`${TD} font-mono font-bold text-right`}>{total}</td>
        </tr>
      </tfoot>
    </table>
  );
}
