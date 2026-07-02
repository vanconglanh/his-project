"use client";

import { useState } from "react";
import Image from "next/image";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Copy, CheckCheck } from "lucide-react";
import { toast } from "sonner";

interface Props {
  prescriptionId: string;
  maDonThuoc?: string | null;
  qrImageUrl?: string | null;
}

export function QrPrescription({ prescriptionId, maDonThuoc, qrImageUrl }: Props) {
  const [copied, setCopied] = useState(false);

  const qrUrl = qrImageUrl ?? `${process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000"}/api/v1/prescriptions/${prescriptionId}/qr`;

  async function copyCode() {
    if (!maDonThuoc) return;
    await navigator.clipboard.writeText(maDonThuoc);
    setCopied(true);
    toast.success("Đã sao chép mã đơn thuốc");
    setTimeout(() => setCopied(false), 2000);
  }

  return (
    <div className="flex flex-col items-center gap-4 p-4">
      <div className="border rounded-lg p-2 bg-white">
        <Image
          src={qrUrl}
          alt="QR code đơn thuốc"
          width={200}
          height={200}
          className="block"
          unoptimized
        />
      </div>

      {maDonThuoc && (
        <div className="flex items-center gap-2">
          <Badge variant="outline" className="font-mono text-base px-4 py-1.5 tracking-widest">
            {maDonThuoc}
          </Badge>
          <Button
            variant="ghost"
            size="icon"
            className="h-8 w-8"
            onClick={copyCode}
            aria-label="Sao chép mã đơn thuốc"
          >
            {copied ? <CheckCheck className="h-4 w-4 text-green-600" /> : <Copy className="h-4 w-4" />}
          </Button>
        </div>
      )}

      <p className="text-xs text-muted-foreground text-center">
        Quét mã QR bằng ứng dụng donthuocquocgia.vn
      </p>
    </div>
  );
}
