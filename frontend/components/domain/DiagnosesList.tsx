"use client";

import { Trash2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { DiagnosisResponse } from "@/lib/api/types";
import { ConfirmDialog } from "./ConfirmDialog";
import { useState } from "react";

interface Props {
  diagnoses: DiagnosisResponse[];
  onDelete?: (id: string) => void;
  readOnly?: boolean;
}

export function DiagnosesList({ diagnoses, onDelete, readOnly }: Props) {
  const [pendingDelete, setPendingDelete] = useState<string | null>(null);

  if (diagnoses.length === 0) {
    return (
      <p className="text-sm text-muted-foreground italic">Chưa có chẩn đoán</p>
    );
  }

  return (
    <>
      <ul className="space-y-2">
        {diagnoses.map((d) => (
          <li
            key={d.id}
            className="flex items-start gap-3 p-3 rounded-lg border bg-card"
          >
            <div className="flex-1 min-w-0">
              <div className="flex items-center gap-2 flex-wrap">
                <Badge
                  variant={d.type === "PRIMARY" ? "default" : "outline"}
                  className="text-xs"
                >
                  {d.type === "PRIMARY" ? "Chính" : "Phụ"}
                </Badge>
                <span className="font-mono text-sm font-semibold text-primary">
                  {d.icd10_code}
                </span>
              </div>
              <p className="mt-0.5 text-sm">{d.name}</p>
              {d.note && (
                <p className="mt-0.5 text-xs text-muted-foreground">{d.note}</p>
              )}
            </div>
            {!readOnly && onDelete && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 text-destructive hover:text-destructive shrink-0"
                onClick={() => setPendingDelete(d.id)}
                aria-label="Xóa chẩn đoán"
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </li>
        ))}
      </ul>

      <ConfirmDialog
        open={!!pendingDelete}
        onOpenChange={(v) => { if (!v) setPendingDelete(null); }}
        title="Xóa chẩn đoán"
        description="Bạn có chắc muốn xóa chẩn đoán này?"
        onConfirm={() => {
          if (pendingDelete && onDelete) onDelete(pendingDelete);
          setPendingDelete(null);
        }}
        variant="destructive"
      />
    </>
  );
}
