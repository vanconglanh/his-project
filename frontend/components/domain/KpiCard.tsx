"use client";

import { TrendingUp, TrendingDown, Minus } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";

interface Props {
  title: string;
  value: string | number;
  icon: React.ElementType;
  delta?: number | null;
  deltaLabel?: string;
  loading?: boolean;
  className?: string;
}

export function KpiCard({ title, value, icon: Icon, delta, deltaLabel, loading, className }: Props) {
  const isPositive = delta !== undefined && delta !== null && delta > 0;
  const isNegative = delta !== undefined && delta !== null && delta < 0;

  return (
    <Card className={cn("hover:shadow-md transition-shadow", className)}>
      <CardHeader className="flex flex-row items-center justify-between pb-2">
        <CardTitle className="text-sm font-medium text-muted-foreground">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground shrink-0" />
      </CardHeader>
      <CardContent>
        {loading ? (
          <>
            <Skeleton className="h-8 w-28 mb-1" />
            <Skeleton className="h-4 w-20" />
          </>
        ) : (
          <>
            <div className="text-2xl font-bold tabular-nums">{value}</div>
            {delta !== undefined && delta !== null && (
              <div
                className={cn(
                  "flex items-center gap-1 text-xs mt-1",
                  isPositive && "text-emerald-600",
                  isNegative && "text-red-500",
                  !isPositive && !isNegative && "text-muted-foreground"
                )}
              >
                {isPositive ? (
                  <TrendingUp className="h-3 w-3" />
                ) : isNegative ? (
                  <TrendingDown className="h-3 w-3" />
                ) : (
                  <Minus className="h-3 w-3" />
                )}
                <span>
                  {isPositive ? "+" : ""}
                  {delta.toFixed(1)}% {deltaLabel ?? "so hôm qua"}
                </span>
              </div>
            )}
          </>
        )}
      </CardContent>
    </Card>
  );
}
