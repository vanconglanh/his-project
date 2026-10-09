"use client";

import { useCallback, useEffect, useMemo, useState, useRef } from "react";
import { CheckCircle } from "lucide-react";
import { EmrEditor } from "@/components/domain/EmrEditor";
import { EmrTemplateSelector } from "@/components/domain/EmrTemplateSelector";
import { DynamicFormRenderer, validateRequiredFields } from "@/components/emr/DynamicFormRenderer";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { useEmr } from "@/lib/hooks/use-emr";
import { formatVnTime } from "@/lib/utils/encounter-format";
import type { EmrFormSchema, EmrTemplateResponse } from "@/lib/api/types";

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
  // BM-35 (tiep, Lark Bug-Feedback): truoc day schema cua mau MOI chon lay tu 1 API fetch rieng
  // (useEmrTemplate) chay bat dong bo - co 1 khoang thoi gian ngan sau khi chon mau nhung TRUOC
  // khi fetch xong, activeSchema con null nen EmrEditor tam thoi nap templateContent (rich-text
  // lap tieu de) lam initialContent; sau khi schema ve, initialContent doi thanh undefined nhung
  // useEffect cua EmrEditor chi goi setContent khi initialContent truthy nen noi dung lap khong
  // bao gio duoc xoa - roi bi autosave luu de vao content_json. Object tra ve tu danh sach mau
  // (EmrTemplateSelector) da co san structured_json, KHONG can fetch lai - dung truc tiep de
  // loai bo hoan toan khoang ho dong bo nay.
  const [pickedTemplateThisSession, setPickedTemplateThisSession] = useState(false);
  // BM-64: chỉ hiện lỗi required sau khi bác sĩ cố save/sign lần đầu
  const [showRequiredErrors, setShowRequiredErrors] = useState(false);
  const [selectedTemplateSchema, setSelectedTemplateSchema] = useState<EmrFormSchema | null>(null);
  const [structuredValues, setStructuredValues] = useState<Record<string, unknown>>({});

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
    setSelectedTemplateSchema(template.structured_json ?? null);
    setPickedTemplateThisSession(true);
    setStructuredValues({});
    setShowRequiredErrors(false); // reset lỗi khi chọn mẫu mới
  }, []);

  const handleFieldChange = useCallback((key: string, value: unknown) => {
    setStructuredValues((prev) => ({ ...prev, [key]: value }));
  }, []);

  // BM-64: validate trường required trước khi lưu; chỉ validate khi có schema
  const handleBeforeSave = useCallback((): boolean => {
    if (!activeSchema || activeSchema.length === 0) return true;
    const missing = validateRequiredFields(activeSchema, structuredValues);
    if (missing.length > 0) {
      setShowRequiredErrors(true);
      return false;
    }
    return true;
  }, [activeSchema, structuredValues]);

  // Bệnh án đã ký / đã lưu trước đó: LUÔN render theo schema_snapshot của
  // chính bản ghi đó, không đọc lại structured_json hiện tại của template
  // (§5.8.2 — nguyên tắc bắt buộc, tránh sai lệch nội dung sau khi ký).
  const activeSchema = useMemo(() => {
    if (pickedTemplateThisSession && !isSigned) {
      return selectedTemplateSchema;
    }
    return emr?.schema_snapshot ?? null;
  }, [pickedTemplateThisSession, isSigned, selectedTemplateSchema, emr?.schema_snapshot]);

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
            showRequiredErrors={showRequiredErrors}
          />
        </div>
      )}

      <EmrEditor
        encounterId={encounterId}
        // BM-35 (Lark Bug-Feedback): khi da co form co cau truc (activeSchema), truoc day
        // EmrEditor van nap templateContent - la ban rich-text cua CUNG mau, lap lai y het cac
        // tieu de (Ly do kham/Tien su/Kham lam sang/Can lam sang/Chan doan/Huong xu tri) da co
        // trong DynamicFormRenderer o tren, chi khac la luon rong -> nhin nhu thua thong tin o
        // cuoi man hinh. Khi co activeSchema, khoi tao EmrEditor RONG (chi con la khu vuc ghi
        // chu them tu do, khong lap tieu de) - van giu duoc autosave/nut "Luu nhap" gui kem
        // structured_values vi day la co che luu duy nhat cho DynamicFormRenderer.
        initialContent={activeSchema && activeSchema.length > 0 ? emr?.content_json : (templateContent ?? emr?.content_json)}
        isSigned={isSigned || !canEdit}
        onSaved={setLastSaved}
        onBeforeSave={handleBeforeSave}
        templateId={selectedTemplateId}
        structuredValues={activeSchema ? structuredValues : undefined}
      />
    </div>
  );
}
