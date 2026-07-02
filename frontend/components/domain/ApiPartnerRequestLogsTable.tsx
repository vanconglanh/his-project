"use client";

import { format, parseISO } from "date-fns";
import { Badge } from "@/components/ui/badge";
import { useApiPartnerRequestLogs } from "@/lib/hooks/use-api-partners";

interface ApiPartnerRequestLogsTableProps {
  partnerId: string;
  page?: number;
}

export function ApiPartnerRequestLogsTable({
  partnerId,
  page = 1,
}: ApiPartnerRequestLogsTableProps) {
  const { data, isLoading } = useApiPartnerRequestLogs(partnerId, page);

  if (isLoading) {
    return <div className="h-32 animate-pulse rounded-lg bg-muted" />;
  }

  if (!data?.data?.length) {
    return (
      <p className="text-center py-6 text-sm text-muted-foreground">Không có log request nào</p>
    );
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-xs">
        <thead>
          <tr className="border-b text-muted-foreground text-left">
            <th className="py-2 pr-3 font-medium">Thời gian</th>
            <th className="py-2 pr-3 font-medium">Method</th>
            <th className="py-2 pr-3 font-medium">Path</th>
            <th className="py-2 pr-3 font-medium">Status</th>
            <th className="py-2 font-medium">Duration</th>
          </tr>
        </thead>
        <tbody>
          {data.data.map((log) => (
            <tr key={log.id} className="border-b last:border-0 hover:bg-muted/30">
              <td className="py-1.5 pr-3 text-muted-foreground whitespace-nowrap">
                {format(parseISO(log.called_at), "HH:mm:ss dd/MM")}
              </td>
              <td className="py-1.5 pr-3">
                <span className="font-mono font-semibold">{log.method}</span>
              </td>
              <td className="py-1.5 pr-3 max-w-[200px] truncate">
                <code className="text-foreground/70">{log.path}</code>
              </td>
              <td className="py-1.5 pr-3">
                <Badge
                  variant={
                    log.status_code >= 500
                      ? "destructive"
                      : log.status_code >= 400
                      ? "outline"
                      : "secondary"
                  }
                  className="text-xs"
                >
                  {log.status_code}
                </Badge>
              </td>
              <td className="py-1.5 text-muted-foreground">{log.duration_ms}ms</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
