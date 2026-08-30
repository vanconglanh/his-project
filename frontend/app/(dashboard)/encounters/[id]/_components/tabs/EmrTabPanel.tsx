"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { CheckCircle } from "lucide-react";
import { EmrEditor } from "@/components/domain/EmrEditor";
import { EmrTemplateSelector } from "@/components/domain/EmrTemplateSelector";
import { DynamicFormRenderer } from "@/components/emr/DynamicFormRenderer";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { useEmr, useEmrTemplate } from "@/lib/hooks/use-emr";
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

  // Bác sĩ chọn mẫu mới trong phiên khám này -> nạp structured_json của
  // chính mẫu đó (KHÔNG dùng schema_snapshot cũ). Nếu chưa chọn mẫu nào
  // trong phiên này thì dùng schema_snapshot đã lưu trên bệnh án hiện hành.
  const [pickedTemplateThisSession, setPickedTemplateThisSession] = useState(false);
  const [structuredValues, setStructuredValues] = useState<Record<string, unknown>>({});
  const { data: selectedTemplateDetail } = useEmrTemplate(
    pickedTemplateThisSession ? selectedTemplateId : undefined
  );

  const isSigned = !!emr?.signed_at;

  // Nạp lại giá trị đã lưu (structured_values) khi mở tab / dữ liệu EMR về,
  // miễn là bác sĩ chưa tự chọn mẫu khác trong phiên hiện tại.
  useEffect(() => {
    if (!pickedTemplateThisSession) {
      setStructuredValues(emr?.structured_values ?? {});
    }
  }, [emr?.structured_values, pickedTemplateThisSession]);

  const handleTemplateSelect = useCallback((template: EmrTemplateResponse) => {
    setTemplateContent(template.content_json);
    setSelectedTemplateId(template.id);
    setPickedTemplateThisSession(true);
    setStructuredValues({});
  }, []);

  const handleFieldChange = useCallback((key: string, value: unknown) => {
    setStructuredValues((prev) => ({ ...prev, [key]: value }));
  }, []);

  // Bệnh án đã ký / đã lưu trước đó: LUÔN render theo schema_snapshot của
  // chính bản ghi đó, không đọc lại structured_json hiện tại của template
  // (§5.8.2 — nguyên tắc bắt buộc, tránh sai lệch nội dung sau khi ký).
  const activeSchema = useMemo(() => {
    if (pickedTemplateThisSession && !isSigned) {
      return selectedTemplateDetail?.structured_json ?? null;
    }
    return emr?.schema_snapshot ?? null;
  }, [pickedTemplateThisSession, isSigned, selectedTemplateDetail, emr?.schema_snapshot]);

  const showLegacyNotice = isSigned && !emr?.schema_snapshot;

  return (
    <div className="space-y-3">
      <div className="flex flex-wrap items-center gap-2">
        {canEdit && !isSigned && (
          <div data-tour="enc-emr-template">
            <EmrTemplateSelector onSelect={handleTemplateSelect} />
          </div>
        )}
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

      {showLegacyNotice && (
        <p className="text-xs italic text-muted-foreground">
          Bệnh án tạo trước 30/08/2026 — không có dữ liệu biểu mẫu có cấu trúc.
        </p>
      )}

      {activeSchema && activeSchema.length > 0 && (
        <div className="rounded-lg border bg-card p-4">
          <h3 className="mb-3 text-sm font-semibold">Thông tin biểu mẫu có cấu trúc</h3>
          <DynamicFormRenderer
            schema={activeSchema}
            values={structuredValues}
            onChange={handleFieldChange}
            readOnly={isSigned || !canEdit}
          />
        </div>
      )}

      <EmrEditor
        encounterId={encounterId}
        initialContent={templateContent ?? emr?.content_json}
        isSigned={isSigned || !canEdit}
        onSaved={setLastSaved}
        templateId={selectedTemplateId}
        structuredValues={activeSchema ? structuredValues : undefined}
      />
    </div>
  );
}
