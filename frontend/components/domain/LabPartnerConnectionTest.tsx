"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { useTestLabPartnerConnection } from "@/lib/hooks/use-lab-partners";
import type { ConnectionTestResult } from "@/lib/api/lab-partners";

interface LabPartnerConnectionTestProps {
  partnerId: string;
}

export function LabPartnerConnectionTest({ partnerId }: LabPartnerConnectionTestProps) {
  const [result, setResult] = useState<ConnectionTestResult | null>(null);
  const testMutation = useTestLabPartnerConnection();

  const handleTest = async () => {
    setResult(null);
    const res = await testMutation.mutateAsync(partnerId);
    setResult(res);
  };

  return (
    <div className="flex items-center gap-3">
      <Button
        variant="outline"
        size="sm"
        onClick={handleTest}
        disabled={testMutation.isPending}
        aria-label="Kiểm tra kết nối"
      >
        {testMutation.isPending ? "Đang kiểm tra..." : "Test kết nối"}
      </Button>

      {result && (
        <div
          className={cn(
            "flex items-center gap-1.5 text-sm font-medium",
            result.ok ? "text-green-600" : "text-red-600"
          )}
          aria-live="polite"
        >
          <span
            className={cn(
              "inline-block w-2.5 h-2.5 rounded-full",
              result.ok ? "bg-green-500" : "bg-red-500"
            )}
          />
          {result.ok ? `OK (${result.latency_ms}ms)` : `Lỗi: ${result.message}`}
        </div>
      )}
    </div>
  );
}
