"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { useInboundRaw } from "@/lib/hooks/use-lab-integration";

interface WebhookLogViewerProps {
  inboundId: string;
}

export function WebhookLogViewer({ inboundId }: WebhookLogViewerProps) {
  const [expanded, setExpanded] = useState(false);
  const { data, isLoading } = useInboundRaw(inboundId);

  if (!expanded) {
    return (
      <Button variant="ghost" size="sm" onClick={() => setExpanded(true)}>
        Xem raw payload
      </Button>
    );
  }

  if (isLoading) return <Skeleton className="h-48 w-full rounded-md" />;

  if (!data) return null;

  return (
    <div className="space-y-3 text-xs">
      <div className="flex items-center justify-between">
        <p className="font-semibold text-sm">Raw Payload</p>
        <Button variant="ghost" size="sm" onClick={() => setExpanded(false)}>
          Thu gon
        </Button>
      </div>

      {data.raw_hl7_message && (
        <div>
          <p className="text-muted-foreground mb-1 font-medium">HL7 Message</p>
          <pre className="bg-muted rounded p-3 overflow-x-auto whitespace-pre-wrap text-xs font-mono">
            {data.raw_hl7_message}
          </pre>
        </div>
      )}

      {data.payload_json && (
        <div>
          <p className="text-muted-foreground mb-1 font-medium">JSON Payload</p>
          <pre className="bg-muted rounded p-3 overflow-x-auto whitespace-pre-wrap text-xs font-mono">
            {JSON.stringify(data.payload_json, null, 2)}
          </pre>
        </div>
      )}

      {data.headers && (
        <div>
          <p className="text-muted-foreground mb-1 font-medium">Headers</p>
          <pre className="bg-muted rounded p-3 overflow-x-auto whitespace-pre-wrap text-xs font-mono">
            {JSON.stringify(data.headers, null, 2)}
          </pre>
        </div>
      )}
    </div>
  );
}
