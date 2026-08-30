"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Loader2 } from "lucide-react";
import { useSearchPatients } from "@/lib/hooks/use-appointments";
import { useBranches } from "@/lib/hooks/use-branches";
import { useCreateInternalReferral } from "@/lib/hooks/use-internal-referrals";
import type { PatientOptionItem } from "@/lib/api/appointments";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
}

export function InternalReferralCreateDialog({ open, onOpenChange }: Props) {
  const [patientQuery, setPatientQuery] = useState("");
  const [selectedPatient, setSelectedPatient] = useState<PatientOptionItem | null>(null);
  const [targetBranchId, setTargetBranchId] = useState<number | "">("");
  const [reason, setReason] = useState("");
  const [note, setNote] = useState("");

  const { data: patientOptions } = useSearchPatients(patientQuery);
  const { data: branchesData } = useBranches();
  const createMutation = useCreateInternalReferral();

  const branches = branchesData?.data ?? [];

  function reset() {
    setPatientQuery("");
    setSelectedPatient(null);
    setTargetBranchId("");
    setReason("");
    setNote("");
  }

  function handleSubmit() {
    if (!selectedPatient || !targetBranchId) return;
    createMutation.mutate(
      {
        patient_id: selectedPatient.value,
        target_branch_id: Number(targetBranchId),
        reason: reason || undefined,
        note: note || undefined,
      },
      {
        onSuccess: () => {
          reset();
          onOpenChange(false);
        },
      }
    );
  }

  return (
    <Dialog
      open={open}
      onOpenChange={(o) => {
        if (!o) reset();
        onOpenChange(o);
      }}
    >
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Giới thiệu sang cơ sở khác</DialogTitle>
          <DialogDescription>
            Tạo giấy giới thiệu chuyển bệnh nhân nội bộ sang chi nhánh khác cùng tổ chức (BR-29).
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3">
          <div className="space-y-1">
            <Label htmlFor="referral-patient">
              Bệnh nhân <span className="text-destructive">*</span>
            </Label>
            {selectedPatient ? (
              <div className="flex items-center justify-between rounded-md border px-3 py-2 text-sm">
                <span>
                  {selectedPatient.label}
                  {selectedPatient.phone ? ` — ${selectedPatient.phone}` : ""}
                </span>
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setSelectedPatient(null)}
                >
                  Đổi
                </Button>
              </div>
            ) : (
              <>
                <Input
                  id="referral-patient"
                  placeholder="Tìm theo tên, mã BN hoặc SĐT..."
                  value={patientQuery}
                  onChange={(e) => setPatientQuery(e.target.value)}
                />
                {patientOptions && patientOptions.length > 0 && (
                  <div className="mt-1 max-h-40 overflow-y-auto rounded-md border">
                    {patientOptions.map((p) => (
                      <button
                        type="button"
                        key={p.value}
                        className="block w-full px-3 py-2 text-left text-sm hover:bg-muted"
                        onClick={() => {
                          setSelectedPatient(p);
                          setPatientQuery("");
                        }}
                      >
                        {p.label}
                        {p.phone ? ` — ${p.phone}` : ""}
                      </button>
                    ))}
                  </div>
                )}
              </>
            )}
          </div>

          <div className="space-y-1">
            <Label htmlFor="referral-target-branch">
              Chi nhánh đích <span className="text-destructive">*</span>
            </Label>
            <select
              id="referral-target-branch"
              className="h-9 w-full rounded-md border bg-background px-3 text-sm"
              value={targetBranchId}
              onChange={(e) =>
                setTargetBranchId(e.target.value ? Number(e.target.value) : "")
              }
            >
              <option value="">-- Chọn chi nhánh đích --</option>
              {branches.map((b) => (
                <option key={b.id} value={b.id}>
                  {b.name} ({b.code})
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1">
            <Label htmlFor="referral-reason">Lý do</Label>
            <Input id="referral-reason" value={reason} onChange={(e) => setReason(e.target.value)} />
          </div>

          <div className="space-y-1">
            <Label htmlFor="referral-note">Ghi chú</Label>
            <Input id="referral-note" value={note} onChange={(e) => setNote(e.target.value)} />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Huỷ
          </Button>
          <Button
            onClick={handleSubmit}
            disabled={!selectedPatient || !targetBranchId || createMutation.isPending}
          >
            {createMutation.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
            Tạo giấy giới thiệu
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
