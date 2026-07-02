"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { DrugAutocomplete } from "./DrugAutocomplete";
import { PrescriptionItemForm } from "./PrescriptionItemForm";
import { PrescriptionItemTable } from "./PrescriptionItemTable";
import { DdiWarningPanel } from "./DdiWarningPanel";
import { SignPrescriptionWizard } from "./SignPrescriptionWizard";
import { ConfirmDialog } from "./ConfirmDialog";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  usePrescription,
  useCreatePrescription,
  useAddPrescriptionItems,
  useRemovePrescriptionItem,
  useCancelPrescription,
  useDdiCheck,
} from "@/lib/hooks/use-prescriptions";
import type { DrugMasterResponse } from "@/lib/api/drugs";
import type { PrescriptionItemRequest } from "@/lib/api/prescriptions";
import { PenTool, Save, XCircle, AlertTriangle } from "lucide-react";

const STATUS_LABELS: Record<string, string> = {
  DRAFT: "Nháp",
  SIGNED: "Đã ký",
  SUBMITTED_DTQG: "Đã gửi ĐTQG",
  DISPENSED: "Đã phát",
  PARTIAL_DISPENSED: "Phát một phần",
  CANCELLED: "Đã hủy",
};

const STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  DRAFT: "secondary",
  SIGNED: "default",
  SUBMITTED_DTQG: "default",
  DISPENSED: "default",
  PARTIAL_DISPENSED: "outline",
  CANCELLED: "destructive",
};

interface Props {
  encounterId: string;
  patientId: string;
  existingPrescriptionId?: string;
}

export function PrescriptionForm({ encounterId, patientId, existingPrescriptionId }: Props) {
  const [selectedDrug, setSelectedDrug] = useState<DrugMasterResponse | null>(null);
  const [signWizardOpen, setSignWizardOpen] = useState(false);
  const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
  const [removingItemId, setRemovingItemId] = useState<string | undefined>();

  const createPrescription = useCreatePrescription();

  // Use existing or create
  const [prescriptionId, setPrescriptionId] = useState(existingPrescriptionId ?? "");

  const { data: prescription } = usePrescription(prescriptionId);
  const { data: ddiData } = useDdiCheck(prescriptionId);

  const addItems = useAddPrescriptionItems(prescriptionId);
  const removeItem = useRemovePrescriptionItem(prescriptionId);
  const cancelPrescription = useCancelPrescription(prescriptionId);

  const isDraft = !prescription || prescription.status === "DRAFT";
  const canEdit = isDraft && !!prescriptionId;
  const canSign = prescription?.status === "DRAFT" && (prescription.items?.length ?? 0) > 0;
  const canCancel =
    prescription?.status === "DRAFT" || prescription?.status === "SIGNED";

  async function ensurePrescription(): Promise<string> {
    if (prescriptionId) return prescriptionId;
    const p = await createPrescription.mutateAsync({ encounter_id: encounterId, patient_id: patientId });
    setPrescriptionId(p.id);
    return p.id;
  }

  async function handleAddItem(item: PrescriptionItemRequest) {
    await ensurePrescription();
    await addItems.mutateAsync([item]);
    setSelectedDrug(null);
  }

  async function handleRemoveItem(itemId: string) {
    setRemovingItemId(itemId);
    try {
      await removeItem.mutateAsync(itemId);
    } finally {
      setRemovingItemId(undefined);
    }
  }

  const hasContraindicated = ddiData?.has_contraindicated ?? false;

  if (!prescriptionId && !createPrescription.isPending) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Đơn thuốc</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <p className="text-sm text-muted-foreground">
            Chưa có đơn thuốc. Tìm thuốc để bắt đầu tạo đơn.
          </p>
          <DrugAutocomplete onSelect={async (drug) => {
            const id = await ensurePrescription();
            setPrescriptionId(id);
            setSelectedDrug(drug);
          }} />
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      {prescription && (
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <span className="text-sm font-medium">Đơn thuốc</span>
            <Badge variant={STATUS_VARIANT[prescription.status] ?? "outline"}>
              {STATUS_LABELS[prescription.status] ?? prescription.status}
            </Badge>
            {prescription.dtqg_code && (
              <Badge variant="outline" className="font-mono text-xs">
                ĐTQG: {prescription.dtqg_code}
              </Badge>
            )}
          </div>
          <div className="flex gap-2">
            {canCancel && (
              <Button
                variant="ghost"
                size="sm"
                className="text-destructive hover:text-destructive"
                onClick={() => setCancelDialogOpen(true)}
              >
                <XCircle className="h-4 w-4 mr-1" />
                Hủy đơn
              </Button>
            )}
            {canSign && (
              <Button
                size="sm"
                onClick={() => setSignWizardOpen(true)}
                disabled={hasContraindicated}
              >
                <PenTool className="h-4 w-4 mr-1" />
                Ký số & gửi ĐTQG
              </Button>
            )}
          </div>
        </div>
      )}

      {/* DDI Warnings */}
      {ddiData && ddiData.warnings.length > 0 && (
        <DdiWarningPanel warnings={ddiData.warnings} hasContraindicated={ddiData.has_contraindicated} />
      )}

      {/* Drug search */}
      {canEdit && !selectedDrug && (
        <DrugAutocomplete onSelect={setSelectedDrug} />
      )}

      {/* Item form */}
      {selectedDrug && canEdit && (
        <PrescriptionItemForm
          drug={selectedDrug}
          onSubmit={handleAddItem}
          onCancel={() => setSelectedDrug(null)}
          loading={addItems.isPending}
        />
      )}

      {/* Items table */}
      {prescription && prescription.items.length > 0 && (
        <PrescriptionItemTable
          items={prescription.items}
          canEdit={canEdit}
          onRemove={handleRemoveItem}
          removingId={removingItemId}
        />
      )}

      {/* Note */}
      {prescription && (
        <div className="space-y-1">
          <Label htmlFor="presc-note">Ghi chú đơn thuốc</Label>
          <Textarea
            id="presc-note"
            placeholder="Ghi chú thêm..."
            defaultValue={prescription.note ?? ""}
            disabled={!canEdit}
            className="resize-none h-16"
          />
        </div>
      )}

      {/* Total */}
      {prescription && prescription.total_amount > 0 && (
        <div className="flex justify-end text-sm font-medium">
          Tổng tiền: {prescription.total_amount.toLocaleString("vi-VN")}đ
        </div>
      )}

      {/* Wizards */}
      {prescription && signWizardOpen && (
        <SignPrescriptionWizard
          open={signWizardOpen}
          onClose={() => setSignWizardOpen(false)}
          prescription={prescription}
        />
      )}

      <ConfirmDialog
        open={cancelDialogOpen}
        onOpenChange={setCancelDialogOpen}
        onConfirm={async () => {
          await cancelPrescription.mutateAsync("Bác sĩ hủy đơn");
          setCancelDialogOpen(false);
        }}
        title="Hủy đơn thuốc"
        description="Bạn có chắc muốn hủy đơn thuốc này? Hành động này không thể hoàn tác."
        variant="destructive"
        isLoading={cancelPrescription.isPending}
      />
    </div>
  );
}
