"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { ShieldCheck } from "lucide-react";

interface Props {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  onSign: (certThumbprint: string, pin: string) => void;
  isPending?: boolean;
}

export function BhytSignDialog({ open, onOpenChange, onSign, isPending }: Props) {
  const [cert, setCert] = useState("");
  const [pin, setPin] = useState("");

  function handleSign() {
    onSign(cert, pin);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <div className="flex items-center gap-2">
            <ShieldCheck className="h-5 w-5 text-violet-600" />
            <DialogTitle>Ký số XML BHYT</DialogTitle>
          </div>
          <DialogDescription>
            Kết nối USB token / chứng thư số để ký file XML trước khi gửi cổng giám định.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-4">
          <div className="space-y-1.5">
            <Label htmlFor="cert">Thumbprint chứng thư số</Label>
            <Input
              id="cert"
              placeholder="e.g. 4A:B2:..."
              value={cert}
              onChange={(e) => setCert(e.target.value)}
            />
          </div>
          <div className="space-y-1.5">
            <Label htmlFor="pin">PIN</Label>
            <Input
              id="pin"
              type="password"
              placeholder="PIN USB token"
              value={pin}
              onChange={(e) => setPin(e.target.value)}
            />
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Huỷ
          </Button>
          <Button
            onClick={handleSign}
            disabled={isPending || !pin}
            className="bg-violet-600 hover:bg-violet-700"
          >
            {isPending ? "Đang ký..." : "Ký số"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
