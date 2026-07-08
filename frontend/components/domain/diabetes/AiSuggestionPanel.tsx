"use client";

import { useState } from "react";
import { Sparkles, ThumbsUp, ThumbsDown, Pencil, AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Separator } from "@/components/ui/separator";
import { Can } from "@/components/auth/Can";
import { useGenerateTreatmentSuggestion, useUpdateAiSuggestionStatus } from "@/lib/hooks/use-ai-suggestion";
import type { TreatmentSuggestionResponse } from "@/lib/api/types";

interface Props {
  patientId: string;
  encounterId?: string;
}

export function AiSuggestionPanel({ patientId, encounterId }: Props) {
  const [suggestion, setSuggestion] = useState<TreatmentSuggestionResponse | null>(null);
  const [decidedStatus, setDecidedStatus] = useState<"ACCEPTED" | "REJECTED" | "EDITED" | null>(null);
  const generate = useGenerateTreatmentSuggestion(patientId);
  const updateStatus = useUpdateAiSuggestionStatus();

  function handleGenerate() {
    setDecidedStatus(null);
    generate.mutate(
      { encounter_id: encounterId },
      {
        onSuccess: (data) => setSuggestion(data),
      }
    );
  }

  function handleDecision(status: "ACCEPTED" | "REJECTED" | "EDITED") {
    if (!suggestion) return;
    updateStatus.mutate(
      { logId: suggestion.log_id, body: { status } },
      { onSuccess: () => setDecidedStatus(status) }
    );
  }

  return (
    <Can permission="ai.suggest">
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle className="text-base flex items-center gap-2">
            <Sparkles className="h-4 w-4 text-primary" />
            Gợi ý điều chỉnh điều trị (AI)
          </CardTitle>
          <Button size="sm" onClick={handleGenerate} disabled={generate.isPending}>
            {generate.isPending ? "Đang tạo gợi ý..." : "Gợi ý điều chỉnh (tham khảo)"}
          </Button>
        </CardHeader>
        <CardContent className="space-y-4">
          {!suggestion && !generate.isPending && (
            <p className="text-sm text-muted-foreground">
              Nhấn nút &quot;Gợi ý điều chỉnh&quot; để nhận đề xuất tham khảo dựa trên phác đồ điều trị ĐTĐ hiện tại của bệnh nhân.
            </p>
          )}

          {suggestion && (
            <div className="space-y-3">
              <Alert className="border-amber-300 bg-amber-50 dark:border-amber-800 dark:bg-amber-950/30">
                <AlertTriangle className="h-4 w-4 text-amber-600" />
                <AlertDescription className="font-medium text-amber-800 dark:text-amber-200">
                  {suggestion.disclaimer_text || "Gợi ý tham khảo — bác sĩ quyết định cuối cùng"}
                </AlertDescription>
              </Alert>

              {suggestion.fallback_used && (
                <Badge variant="secondary" className="text-[11px]">
                  Dùng gợi ý theo phác đồ (fallback) — AI không khả dụng
                </Badge>
              )}

              <div className="rounded-md border bg-muted/30 p-3 text-sm whitespace-pre-wrap">
                {suggestion.body_text}
              </div>

              {suggestion.rule_derived.length > 0 && (
                <div className="space-y-1.5">
                  <p className="text-xs font-medium text-muted-foreground uppercase tracking-wide">
                    Căn cứ theo phác đồ
                  </p>
                  <ul className="space-y-1">
                    {suggestion.rule_derived.map((rec, idx) => (
                      <li key={`${rec.code}-${idx}`} className="text-xs flex gap-2">
                        <span className="font-mono text-muted-foreground shrink-0">{rec.code}</span>
                        <span>
                          {rec.text} <span className="text-muted-foreground">({rec.source})</span>
                        </span>
                      </li>
                    ))}
                  </ul>
                </div>
              )}

              <Separator />

              {decidedStatus ? (
                <p className="text-sm text-muted-foreground">
                  Đã ghi nhận:{" "}
                  <span className="font-medium">
                    {decidedStatus === "ACCEPTED"
                      ? "Chấp nhận gợi ý"
                      : decidedStatus === "REJECTED"
                        ? "Từ chối gợi ý"
                        : "Đã sửa gợi ý"}
                  </span>
                </p>
              ) : (
                <div className="flex flex-wrap gap-2">
                  <Button
                    size="sm"
                    variant="outline"
                    className="gap-1.5"
                    onClick={() => handleDecision("ACCEPTED")}
                    disabled={updateStatus.isPending}
                  >
                    <ThumbsUp className="h-3.5 w-3.5" />
                    Chấp nhận
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    className="gap-1.5"
                    onClick={() => handleDecision("EDITED")}
                    disabled={updateStatus.isPending}
                  >
                    <Pencil className="h-3.5 w-3.5" />
                    Sửa
                  </Button>
                  <Button
                    size="sm"
                    variant="outline"
                    className="gap-1.5 text-destructive hover:text-destructive"
                    onClick={() => handleDecision("REJECTED")}
                    disabled={updateStatus.isPending}
                  >
                    <ThumbsDown className="h-3.5 w-3.5" />
                    Từ chối
                  </Button>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>
    </Can>
  );
}
