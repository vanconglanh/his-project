"use client";

import { Download, Clock, QrCode } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PortalLayout } from "@/components/domain/PortalLayout";
import {
  usePortalPrescriptions,
  useDownloadPrescriptionPdf,
} from "@/lib/hooks/use-portal";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

export default function PortalPrescriptionsPage() {
  const { data, isLoading } = usePortalPrescriptions();
  const downloadMutation = useDownloadPrescriptionPdf();

  const prescriptions = data?.data ?? [];

  return (
    <PortalLayout>
      <div className="space-y-4">
        <div>
          <h2 className="text-xl font-bold">Đơn thuốc của tôi</h2>
          <p className="text-sm text-muted-foreground">{prescriptions.length} đơn thuốc</p>
        </div>

        {isLoading ? (
          <div className="space-y-3">
            {[...Array(3)].map((_, i) => (
              <div key={i} className="h-36 animate-pulse rounded-lg bg-muted" />
            ))}
          </div>
        ) : prescriptions.length === 0 ? (
          <div className="flex flex-col items-center py-16 text-center">
            <Clock className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="font-medium">Chưa có đơn thuốc</p>
          </div>
        ) : (
          <div className="space-y-4">
            {prescriptions.map((rx) => (
              <Card key={rx.id}>
                <CardHeader className="pb-2">
                  <div className="flex items-start justify-between">
                    <div>
                      <CardTitle className="text-sm font-medium">{rx.prescription_code}</CardTitle>
                      <p className="text-xs text-muted-foreground mt-0.5">
                        {format(parseISO(rx.issued_at), "dd/MM/yyyy HH:mm", { locale: vi })} —{" "}
                        {rx.doctor_name}
                      </p>
                    </div>
                    <div className="flex items-center gap-2">
                      {rx.dtqg_code && (
                        <Badge variant="secondary" className="text-xs flex items-center gap-1">
                          <QrCode className="h-3 w-3" />
                          ĐTQG
                        </Badge>
                      )}
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => downloadMutation.mutate(rx.id)}
                        disabled={downloadMutation.isPending}
                      >
                        <Download className="mr-1.5 h-3.5 w-3.5" />
                        PDF
                      </Button>
                    </div>
                  </div>
                </CardHeader>
                <CardContent>
                  <ul className="divide-y">
                    {rx.items.map((item, idx) => (
                      <li key={idx} className="py-2 first:pt-0 last:pb-0">
                        <div className="flex items-start justify-between text-sm">
                          <div>
                            <p className="font-medium">{item.drug_name}</p>
                            <p className="text-xs text-muted-foreground">{item.usage_instruction}</p>
                          </div>
                          <span className="text-sm text-muted-foreground ml-4 shrink-0">
                            {item.dosage} × {item.quantity}
                          </span>
                        </div>
                      </li>
                    ))}
                  </ul>
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </PortalLayout>
  );
}
