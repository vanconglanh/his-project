"use client";

import { useState } from "react";
import { usePrescription } from "@/lib/hooks/use-prescriptions";
import { useDtqgStatus } from "@/lib/hooks/use-dtqg";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { PrescriptionItemTable } from "@/components/domain/PrescriptionItemTable";
import { QrPrescription } from "@/components/domain/QrPrescription";
import { DdiWarningPanel } from "@/components/domain/DdiWarningPanel";
import { SignPrescriptionWizard } from "@/components/domain/SignPrescriptionWizard";
import { ArrowLeft, Printer, AlertTriangle, PenTool } from "lucide-react";
import Link from "next/link";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import { printPrescriptionPdf } from "@/lib/api/prescriptions";

const STATUS_LABELS: Record<string, string> = {
  DRAFT: "Nháp",
  SIGNED: "Đã ký",
  SUBMITTED_DTQG: "Đã gửi ĐTQG",
  DISPENSED: "Đã phát",
  PARTIAL_DISPENSED: "Phát một phần",
  CANCELLED: "Đã hủy",
};

const STATUS_VARIANT: Record<string, string> = {
  DRAFT: "bg-gray-100 text-gray-700 border-gray-300",
  SIGNED: "bg-blue-100 text-blue-800 border-blue-300",
  SUBMITTED_DTQG: "bg-purple-100 text-purple-800 border-purple-300",
  DISPENSED: "bg-green-100 text-green-800 border-green-300",
  PARTIAL_DISPENSED: "bg-yellow-100 text-yellow-800 border-yellow-300",
  CANCELLED: "bg-red-100 text-red-800 border-red-300",
};

interface Props {
  prescriptionId: string;
}

export function PrescriptionDetailClient({ prescriptionId }: Props) {
  const { data: prescription, isLoading } = usePrescription(prescriptionId);
  const { data: dtqgData } = useDtqgStatus(prescriptionId);
  const [signWizardOpen, setSignWizardOpen] = useState(false);

  if (isLoading) {
    return (
      <div className="space-y-4 p-4">
        <Skeleton className="h-8 w-48" />
        <Skeleton className="h-64 w-full" />
      </div>
    );
  }

  if (!prescription) {
    return (
      <div className="flex flex-col items-center gap-4 py-20">
        <AlertTriangle className="h-12 w-12 text-muted-foreground" />
        <p className="text-muted-foreground">Không tìm thấy đơn thuốc</p>
        <Link href="/prescriptions">
          <Button variant="outline">Quay lại danh sách</Button>
        </Link>
      </div>
    );
  }

  const canSign = prescription.status === "DRAFT" && prescription.items.length > 0;

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Link href="/prescriptions" className="hover:text-foreground flex items-center gap-1">
          <ArrowLeft className="h-4 w-4" />
          Kê đơn thuốc
        </Link>
        <span>/</span>
        <span className="text-foreground">Chi tiết đơn</span>
      </div>

      {/* Header */}
      <div className="flex items-start justify-between flex-wrap gap-4">
        <div className="space-y-1">
          <div className="flex items-center gap-3 flex-wrap">
            <h2 className="text-xl font-bold">Đơn thuốc</h2>
            <Badge className={STATUS_VARIANT[prescription.status]} variant="outline">
              {STATUS_LABELS[prescription.status] ?? prescription.status}
            </Badge>
            {prescription.dtqg_code && (
              <Badge variant="outline" className="font-mono text-sm">
                ĐTQG: {prescription.dtqg_code}
              </Badge>
            )}
          </div>
          <p className="text-sm text-muted-foreground">
            Kê lúc: {format(parseISO(prescription.prescribed_at), "dd/MM/yyyy HH:mm", { locale: vi })}
          </p>
        </div>
        <div className="flex gap-2">
          <Button variant="outline" onClick={() => printPrescriptionPdf(prescription.id)}>
            <Printer className="h-4 w-4 mr-2" />
            In đơn
          </Button>
          {canSign && (
            <Button onClick={() => setSignWizardOpen(true)}>
              <PenTool className="h-4 w-4 mr-2" />
              Ký số & gửi ĐTQG
            </Button>
          )}
        </div>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
        {/* Main content */}
        <div className="md:col-span-2 space-y-6">
          {/* Patient info */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Thông tin bệnh nhân</CardTitle>
            </CardHeader>
            <CardContent className="space-y-2 text-sm">
              <div className="grid grid-cols-2 gap-x-4 gap-y-1">
                <span className="text-muted-foreground">Họ tên:</span>
                <span className="font-medium">{prescription.patient_summary?.full_name}</span>
                <span className="text-muted-foreground">Giới tính:</span>
                <span>{prescription.patient_summary?.gender}</span>
                <span className="text-muted-foreground">Ngày sinh:</span>
                <span>{prescription.patient_summary?.dob}</span>
                {prescription.patient_summary?.bhyt_no && (
                  <>
                    <span className="text-muted-foreground">Số BHYT:</span>
                    <span className="font-mono">{prescription.patient_summary.bhyt_no}</span>
                  </>
                )}
                <span className="text-muted-foreground">Bác sĩ kê:</span>
                <span>{prescription.doctor_name}</span>
              </div>
            </CardContent>
          </Card>

          {/* DDI Warnings */}
          {prescription.ddi_warnings && prescription.ddi_warnings.length > 0 && (
            <DdiWarningPanel warnings={prescription.ddi_warnings} />
          )}

          {/* Items */}
          <Card>
            <CardHeader>
              <CardTitle className="text-base">Danh sách thuốc</CardTitle>
            </CardHeader>
            <CardContent>
              <PrescriptionItemTable items={prescription.items} canEdit={false} />
            </CardContent>
          </Card>

          {prescription.note && (
            <Card>
              <CardHeader><CardTitle className="text-base">Ghi chú</CardTitle></CardHeader>
              <CardContent>
                <p className="text-sm">{prescription.note}</p>
              </CardContent>
            </Card>
          )}

          {prescription.total_amount > 0 && (
            <div className="flex justify-end text-base font-semibold">
              Tổng tiền: {prescription.total_amount.toLocaleString("vi-VN")}đ
            </div>
          )}
        </div>

        {/* Sidebar: QR + DTQG */}
        <div className="space-y-4">
          {prescription.dtqg_code && (
            <Card>
              <CardHeader><CardTitle className="text-base">Mã ĐTQG</CardTitle></CardHeader>
              <CardContent>
                <QrPrescription
                  prescriptionId={prescription.id}
                  maDonThuoc={prescription.dtqg_code}
                  qrImageUrl={dtqgData?.qr_image_url}
                />
              </CardContent>
            </Card>
          )}

          {/* DTQG submission status */}
          {dtqgData && (
            <Card>
              <CardHeader><CardTitle className="text-base">Gửi ĐTQG</CardTitle></CardHeader>
              <CardContent className="space-y-2 text-sm">
                <div className="grid grid-cols-2 gap-1">
                  <span className="text-muted-foreground">Trạng thái:</span>
                  <span>{dtqgData.status}</span>
                  <span className="text-muted-foreground">Lần thử:</span>
                  <span>{dtqgData.retry_count}</span>
                  {dtqgData.error_message && (
                    <>
                      <span className="text-muted-foreground">Lỗi:</span>
                      <span className="text-destructive text-xs">{dtqgData.error_message}</span>
                    </>
                  )}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Signed info */}
          {prescription.signed_at && (
            <Card>
              <CardHeader><CardTitle className="text-base">Ký số</CardTitle></CardHeader>
              <CardContent className="text-sm space-y-1">
                <div className="grid grid-cols-2 gap-1">
                  <span className="text-muted-foreground">Ký lúc:</span>
                  <span>{format(parseISO(prescription.signed_at), "dd/MM/yyyy HH:mm", { locale: vi })}</span>
                </div>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {signWizardOpen && (
        <SignPrescriptionWizard
          open={signWizardOpen}
          onClose={() => setSignWizardOpen(false)}
          prescription={prescription}
        />
      )}
    </div>
  );
}
