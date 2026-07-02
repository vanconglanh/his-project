"use client";

import { useState } from "react";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { LabIntegrationDashboard } from "@/components/domain/LabIntegrationDashboard";
import { WebhookLogViewer } from "@/components/domain/WebhookLogViewer";
import {
  useOutbound,
  useInbound,
  useRetryOutbound,
  useReprocessInbound,
} from "@/lib/hooks/use-lab-integration";

const OUTBOUND_STATUS_LABELS: Record<string, string> = {
  PENDING: "Chờ gửi",
  SENT: "Đã gửi",
  ACKED: "Xác nhận",
  FAILED: "Thất bại",
};

const OUTBOUND_STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  PENDING: "secondary",
  SENT: "outline",
  ACKED: "default",
  FAILED: "destructive",
};

const INBOUND_STATUS_VARIANT: Record<string, "default" | "secondary" | "destructive" | "outline"> = {
  RECEIVED: "secondary",
  PROCESSED: "default",
  FAILED: "destructive",
};

export function LabIntegrationTab() {
  return (
    <div className="space-y-6">
      <LabIntegrationDashboard />

      <Tabs defaultValue="outbound">
        <TabsList>
          <TabsTrigger value="outbound">Gửi đi (Outbound)</TabsTrigger>
          <TabsTrigger value="inbound">Nhận về (Inbound)</TabsTrigger>
        </TabsList>
        <TabsContent value="outbound" className="pt-4">
          <OutboundTable />
        </TabsContent>
        <TabsContent value="inbound" className="pt-4">
          <InboundTable />
        </TabsContent>
      </Tabs>
    </div>
  );
}

function OutboundTable() {
  const { data, isLoading } = useOutbound({ page: 1, page_size: 30 });
  const retryMutation = useRetryOutbound();
  const rows = data?.data ?? [];

  if (isLoading) return <Skeleton className="h-48 w-full rounded-md" />;

  return (
    <div className="rounded-md border overflow-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Đối tác</TableHead>
            <TableHead>Mã đơn ngoài</TableHead>
            <TableHead>Trạng thái</TableHead>
            <TableHead>Retry</TableHead>
            <TableHead>Gửi lúc</TableHead>
            <TableHead>Lỗi</TableHead>
            <TableHead className="text-right">Thao tác</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={7} className="text-center text-muted-foreground py-8">
                Chưa có dữ liệu gửi đi
              </TableCell>
            </TableRow>
          )}
          {rows.map((row) => (
            <TableRow key={row.id}>
              <TableCell>{row.partner_name}</TableCell>
              <TableCell className="font-mono text-xs">{row.external_order_id ?? "—"}</TableCell>
              <TableCell>
                <Badge variant={OUTBOUND_STATUS_VARIANT[row.status] ?? "outline"}>
                  {OUTBOUND_STATUS_LABELS[row.status] ?? row.status}
                </Badge>
              </TableCell>
              <TableCell>{row.retry_count}</TableCell>
              <TableCell className="text-xs text-muted-foreground">
                {row.sent_at
                  ? format(new Date(row.sent_at), "dd/MM/yyyy HH:mm", { locale: vi })
                  : "—"}
              </TableCell>
              <TableCell className="max-w-xs truncate text-xs text-red-500">
                {row.error_message ?? ""}
              </TableCell>
              <TableCell className="text-right">
                {row.status === "FAILED" && (
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => retryMutation.mutate(row.id)}
                    disabled={retryMutation.isPending}
                  >
                    Retry
                  </Button>
                )}
              </TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function InboundTable() {
  const { data, isLoading } = useInbound({ page: 1, page_size: 30 });
  const reprocessMutation = useReprocessInbound();
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const rows = data?.data ?? [];

  if (isLoading) return <Skeleton className="h-48 w-full rounded-md" />;

  return (
    <div className="rounded-md border overflow-auto">
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Đối tác</TableHead>
            <TableHead>Mã kết quả ngoài</TableHead>
            <TableHead>Trạng thái</TableHead>
            <TableHead>KQ đã xử lý</TableHead>
            <TableHead>Nhận lúc</TableHead>
            <TableHead className="text-right">Thao tác</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {rows.length === 0 && (
            <TableRow>
              <TableCell colSpan={6} className="text-center text-muted-foreground py-8">
                Chưa có dữ liệu nhận về
              </TableCell>
            </TableRow>
          )}
          {rows.map((row) => (
            <>
              <TableRow key={row.id}>
                <TableCell>{row.partner_name}</TableCell>
                <TableCell className="font-mono text-xs">{row.external_result_id}</TableCell>
                <TableCell>
                  <Badge variant={INBOUND_STATUS_VARIANT[row.status] ?? "outline"}>
                    {row.status === "RECEIVED" ? "Đã nhận" : row.status === "PROCESSED" ? "Đã xử lý" : "Thất bại"}
                  </Badge>
                </TableCell>
                <TableCell>{row.processed_result_count}</TableCell>
                <TableCell className="text-xs text-muted-foreground">
                  {format(new Date(row.received_at), "dd/MM/yyyy HH:mm", { locale: vi })}
                </TableCell>
                <TableCell className="text-right">
                  <div className="flex gap-1 justify-end">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setExpandedId(expandedId === row.id ? null : row.id)}
                    >
                      {expandedId === row.id ? "Thu" : "Raw"}
                    </Button>
                    {row.status === "FAILED" && (
                      <Button
                        variant="outline"
                        size="sm"
                        onClick={() => reprocessMutation.mutate(row.id)}
                        disabled={reprocessMutation.isPending}
                      >
                        Xử lý lại
                      </Button>
                    )}
                  </div>
                </TableCell>
              </TableRow>
              {expandedId === row.id && (
                <TableRow key={`${row.id}-raw`}>
                  <TableCell colSpan={6} className="p-4 bg-muted/30">
                    <WebhookLogViewer inboundId={row.id} />
                  </TableCell>
                </TableRow>
              )}
            </>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}
