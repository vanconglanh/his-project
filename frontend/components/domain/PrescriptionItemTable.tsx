"use client";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Trash2 } from "lucide-react";
import type { PrescriptionItemResponse } from "@/lib/api/prescriptions";

const routeLabel: Record<string, string> = {
  ORAL: "Uống",
  IV: "IV",
  IM: "IM",
  SC: "SC",
  TOP: "Bôi",
  INH: "Hít",
  OPH: "Nhỏ mắt",
  OTIC: "Nhỏ tai",
  NAS: "Nhỏ mũi",
  REC: "Đặt HM",
  OTHER: "Khác",
};

interface Props {
  items: PrescriptionItemResponse[];
  canEdit?: boolean;
  onRemove?: (itemId: string) => void;
  removingId?: string;
}

export function PrescriptionItemTable({ items, canEdit, onRemove, removingId }: Props) {
  if (items.length === 0) {
    return (
      <div className="flex flex-col items-center py-10 text-muted-foreground text-sm gap-2">
        <p>Chưa có thuốc trong đơn</p>
      </div>
    );
  }

  return (
    <div className="rounded-md border overflow-x-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead className="w-8">#</TableHead>
            <TableHead>Tên thuốc</TableHead>
            <TableHead>Liều</TableHead>
            <TableHead>Tần suất</TableHead>
            <TableHead>Đường</TableHead>
            <TableHead className="text-center">Ngày</TableHead>
            <TableHead className="text-center">SL</TableHead>
            <TableHead>Hướng dẫn</TableHead>
            {canEdit && <TableHead className="w-10" />}
          </TableRow>
        </TableHeader>
        <TableBody>
          {items.map((item, idx) => (
            <TableRow key={item.id}>
              <TableCell className="text-muted-foreground">{idx + 1}</TableCell>
              <TableCell>
                <div>
                  <p className="font-medium text-sm">{item.drug_name}</p>
                  {item.strength && (
                    <p className="text-xs text-muted-foreground">{item.strength}</p>
                  )}
                </div>
              </TableCell>
              <TableCell className="text-sm">{item.dosage}</TableCell>
              <TableCell className="text-sm">{item.frequency}</TableCell>
              <TableCell>
                <Badge variant="outline" className="text-xs">
                  {routeLabel[item.route] ?? item.route}
                </Badge>
              </TableCell>
              <TableCell className="text-center text-sm">{item.duration_days}</TableCell>
              <TableCell className="text-center text-sm">
                {item.quantity} {item.unit}
              </TableCell>
              <TableCell className="text-xs text-muted-foreground max-w-[160px] truncate">
                {item.instructions || "-"}
              </TableCell>
              {canEdit && (
                <TableCell>
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8 text-destructive hover:text-destructive"
                    onClick={() => onRemove?.(item.id)}
                    disabled={removingId === item.id}
                    aria-label={`Xóa ${item.drug_name}`}
                  >
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </TableCell>
              )}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
