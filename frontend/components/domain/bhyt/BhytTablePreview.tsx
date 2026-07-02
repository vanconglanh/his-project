"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useBhytExportItems } from "@/lib/hooks/use-bhyt-export";
import { Button } from "@/components/ui/button";
import { getBhytXmlDownloadUrl } from "@/lib/api/bhyt-export";

const TABLE_LABELS: Record<number, string> = {
  1: "Bảng 1 - Đợt KCB",
  2: "Bảng 2 - Thuốc",
  3: "Bảng 3 - CLS",
  4: "Bảng 4 - DVKT cao",
  5: "Bảng 5 - Chi phí",
};

// Columns displayed per table
const TABLE_COLUMNS: Record<number, { key: string; label: string }[]> = {
  1: [
    { key: "ma_lien_ket", label: "Mã liên kết" },
    { key: "ho_ten", label: "Họ tên" },
    { key: "ma_the_bhyt", label: "Thẻ BHYT" },
    { key: "ma_benh", label: "ICD-10" },
    { key: "t_tongchi", label: "Tổng chi" },
    { key: "t_bhtt", label: "BHYT trả" },
  ],
  2: [
    { key: "ma_lien_ket", label: "Mã liên kết" },
    { key: "ten_thuoc", label: "Tên thuốc" },
    { key: "so_luong", label: "SL" },
    { key: "don_gia", label: "Đơn giá" },
    { key: "thanh_tien", label: "Thành tiền" },
    { key: "t_bhtt", label: "BHYT trả" },
  ],
  3: [
    { key: "ma_lien_ket", label: "Mã liên kết" },
    { key: "ma_dich_vu", label: "Mã DV" },
    { key: "don_vi_tinh", label: "ĐVT" },
    { key: "so_luong", label: "SL" },
    { key: "thanh_tien", label: "Thành tiền" },
    { key: "t_bhtt", label: "BHYT trả" },
  ],
  4: [
    { key: "ma_lien_ket", label: "Mã liên kết" },
    { key: "ma_dich_vu", label: "Mã DVKT" },
    { key: "ten_dich_vu", label: "Tên DVKT" },
    { key: "so_luong", label: "SL" },
    { key: "thanh_tien", label: "Thành tiền" },
    { key: "t_bhtt", label: "BHYT trả" },
  ],
  5: [
    { key: "ma_lien_ket", label: "Mã liên kết" },
    { key: "ten_chi_phi", label: "Tên chi phí" },
    { key: "nhom_chi_phi", label: "Nhóm" },
    { key: "thanh_tien", label: "Thành tiền" },
    { key: "t_bhtt", label: "BHYT trả" },
  ],
};

function TableTab({ exportId, tableNo }: { exportId: string; tableNo: number }) {
  const { data, isLoading } = useBhytExportItems(exportId, tableNo, { page: 1, page_size: 50 });
  const columns = TABLE_COLUMNS[tableNo] ?? [];

  if (isLoading) {
    return (
      <div className="space-y-2 p-2">
        {Array.from({ length: 5 }).map((_, i) => (
          <Skeleton key={i} className="h-9 w-full" />
        ))}
      </div>
    );
  }

  const rows = data?.data ?? [];

  return (
    <div>
      <div className="flex items-center justify-between mb-2 px-1">
        <p className="text-xs text-muted-foreground">{data?.meta?.total ?? rows.length} dòng</p>
        <Button
          variant="outline"
          size="sm"
          className="h-7 text-xs"
          render={<a href={getBhytXmlDownloadUrl(exportId, tableNo)} target="_blank" rel="noopener noreferrer" />}
        >
          Download XML Bảng {tableNo}
        </Button>
      </div>

      {rows.length === 0 ? (
        <p className="py-8 text-center text-sm text-muted-foreground">Chưa có dữ liệu</p>
      ) : (
        <div className="overflow-x-auto rounded-md border">
          <Table>
            <TableHeader>
              <TableRow>
                {columns.map((col) => (
                  <TableHead key={col.key} className="text-xs whitespace-nowrap">
                    {col.label}
                  </TableHead>
                ))}
              </TableRow>
            </TableHeader>
            <TableBody>
              {rows.map((row) => (
                <TableRow key={row.id}>
                  {columns.map((col) => {
                    const val = (row.row_data_json as Record<string, unknown>)[col.key];
                    return (
                      <TableCell key={col.key} className="text-xs py-2 whitespace-nowrap">
                        {typeof val === "number"
                          ? new Intl.NumberFormat("vi-VN").format(val)
                          : String(val ?? "")}
                      </TableCell>
                    );
                  })}
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>
      )}
    </div>
  );
}

interface Props {
  exportId: string;
}

export function BhytTablePreview({ exportId }: Props) {
  const [activeTab, setActiveTab] = useState("1");

  return (
    <Tabs value={activeTab} onValueChange={setActiveTab}>
      <TabsList className="flex-wrap h-auto gap-1">
        {[1, 2, 3, 4, 5].map((n) => (
          <TabsTrigger key={n} value={String(n)} className="text-xs">
            {TABLE_LABELS[n]}
          </TabsTrigger>
        ))}
      </TabsList>

      {[1, 2, 3, 4, 5].map((n) => (
        <TabsContent key={n} value={String(n)} className="mt-3">
          <TableTab exportId={exportId} tableNo={n} />
        </TabsContent>
      ))}
    </Tabs>
  );
}
