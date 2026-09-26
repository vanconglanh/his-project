"use client";

import { useState, useEffect, useMemo } from "react";
import { Button } from "@/components/ui/button";
import { DrugAutocomplete } from "./DrugAutocomplete";
import { PrescriptionItemForm } from "./PrescriptionItemForm";
import { PrescriptionItemTable } from "./PrescriptionItemTable";
import { DdiWarningPanel } from "./DdiWarningPanel";
import { SignPrescriptionWizard } from "./SignPrescriptionWizard";
import { ConfirmDialog } from "./ConfirmDialog";
import { CdssAlertBanner } from "./prescriptions/CdssAlertBanner";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Textarea } from "@/components/ui/textarea";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import {
  usePrescription,
  useCreatePrescription,
  useAddPrescriptionItems,
  useRemovePrescriptionItem,
  useCancelPrescription,
  useDdiCheck,
  useUpdatePrescription,
} from "@/lib/hooks/use-prescriptions";
import { useAllergies, useActiveMedications } from "@/lib/hooks/use-patients";
import type { DrugMasterResponse } from "@/lib/api/drugs";
import type { PrescriptionItemRequest } from "@/lib/api/prescriptions";
import { PenTool, Save, XCircle, AlertTriangle, Pill } from "lucide-react";

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
  const [noteDraft, setNoteDraft] = useState<string | undefined>(undefined);

  const createPrescription = useCreatePrescription();

  // Use existing or create
  const [prescriptionId, setPrescriptionId] = useState(existingPrescriptionId ?? "");

  // BUG FIX (phát hiện khi test data migrate): existingPrescriptionId đến từ
  // PrescriptionTabPanel qua React Query (usePrescriptions) — thường CHƯA có
  // giá trị ở lần render đầu (đang loading). useState(existingPrescriptionId)
  // chỉ đọc giá trị này 1 LẦN lúc mount, nên khi query resolve xong và prop đổi
  // từ undefined -> id thật, state KHÔNG tự cập nhật theo — tab "Đơn thuốc" luôn
  // hiện "Chưa có đơn thuốc" dù đã có đơn (ẩn với mọi lượt khám đã có sẵn đơn
  // thuốc từ trước khi vào tab, không riêng gì data migrate).
  useEffect(() => {
    if (existingPrescriptionId && existingPrescriptionId !== prescriptionId) {
      setPrescriptionId(existingPrescriptionId);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [existingPrescriptionId]);

  const { data: prescription } = usePrescription(prescriptionId);
  const { data: ddiData } = useDdiCheck(prescriptionId);
  const { data: allergies } = useAllergies(patientId);
  const { data: activeMedications } = useActiveMedications(patientId);
  const [cdssBlocking, setCdssBlocking] = useState(false);

  const addItems = useAddPrescriptionItems(prescriptionId);
  const removeItem = useRemovePrescriptionItem(prescriptionId);
  const cancelPrescription = useCancelPrescription(prescriptionId);
  const updatePrescription = useUpdatePrescription(prescriptionId);

  const isDraft = !prescription || prescription.status === "DRAFT";
  const canEdit = isDraft && !!prescriptionId;
  const canCancel =
    prescription?.status === "DRAFT" || prescription?.status === "SIGNED";

  // Điều kiện ký hợp lệ theo phía client (mirror quy tắc server): ≥1 thuốc, mỗi
  // thuốc có ≥1 liều theo buổi > 0, số ngày dùng > 0, số lượng > 0. Đây chỉ là
  // UX để tránh round-trip vô ích — validation thật sự nằm ở server khi ký.
  const signValidationError = useMemo(() => {
    if (!prescription || prescription.status !== "DRAFT") return null;
    if ((prescription.items?.length ?? 0) === 0) return "Đơn thuốc chưa có thuốc nào";
    for (const item of prescription.items) {
      const totalDose =
        (item.morning_dose ?? 0) + (item.noon_dose ?? 0) + (item.afternoon_dose ?? 0) + (item.evening_dose ?? 0);
      if (totalDose <= 0) return `Thuốc "${item.drug_name}" chưa nhập liều dùng theo buổi`;
      if (!item.duration_days || item.duration_days <= 0) return `Thuốc "${item.drug_name}" chưa nhập số ngày dùng`;
      if (!item.quantity || item.quantity <= 0) return `Thuốc "${item.drug_name}" chưa nhập số lượng cấp phát`;
    }
    if (cdssBlocking) return "Còn cảnh báo lâm sàng nghiêm trọng chưa được xử lý hoặc bỏ qua";
    return null;
  }, [prescription, cdssBlocking]);

  const canSign = prescription?.status === "DRAFT" && !signValidationError;

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
    // Lưu ý: KHÔNG đặt DrugAutocomplete bên trong <Card>/<CardContent> — Card mặc định có
    // class "overflow-hidden" (components/ui/card.tsx) nên sẽ CẮT dropdown gợi ý thuốc
    // (chỉ còn hiển thị vài px). Tách phần header thông báo ra Card riêng, còn ô tìm thuốc
    // (và dropdown absolute của nó) đặt ở ngoài, trong một div overflow visible bình thường.
    return (
      <div className="space-y-3">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Đơn thuốc</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground">
              Chưa có đơn thuốc. Tìm thuốc để bắt đầu tạo đơn.
            </p>
          </CardContent>
        </Card>
        <DrugAutocomplete onSelect={async (drug) => {
          const id = await ensurePrescription();
          setPrescriptionId(id);
          setSelectedDrug(drug);
        }} />
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Cảnh báo dị ứng */}
      {allergies && allergies.length > 0 && (
        <Alert className="border-red-600 bg-red-50 dark:border-red-800 dark:bg-red-950/40">
          <AlertTriangle className="h-4 w-4 text-red-700" />
          <AlertDescription className="text-red-800 dark:text-red-200">
            <span className="font-medium">Dị ứng: </span>
            {allergies.map((a) => a.allergen).join(", ")}
          </AlertDescription>
        </Alert>
      )}

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
            {canEdit && (
              <Button
                variant="outline"
                size="sm"
                disabled={updatePrescription.isPending}
                onClick={() =>
                  updatePrescription.mutate({
                    note: noteDraft ?? prescription?.note ?? "",
                    row_version: prescription?.row_version ?? 0,
                  })
                }
              >
                <Save className="h-4 w-4 mr-1" />
                {updatePrescription.isPending ? "Đang lưu..." : "Lưu đơn"}
              </Button>
            )}
            {prescription?.status === "DRAFT" && (
              <Tooltip>
                <TooltipTrigger render={<span className="inline-block" />}>
                  <Button
                    size="sm"
                    onClick={() => setSignWizardOpen(true)}
                    disabled={!canSign || hasContraindicated}
                  >
                    <PenTool className="h-4 w-4 mr-1" />
                    Ký số & gửi ĐTQG
                  </Button>
                </TooltipTrigger>
                {(signValidationError || hasContraindicated) && (
                  <TooltipContent>
                    {signValidationError ?? "Có tương tác thuốc chống chỉ định, không thể ký đơn"}
                  </TooltipContent>
                )}
              </Tooltip>
            )}
          </div>
        </div>
      )}

      {/* DDI Warnings */}
      {ddiData && ddiData.warnings.length > 0 && (
        <DdiWarningPanel warnings={ddiData.warnings} hasContraindicated={ddiData.has_contraindicated} />
      )}

      {/* Cảnh báo lâm sàng (CDSS) */}
      {prescription && prescription.items.length > 0 && (
        <div className="space-y-1">
          <p className="text-sm font-medium flex items-center gap-1">
            <AlertTriangle className="h-4 w-4" />
            Cảnh báo lâm sàng
          </p>
          <CdssAlertBanner
            items={prescription.items.map((it) => ({ drug_id: it.drug_id }))}
            patientId={patientId}
            encounterId={encounterId}
            prescriptionId={prescriptionId}
            onBlockingChange={setCdssBlocking}
          />
        </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-[minmax(0,1fr)_280px] gap-4">
        <div className="space-y-4">
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
        </div>

        {/* Thuốc đang dùng khác */}
        <Card>
          <CardHeader>
            <CardTitle className="text-sm flex items-center gap-1">
              <Pill className="h-4 w-4" />
              Thuốc đang dùng khác
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-2">
            {!activeMedications || activeMedications.length === 0 ? (
              <p className="text-xs text-muted-foreground">Không có thuốc nào đang dùng</p>
            ) : (
              activeMedications.map((m) => (
                <div key={m.prescription_item_id} className="text-xs border rounded-md p-2 space-y-0.5">
                  <p className="font-medium">
                    {m.drug_name} {m.strength ? `(${m.strength})` : ""}
                  </p>
                  <p className="text-muted-foreground">{m.dosage_summary}</p>
                  <p className="text-muted-foreground">
                    Kê ngày: {new Date(m.prescribed_at).toLocaleDateString("vi-VN")}
                  </p>
                </div>
              ))
            )}
          </CardContent>
        </Card>
      </div>

      {/* Note */}
      {prescription && (
        <div className="space-y-1">
          <Label htmlFor="presc-note">Ghi chú đơn thuốc</Label>
          <Textarea
            id="presc-note"
            placeholder="Ghi chú thêm..."
            value={noteDraft ?? prescription.note ?? ""}
            onChange={(e) => setNoteDraft(e.target.value)}
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
