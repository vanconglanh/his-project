"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";

interface ErrorCardProps {
  title: string;
  description: string;
  /** Hành động khi bấm "Thử lại". Mặc định reload cả trang. */
  onRetry?: () => void;
}

export function ErrorCard({ title, description, onRetry }: ErrorCardProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-muted px-4 print:hidden">
      <Card className="max-w-md w-full">
        <CardContent className="p-8 text-center">
          <div className="mx-auto mb-4 w-12 h-12 rounded-full bg-[color:var(--status-warning)]/10 flex items-center justify-center">
            <AlertTriangle className="w-6 h-6 text-[color:var(--status-warning)]" />
          </div>
          <h2 className="text-lg font-semibold text-foreground mb-2">{title}</h2>
          <p className="text-sm text-muted-foreground mb-6">{description}</p>
          <Button type="button" onClick={onRetry ?? (() => location.reload())}>
            <RefreshCw className="w-4 h-4" />
            Thử lại
          </Button>
        </CardContent>
      </Card>
    </div>
  );
}
