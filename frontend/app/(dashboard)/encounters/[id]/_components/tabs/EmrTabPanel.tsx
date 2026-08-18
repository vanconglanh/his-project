"use client";

import { useCallback, useState } from "react";
import { CheckCircle } from "lucide-react";
import { EmrEditor } from "@/components/domain/EmrEditor";
import { EmrTemplateSelector } from "@/components/domain/EmrTemplateSelector";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { useEmr } from "@/lib/hooks/use-emr";
import { formatVnTime } from "@/lib/utils/encounter-format";
import type { EmrTemplateResponse } from "@/lib/api/types";

interface Props {
  encounterId: string;
  canEdit: boolean;
}

export function EmrTabPanel({ encounterId, canEdit }: Props) {
  const { data: emr } = useEmr(encounterId);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | undefined>();
  const [templateContent, setTemplateContent] = useState<Record<string, unknown> | undefined>();
  const [lastSaved, setLastSaved] = useState<Date | null>(null);

  const isSigned = !!emr?.signed_at;

  const handleTemplateSelect = useCallback((template: EmrTemplateResponse) => {
    setTemplateContent(template.content_json);
    setSelectedTemplateId(template.id);
  }, []);

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        {canEdit && !isSigned && <EmrTemplateSelector onSelect={handleTemplateSelect} />}
        {isSigned && (
          <HisStatusBadge variant="done">
            {emr?.signed_by_name ? `Đã ký số — ${emr.signed_by_name}` : "Đã ký số"}
          </HisStatusBadge>
        )}
        {!isSigned && (
          <span className="ml-auto text-xs text-muted-foreground">
            {lastSaved ? `Đã lưu lúc ${formatVnTime(lastSaved.toISOString())}` : "Chưa lưu"}
          </span>
        )}
      </div>

      {isSigned && (
        <p className="flex items-center gap-1.5 text-xs text-[color:var(--status-done)]">
          <CheckCircle className="h-3.5 w-3.5" aria-hidden="true" />
          Bệnh án đã ký số, nội dung không thể chỉnh sửa.
        </p>
      )}

      <EmrEditor
        encounterId={encounterId}
        initialContent={templateContent ?? emr?.content_json}
        isSigned={isSigned || !canEdit}
        onSaved={setLastSaved}
        templateId={selectedTemplateId}
      />
    </div>
  );
}
