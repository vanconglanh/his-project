"use client";

import { useState } from "react";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { QrPrescription } from "./QrPrescription";
import { useSignPrescription } from "@/lib/hooks/use-prescriptions";
import { useSubmitToDtqg } from "@/lib/hooks/use-dtqg";
import { Printer, Send, Key, CheckCircle, AlertCircle } from "lucide-react";
import { printPrescriptionPdf } from "@/lib/api/prescriptions";
import type { PrescriptionResponse } from "@/lib/api/prescriptions";

const MOCK_TOKEN_SLOTS = [
  { id: "slot-1", label: "USB Token #1 (VNPT-CA)" },
  { id: "slot-2", label: "USB Token #2 (VIETTEL-CA)" },
  { id: "slot-3", label: "Chứng thư phần mềm (dev)" },
];

type Step = "select_token" | "enter_pin" | "signing" | "submit_dtqg" | "done" | "error";

interface Props {
  open: boolean;
  onClose: () => void;
  prescription: PrescriptionResponse;
}

export function SignPrescriptionWizard({ open, onClose, prescription }: Props) {
  const [step, setStep] = useState<Step>("select_token");
  const [tokenSlot, setTokenSlot] = useState("");
  const [pin, setPin] = useState("");
  const [errorMsg, setErrorMsg] = useState("");
  const [dtqgData, setDtqgData] = useState<{ ma_don_thuoc?: string; qr_image_url?: string } | null>(null);

  const signMutation = useSignPrescription(prescription.id);
  const submitDtqg = useSubmitToDtqg();

  async function handleSign() {
    if (!pin || pin.length < 4) {
      setErrorMsg("PIN phải có ít nhất 4 ký tự");
      return;
    }
    setStep("signing");
    setErrorMsg("");

    // Mock: generate fake signature for dev
    const mockSignature = btoa(`PKCS7:${tokenSlot}:${Date.now()}`);
    const mockThumbprint = btoa(tokenSlot).slice(0, 40);

    try {
      await signMutation.mutateAsync({
        signature_data: mockSignature,
        certificate_thumbprint: mockThumbprint,
        signing_time: new Date().toISOString(),
      });
      setStep("submit_dtqg");
    } catch {
      setErrorMsg("Ký số thất bại. Kiểm tra USB token và PIN.");
      setStep("error");
    }
  }

  async function handleSubmitDtqg() {
    try {
      const result = await submitDtqg.mutateAsync(prescription.id);
      setDtqgData({
        ma_don_thuoc: result.ma_don_thuoc,
        qr_image_url: result.qr_image_url,
      });
      setStep("done");
    } catch {
      setErrorMsg("Gửi ĐTQG thất bại. Có thể thử lại sau.");
      setStep("done"); // still show QR if signed
    }
  }

  function handleReset() {
    setStep("select_token");
    setTokenSlot("");
    setPin("");
    setErrorMsg("");
    setDtqgData(null);
  }

  function handleClose() {
    handleReset();
    onClose();
  }

  const stepTitles: Record<Step, string> = {
    select_token: "Bước 1: Chọn USB Token",
    enter_pin: "Bước 2: Nhập PIN",
    signing: "Đang ký số...",
    submit_dtqg: "Bước 3: Gửi ĐTQG",
    done: "Hoàn tất",
    error: "Lỗi",
  };

  return (
    <Dialog open={open} onOpenChange={handleClose}>
      <DialogContent className="max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Key className="h-5 w-5 text-primary" />
            {stepTitles[step]}
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-4">
          {/* Step: select token */}
          {step === "select_token" && (
            <div className="space-y-4">
              <div className="space-y-1">
                <Label>Chọn USB Token / Chứng thư số</Label>
                <Select
                  items={Object.fromEntries(MOCK_TOKEN_SLOTS.map((s) => [s.id, s.label]))}
                  value={tokenSlot}
                  onValueChange={(v) => setTokenSlot(v ?? "")}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="-- Chọn token --" />
                  </SelectTrigger>
                  <SelectContent>
                    {MOCK_TOKEN_SLOTS.map((s) => (
                      <SelectItem key={s.id} value={s.id}>
                        {s.label}
                      </SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <p className="text-xs text-muted-foreground">
                Dev mode: chọn "Chứng thư phần mềm" để test không cần USB thật.
              </p>
              <div className="flex justify-end gap-2">
                <Button variant="ghost" onClick={handleClose}>Hủy</Button>
                <Button onClick={() => setStep("enter_pin")} disabled={!tokenSlot}>
                  Tiếp theo
                </Button>
              </div>
            </div>
          )}

          {/* Step: enter pin */}
          {step === "enter_pin" && (
            <div className="space-y-4">
              <div className="space-y-1">
                <Label htmlFor="pin">Mã PIN token</Label>
                <Input
                  id="pin"
                  type="password"
                  placeholder="Nhập PIN..."
                  value={pin}
                  onChange={(e) => setPin(e.target.value)}
                  onKeyDown={(e) => e.key === "Enter" && handleSign()}
                  autoFocus
                />
              </div>
              {errorMsg && <p className="text-sm text-destructive">{errorMsg}</p>}
              <div className="flex justify-between gap-2">
                <Button variant="ghost" onClick={() => setStep("select_token")}>Quay lại</Button>
                <Button onClick={handleSign} disabled={signMutation.isPending}>
                  Ký số
                </Button>
              </div>
            </div>
          )}

          {/* Step: signing */}
          {step === "signing" && (
            <div className="flex flex-col items-center py-6 gap-3">
              <div className="animate-spin rounded-full border-4 border-primary border-t-transparent h-10 w-10" />
              <p className="text-sm text-muted-foreground">Đang xử lý ký số, vui lòng chờ...</p>
            </div>
          )}

          {/* Step: submit dtqg */}
          {step === "submit_dtqg" && (
            <div className="space-y-4">
              <div className="flex items-center gap-2 text-green-700 bg-green-50 rounded-md p-3 border border-green-200">
                <CheckCircle className="h-5 w-5 shrink-0" />
                <p className="text-sm font-medium">Ký số thành công!</p>
              </div>
              <p className="text-sm text-muted-foreground">
                Gửi đơn thuốc lên Cổng Đơn Thuốc Quốc Gia để nhận mã đơn và QR code.
              </p>
              <div className="flex justify-end gap-2">
                <Button variant="ghost" onClick={handleClose}>Bỏ qua</Button>
                <Button onClick={handleSubmitDtqg} disabled={submitDtqg.isPending}>
                  <Send className="h-4 w-4 mr-2" />
                  {submitDtqg.isPending ? "Đang gửi..." : "Gửi lên ĐTQG"}
                </Button>
              </div>
            </div>
          )}

          {/* Step: done */}
          {step === "done" && (
            <div className="space-y-4">
              {errorMsg && (
                <div className="flex items-center gap-2 text-orange-700 bg-orange-50 rounded-md p-3 border border-orange-200">
                  <AlertCircle className="h-5 w-5 shrink-0" />
                  <p className="text-sm">{errorMsg}</p>
                </div>
              )}

              {dtqgData?.ma_don_thuoc ? (
                <QrPrescription
                  prescriptionId={prescription.id}
                  maDonThuoc={dtqgData.ma_don_thuoc}
                  qrImageUrl={dtqgData.qr_image_url}
                />
              ) : (
                <div className="flex items-center gap-2 text-green-700 bg-green-50 rounded-md p-3 border border-green-200">
                  <CheckCircle className="h-5 w-5 shrink-0" />
                  <p className="text-sm font-medium">Đơn thuốc đã được ký số</p>
                </div>
              )}

              <div className="flex justify-between gap-2">
                <Button
                  variant="outline"
                  onClick={() => printPrescriptionPdf(prescription.id)}
                >
                  <Printer className="h-4 w-4 mr-2" />
                  In đơn thuốc
                </Button>
                <Button onClick={handleClose}>Đóng</Button>
              </div>
            </div>
          )}

          {/* Step: error */}
          {step === "error" && (
            <div className="space-y-4">
              <div className="flex items-center gap-2 text-destructive bg-destructive/10 rounded-md p-3 border border-destructive/30">
                <AlertCircle className="h-5 w-5 shrink-0" />
                <p className="text-sm">{errorMsg}</p>
              </div>
              <div className="flex justify-end gap-2">
                <Button variant="ghost" onClick={handleClose}>Đóng</Button>
                <Button onClick={handleReset}>Thử lại</Button>
              </div>
            </div>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
