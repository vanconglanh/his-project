"use client";

import { Phone, SkipForward, X, Printer, Stethoscope } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import type { ReceptionTicketResponse, TicketStatus, TicketPriority } from "@/lib/api/types";
import { getTicketPdfUrl } from "@/lib/api/reception";

const STATUS_CONFIG: Record<TicketStatus, { label: string; className: string }> = {
  WAITING: { label: "Chờ", className: "bg-yellow-100 text-yellow-800 dark:bg-yellow-900 dark:text-yellow-200" },
  CALLED: { label: "Đã gọi", className: "bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200" },
  IN_PROGRESS: { label: "Đang khám", className: "bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200" },
  DONE: { label: "Xong", className: "bg-muted text-muted-foreground" },
  SKIPPED: { label: "Bỏ qua", className: "bg-orange-100 text-orange-800" },
  CANCELLED: { label: "Huỷ", className: "bg-red-100 text-red-800" },
};

const PRIORITY_CONFIG: Record<TicketPriority, { label: string; borderClass: string }> = {
  NORMAL: { label: "", borderClass: "" },
  PRIORITY: { label: "Ưu tiên", borderClass: "border-l-4 border-l-orange-400" },
  EMERGENCY: { label: "Khẩn cấp", borderClass: "border-l-4 border-l-red-500" },
};

interface TicketCardProps {
  ticket: ReceptionTicketResponse;
  onCall?: (id: string) => void;
  onSkip?: (id: string) => void;
  onCancel?: (id: string) => void;
  onAdmit?: (id: string) => void;
  isCallLoading?: boolean;
  isSkipLoading?: boolean;
  isAdmitLoading?: boolean;
}

export function TicketCard({
  ticket,
  onCall,
  onSkip,
  onCancel,
  onAdmit,
  isCallLoading,
  isSkipLoading,
  isAdmitLoading,
}: TicketCardProps) {
  const statusCfg = STATUS_CONFIG[ticket.status];
  const priorityCfg = PRIORITY_CONFIG[ticket.priority];
  const canAct = ticket.status === "WAITING" || ticket.status === "CALLED";

  const printTicket = () => {
    window.open(getTicketPdfUrl(ticket.id), "_blank");
  };

  return (
    <div
      className={cn(
        "bg-card rounded-lg border p-3 space-y-2 shadow-sm",
        priorityCfg.borderClass
      )}
    >
      {/* Ticket number */}
      <div className="flex items-center justify-between">
        <span className="text-2xl font-bold tabular-nums leading-none">
          {ticket.ticket_no}
        </span>
        <span
          className={cn(
            "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
            statusCfg.className
          )}
        >
          {statusCfg.label}
        </span>
      </div>

      {/* Patient info */}
      <div>
        <p className="text-sm font-medium line-clamp-1">
          {ticket.patient_summary?.full_name ?? "—"}
        </p>
        <p className="text-xs text-muted-foreground">
          {ticket.patient_summary?.code}
          {priorityCfg.label && (
            <span className="ml-1 text-orange-600 font-medium">• {priorityCfg.label}</span>
          )}
        </p>
      </div>

      {ticket.reason_for_visit && (
        <p className="text-xs text-muted-foreground line-clamp-2">{ticket.reason_for_visit}</p>
      )}

      {/* Đưa vào khám: tạo lượt khám + điều hướng sang màn khám (action chính) */}
      {canAct && onAdmit && (
        <Button
          size="sm"
          className="w-full h-8 text-xs gap-1.5"
          onClick={() => onAdmit(ticket.id)}
          disabled={isAdmitLoading}
        >
          <Stethoscope className="h-3.5 w-3.5" />
          Đưa vào khám
        </Button>
      )}

      {/* Actions phụ */}
      <div className="flex gap-1 pt-1">
        {canAct && onCall && (
          <Button
            size="sm"
            variant="outline"
            className="flex-1 h-7 text-xs gap-1"
            onClick={() => onCall(ticket.id)}
            disabled={isCallLoading}
          >
            <Phone className="h-3 w-3" />
            Gọi vào
          </Button>
        )}
        {canAct && onSkip && (
          <Button
            size="icon"
            variant="outline"
            className="h-7 w-7"
            onClick={() => onSkip(ticket.id)}
            disabled={isSkipLoading}
            title="Bỏ qua"
          >
            <SkipForward className="h-3 w-3" />
          </Button>
        )}
        {canAct && onCancel && (
          <Button
            size="icon"
            variant="outline"
            className="h-7 w-7 hover:text-destructive"
            onClick={() => onCancel(ticket.id)}
            title="Huỷ"
          >
            <X className="h-3 w-3" />
          </Button>
        )}
        <Button
          size="icon"
          variant="ghost"
          className="h-7 w-7 ml-auto"
          onClick={printTicket}
          title="In phiếu"
        >
          <Printer className="h-3 w-3" />
        </Button>
      </div>
    </div>
  );
}
