"use client";

import { useState } from "react";
import { Copy, Check, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

interface ApiPartnerKeyDisplayProps {
  apiKey: string;
  className?: string;
}

export function ApiPartnerKeyDisplay({ apiKey, className }: ApiPartnerKeyDisplayProps) {
  const [copied, setCopied] = useState(false);

  async function handleCopy() {
    try {
      await navigator.clipboard.writeText(apiKey);
      setCopied(true);
      toast.success("Đã sao chép API key");
      setTimeout(() => setCopied(false), 3000);
    } catch {
      toast.error("Không thể sao chép, vui lòng copy thủ công");
    }
  }

  return (
    <div className={cn("space-y-3", className)}>
      <Alert className="border-amber-500 bg-amber-50 text-amber-900 dark:bg-amber-950 dark:text-amber-100">
        <AlertTriangle className="h-4 w-4" />
        <AlertDescription className="font-semibold">
          Copy ngay! API key chỉ hiển thị một lần duy nhất và không thể lấy lại.
        </AlertDescription>
      </Alert>

      <div className="flex items-center gap-2 rounded-lg border border-border bg-muted/50 p-3">
        <code className="flex-1 break-all font-mono text-sm select-all">{apiKey}</code>
        <Button
          variant="outline"
          size="sm"
          onClick={handleCopy}
          className="shrink-0 min-w-[100px]"
          aria-label="Sao chép API key"
        >
          {copied ? (
            <>
              <Check className="mr-1.5 h-3.5 w-3.5 text-green-600" />
              Đã copy
            </>
          ) : (
            <>
              <Copy className="mr-1.5 h-3.5 w-3.5" />
              Copy
            </>
          )}
        </Button>
      </div>
    </div>
  );
}
