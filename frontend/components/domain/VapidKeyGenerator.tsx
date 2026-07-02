"use client";

import { ShieldCheck, ShieldOff, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { useVapidStatus, useGenerateVapidKey } from "@/lib/hooks/use-notifications";

export function VapidKeyGenerator() {
  const { data, isLoading } = useVapidStatus();
  const generateMutation = useGenerateVapidKey();

  if (isLoading) {
    return <div className="h-20 animate-pulse rounded-lg bg-muted" />;
  }

  return (
    <div className="rounded-lg border p-4 space-y-3">
      <div className="flex items-center gap-3">
        {data?.configured ? (
          <>
            <ShieldCheck className="h-6 w-6 text-green-600" />
            <div>
              <p className="font-medium">VAPID Key đã được cấu hình</p>
              {data.public_key && (
                <code className="text-xs text-muted-foreground">
                  {data.public_key.slice(0, 32)}...
                </code>
              )}
            </div>
          </>
        ) : (
          <>
            <ShieldOff className="h-6 w-6 text-amber-600" />
            <div>
              <p className="font-medium">Chưa có VAPID Key</p>
              <p className="text-sm text-muted-foreground">
                Cần tạo VAPID key để bật Web Push notifications
              </p>
            </div>
          </>
        )}
      </div>

      {data?.configured ? (
        <Alert>
          <AlertDescription className="text-sm">
            Đang hoạt động. Tạo lại VAPID key sẽ làm tất cả subscription hiện tại không nhận được
            thông báo.
          </AlertDescription>
        </Alert>
      ) : null}

      <Button
        variant={data?.configured ? "outline" : "default"}
        onClick={() => generateMutation.mutate()}
        disabled={generateMutation.isPending}
      >
        <RefreshCw className="mr-2 h-4 w-4" />
        {data?.configured ? "Tạo lại VAPID Key" : "Generate VAPID Key"}
      </Button>
    </div>
  );
}
