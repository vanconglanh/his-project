"use client";

import { Download, FlaskConical, TrendingUp } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { PortalLayout } from "@/components/domain/PortalLayout";
import { usePortalLabResults, useDownloadLabResultPdf } from "@/lib/hooks/use-portal";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

export default function PortalLabResultsPage() {
  const { data, isLoading } = usePortalLabResults();
  const downloadMutation = useDownloadLabResultPdf();

  const results = data?.data ?? [];

  // HbA1c results for trend
  const hba1cResults = results.filter((r) =>
    r.test_name?.toLowerCase().includes("hba1c")
  );

  return (
    <PortalLayout>
      <div className="space-y-5">
        <div>
          <h2 className="text-xl font-bold">Kết quả xét nghiệm</h2>
          <p className="text-sm text-muted-foreground">{results.length} kết quả</p>
        </div>

        {/* HbA1c trend note */}
        {hba1cResults.length > 0 && (
          <Card className="border-blue-200 bg-blue-50 dark:bg-blue-950 dark:border-blue-800">
            <CardHeader className="pb-2">
              <CardTitle className="text-sm flex items-center gap-2 text-blue-800 dark:text-blue-200">
                <TrendingUp className="h-4 w-4" />
                HbA1c ({hba1cResults.length} lần đo)
              </CardTitle>
            </CardHeader>
            <CardContent>
              <div className="flex flex-wrap gap-2">
                {hba1cResults.map((r) => (
                  <div key={r.id} className="text-center">
                    <p className="text-lg font-bold text-blue-800 dark:text-blue-200">
                      {r.value}
                      <span className="text-xs font-normal">{r.unit}</span>
                    </p>
                    <p className="text-xs text-blue-600 dark:text-blue-300">
                      {r.ordered_at && format(parseISO(r.ordered_at), "MM/yyyy")}
                    </p>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        )}

        {isLoading ? (
          <div className="space-y-3">
            {[...Array(4)].map((_, i) => (
              <div key={i} className="h-20 animate-pulse rounded-lg bg-muted" />
            ))}
          </div>
        ) : results.length === 0 ? (
          <div className="flex flex-col items-center py-16 text-center">
            <FlaskConical className="h-12 w-12 text-muted-foreground mb-4" />
            <p className="font-medium">Chưa có kết quả xét nghiệm</p>
          </div>
        ) : (
          <div className="divide-y rounded-lg border">
            {results.map((result) => (
              <div key={result.id} className="flex items-center gap-4 p-4">
                <div className="flex-1 min-w-0">
                  <p className="font-medium text-sm">{result.test_name}</p>
                  <p className="text-xs text-muted-foreground mt-0.5">
                    {result.ordered_at &&
                      format(parseISO(result.ordered_at), "dd/MM/yyyy", { locale: vi })}
                  </p>
                  {result.value && (
                    <p className="text-sm mt-1">
                      <span className="font-semibold">{result.value}</span>{" "}
                      {result.unit && (
                        <span className="text-muted-foreground text-xs">{result.unit}</span>
                      )}
                      {result.reference_range && (
                        <span className="text-xs text-muted-foreground ml-2">
                          (BT: {result.reference_range})
                        </span>
                      )}
                    </p>
                  )}
                </div>
                <div className="flex items-center gap-2 shrink-0">
                  <Badge
                    variant={result.status === "done" ? "secondary" : "outline"}
                    className="text-xs"
                  >
                    {result.status === "done" ? "Có kết quả" : "Chờ kết quả"}
                  </Badge>
                  {result.status === "done" && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => downloadMutation.mutate(result.id)}
                      disabled={downloadMutation.isPending}
                    >
                      <Download className="h-3.5 w-3.5" />
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </PortalLayout>
  );
}
