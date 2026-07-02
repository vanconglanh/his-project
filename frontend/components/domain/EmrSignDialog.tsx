"use client";

import { useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogDescription,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { ShieldCheck } from "lucide-react";

interface Props {
  open: boolean;
  onClose: () => void;
  onSign: (signatureData: string, certificateId: string) => void;
  isLoading?: boolean;
}

export function EmrSignDialog({ open, onClose, onSign, isLoading }: Props) {
  const [pin, setPin] = useState("");

  function handleSign() {
    // Mock: in dev, use PIN as signature_data, "DEV_CERT" as cert id
    const mockSig = btoa(`MOCK_PKCS7_${pin}_${Date.now()}`);
    onSign(mockSig, "DEV_CERT_SLOT_1");
    setPin("");
  }

  return (
    <Dialog open={open} onOpenChange={(v) => !v && onClose()}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-primary" />
            Ký số bệnh án điện tử
          </DialogTitle>
          <DialogDescription>
            Nhập PIN USB token để ký số bệnh án. Sau khi ký, bệnh án không thể sửa.
          </DialogDescription>
        </DialogHeader>
        <div className="space-y-4 py-2">
          <div className="space-y-1.5">
            <Label htmlFor="pin-input">PIN USB Token</Label>
            <Input
              id="pin-input"
              type="password"
              placeholder="Nhập PIN..."
              value={pin}
              onChange={(e) => setPin(e.target.value)}
              onKeyDown={(e) => e.key === "Enter" && pin && handleSign()}
              autoFocus
            />
          </div>
          <p className="text-xs text-muted-foreground">
            Môi trường dev: nhập bất kỳ PIN để mô phỏng ký số.
          </p>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={onClose} disabled={isLoading}>
            Hủy
          </Button>
          <Button onClick={handleSign} disabled={!pin || isLoading}>
            {isLoading ? "Đang ký..." : "Ký số"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
