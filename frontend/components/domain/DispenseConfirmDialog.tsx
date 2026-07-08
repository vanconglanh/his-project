"use client";

import { useState } from "react";
import { Sheet, SheetContent, SheetHeader, SheetTitle } from "@/components/ui/sheet";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { usePrescription } from "@/lib/hooks/use-prescriptions";
import { useDispensePrescription } from "@/lib/hooks/use-pharmacy-dispensing";
import { useWarehouses } from "@/lib/hooks/use-pharmacy-warehouse";
import { printDispenseReceipt } from "@/lib/api/pharmacy-dispensing";
import type { DispenseQueueItem } from "@/lib/api/pharmacy-dispensing";
import type { DispenseRequest } from "@/lib/api/pharmacy-dispensing";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Printer } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  item: DispenseQueueItem;
}

export function DispenseConfirmDialog({ open, onClose, item }: Props) {
  const { data: prescription } = usePrescription(item.prescription_id);
  const { data: warehouses } = useWarehouses();
  const dispense = useDispensePrescription();

  const [warehouseId, setWarehouseId] = useState("");
  const [note, setNote] = useState("");
  const [lastDispenseId, setLastDispenseId] = useState<string | null>(null);

  async function handleDispense() {
    if (!prescription || !warehouseId) return;

    const body: DispenseRequest = {
      warehouse_id: warehouseId,
      note: note || undefined,
      items: prescription.items.map((item) => ({
        prescription_item_id: item.id,
        batch_picks: [], // FEFO auto-pick by backend
      })),
    };

    const result = await dispense.mutateAsync({ prescriptionId: item.prescription_id, body });
    setLastDispenseId(result.id);
  }

  function handleClose() {
    setWarehouseId("");
    setNote("");
    setLastDispenseId(null);
    onClose();
  }

  return (
    <Sheet open={open} onOpenChange={handleClose}>
      <SheetContent className="w-full sm:max-w-lg overflow-y-auto px-6 pb-6">
        <SheetHeader className="px-0">
          <SheetTitle>Phát thuốc — {item.patient_name}</SheetTitle>
        </SheetHeader>

        {lastDispenseId ? (
          <div className="space-y-4">
            <div className="flex items-center gap-2 text-green-700 bg-green-50 rounded-md p-3 border border-green-200">
              <p className="text-sm font-medium">Phát thuốc thành công!</p>
            </div>
            <div className="flex justify-between gap-2">
              <Button
                variant="outline"
                onClick={() => printDispenseReceipt(lastDispenseId)}
              >
                <Printer className="h-4 w-4 mr-2" />
                In phiếu phát
              </Button>
              <Button onClick={handleClose}>Đóng</Button>
            </div>
          </div>
        ) : (
          <div className="space-y-4">
            {/* Drug list */}
            {prescription?.items && prescription.items.length > 0 && (
              <div className="space-y-2">
                <p className="text-sm font-medium">Danh sách thuốc (FEFO auto-pick):</p>
                <div className="rounded-md border divide-y">
                  {prescription.items.map((it) => (
                    <div key={it.id} className="flex items-center justify-between px-3 py-2 text-sm">
                      <div>
                        <span className="font-medium">{it.drug_name}</span>
                        <span className="text-muted-foreground ml-2">{it.dosage} · {it.frequency}</span>
                      </div>
                      <Badge variant="outline">{it.quantity} {it.unit}</Badge>
                    </div>
                  ))}
                </div>
              </div>
            )}

            {/* Warehouse select */}
            <div className="space-y-1">
              <Label htmlFor="warehouse">Kho phát thuốc</Label>
              <Select
                items={Object.fromEntries((warehouses ?? []).map((w) => [w.id, w.name]))}
                value={warehouseId}
                onValueChange={(v) => setWarehouseId(v ?? "")}
              >
                <SelectTrigger id="warehouse">
                  <SelectValue placeholder="-- Chọn kho --" />
                </SelectTrigger>
                <SelectContent>
                  {warehouses?.map((w) => (
                    <SelectItem key={w.id} value={w.id}>
                      {w.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-1">
              <Label htmlFor="note">Ghi chú</Label>
              <Input
                id="note"
                value={note}
                onChange={(e) => setNote(e.target.value)}
                placeholder="Ghi chú thêm..."
              />
            </div>

            <div className="flex justify-end gap-2">
              <Button variant="ghost" onClick={handleClose}>Hủy</Button>
              <Button
                onClick={handleDispense}
                disabled={!warehouseId || dispense.isPending}
              >
                {dispense.isPending ? "Đang phát..." : "Xác nhận phát thuốc"}
              </Button>
            </div>
          </div>
        )}
      </SheetContent>
    </Sheet>
  );
}
