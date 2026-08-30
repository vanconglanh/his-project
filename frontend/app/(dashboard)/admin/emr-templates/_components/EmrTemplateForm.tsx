"use client";

import { useEffect, useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import { Table } from "@tiptap/extension-table";
import { TableRow } from "@tiptap/extension-table-row";
import { TableCell } from "@tiptap/extension-table-cell";
import { TableHeader } from "@tiptap/extension-table-header";
import Image from "@tiptap/extension-image";
import Placeholder from "@tiptap/extension-placeholder";
import {
  Bold,
  Italic,
  List,
  ListOrdered,
  Quote,
  TableIcon,
  ImageIcon,
  Heading1,
  Heading2,
  Minus,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Separator } from "@/components/ui/separator";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { DynamicFormRenderer } from "@/components/emr/DynamicFormRenderer";
import { Eye, EyeOff } from "lucide-react";
import { cn } from "@/lib/utils";
import type {
  EmrTemplateResponse,
  EmrTemplateRequest,
  EmrTemplateSpeciality,
  EmrFormSchema,
  EmrFormFieldType,
} from "@/lib/api/types";

const VALID_FIELD_TYPES: EmrFormFieldType[] = [
  "text",
  "number",
  "textarea",
  "select",
  "checkbox",
  "checklist",
  "date",
  "radio",
];

/**
 * Validate structuredJson nhập tay ở màn admin (MVP — chưa có kéo-thả).
 * Trả về schema hợp lệ hoặc throw Error với thông báo tiếng Việt cụ thể.
 */
function parseStructuredJson(raw: string): EmrFormSchema | null {
  const trimmed = raw.trim();
  if (!trimmed) return null;

  let parsed: unknown;
  try {
    parsed = JSON.parse(trimmed);
  } catch {
    throw new Error("Cú pháp JSON không hợp lệ");
  }

  if (!Array.isArray(parsed)) {
    throw new Error("structuredJson phải là một mảng (array) các field");
  }

  parsed.forEach((item, idx) => {
    if (!item || typeof item !== "object") {
      throw new Error(`Field thứ ${idx + 1}: phải là object`);
    }
    const field = item as Record<string, unknown>;
    if (typeof field.key !== "string" || !field.key.trim()) {
      throw new Error(`Field thứ ${idx + 1}: thiếu "key" hoặc "key" không phải chuỗi`);
    }
    if (typeof field.label !== "string" || !field.label.trim()) {
      throw new Error(`Field "${field.key}": thiếu "label" hoặc "label" không phải chuỗi`);
    }
    if (typeof field.type !== "string" || !VALID_FIELD_TYPES.includes(field.type as EmrFormFieldType)) {
      throw new Error(
        `Field "${field.key}": "type" không hợp lệ. Chỉ chấp nhận: ${VALID_FIELD_TYPES.join(", ")}`
      );
    }
    if (["select", "radio", "checklist"].includes(field.type as string) && field.options !== undefined) {
      if (!Array.isArray(field.options)) {
        throw new Error(`Field "${field.key}": "options" phải là mảng`);
      }
    }
  });

  return parsed as EmrFormSchema;
}

const SPECIALITY_LABELS: Record<EmrTemplateSpeciality, string> = {
  GENERAL: "Đa khoa",
  DIABETES: "Đái tháo đường",
  CARDIOLOGY: "Tim mạch",
  ENDOCRINOLOGY: "Nội tiết",
  NEPHROLOGY: "Thận",
  OPHTHALMOLOGY: "Mắt",
  OTHER: "Khác",
};

const EMPTY_DOC = { type: "doc", content: [] };

const schema = z.object({
  name: z.string().min(1, "Vui lòng nhập tên mẫu"),
  speciality: z.enum([
    "GENERAL",
    "DIABETES",
    "CARDIOLOGY",
    "ENDOCRINOLOGY",
    "NEPHROLOGY",
    "OPHTHALMOLOGY",
    "OTHER",
  ]),
});

type FormData = z.infer<typeof schema>;

interface Props {
  /** id gắn vào <form> để FullPageFormShell trigger submit từ ngoài */
  formId: string;
  template?: EmrTemplateResponse | null;
  onSubmit: (payload: EmrTemplateRequest) => void;
}

export function EmrTemplateForm({ formId, template, onSubmit }: Props) {
  const { register, handleSubmit, setValue, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      name: template?.name ?? "",
      speciality: (template?.speciality as EmrTemplateSpeciality) ?? "GENERAL",
    },
  });
  const speciality = watch("speciality");

  const [structuredJsonText, setStructuredJsonText] = useState(
    template?.structured_json ? JSON.stringify(template.structured_json, null, 2) : ""
  );
  const [structuredJsonError, setStructuredJsonError] = useState<string | null>(null);
  const [previewSchema, setPreviewSchema] = useState<EmrFormSchema | null>(null);

  function handlePreviewToggle() {
    if (previewSchema) {
      setPreviewSchema(null);
      return;
    }
    try {
      const schema = parseStructuredJson(structuredJsonText);
      setStructuredJsonError(null);
      setPreviewSchema(schema ?? []);
    } catch (err) {
      setStructuredJsonError(err instanceof Error ? err.message : "structuredJson không hợp lệ");
      setPreviewSchema(null);
    }
  }

  const editor = useEditor({
    extensions: [
      StarterKit,
      Table.configure({ resizable: true }),
      TableRow,
      TableCell,
      TableHeader,
      Image,
      Placeholder.configure({ placeholder: "Soạn nội dung mẫu bệnh án..." }),
    ],
    content: (template?.content_json as Record<string, unknown>) ?? EMPTY_DOC,
  });
  const seededRef = useRef(false);

  // Đổ nội dung ban đầu vào editor một lần khi editor sẵn sàng
  useEffect(() => {
    if (!editor || editor.isDestroyed || seededRef.current) return;
    const initial = (template?.content_json as Record<string, unknown>) ?? EMPTY_DOC;
    editor.commands.setContent(initial);
    seededRef.current = true;
  }, [editor, template]);

  function handleSubmitForm(data: FormData) {
    let structured_json: EmrFormSchema | null = null;
    try {
      structured_json = parseStructuredJson(structuredJsonText);
      setStructuredJsonError(null);
    } catch (err) {
      setStructuredJsonError(err instanceof Error ? err.message : "structuredJson không hợp lệ");
      return; // chặn submit nếu JSON sai
    }
    const content_json = (editor?.getJSON() as Record<string, unknown>) ?? EMPTY_DOC;
    onSubmit({ name: data.name, speciality: data.speciality, content_json, structured_json });
  }

  return (
    <form
      id={formId}
      onSubmit={handleSubmit(handleSubmitForm)}
      className="flex flex-1 flex-col gap-4"
    >
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 max-w-2xl">
        <div className="space-y-1">
          <Label htmlFor="name">
            Tên mẫu <span className="text-destructive">*</span>
          </Label>
          <Input
            id="name"
            placeholder="VD: Khám tổng quát đái tháo đường"
            {...register("name")}
            aria-invalid={!!errors.name}
          />
          {errors.name && <p className="text-xs text-destructive">{errors.name.message}</p>}
        </div>
        <div className="space-y-1">
          <Label htmlFor="speciality">Chuyên khoa</Label>
          <Select
            value={speciality}
            onValueChange={(v) => setValue("speciality", (v ?? "GENERAL") as EmrTemplateSpeciality)}
          >
            <SelectTrigger id="speciality">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {Object.entries(SPECIALITY_LABELS).map(([value, label]) => (
                <SelectItem key={value} value={value}>
                  {label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      <div className="flex flex-1 flex-col gap-2 min-h-[300px]">
        <Label>Nội dung mẫu</Label>
        <TemplateEditorToolbar editor={editor} />
        <div className="flex-1 min-h-[240px] rounded-md border bg-background p-4 prose prose-sm max-w-none dark:prose-invert focus-within:ring-2 focus-within:ring-ring overflow-y-auto">
          <EditorContent editor={editor} />
        </div>
      </div>

      <div className="flex flex-col gap-2 max-w-2xl">
        <div className="flex items-center justify-between">
          <Label htmlFor="structured-json">Biểu mẫu có cấu trúc (structuredJson) — tùy chọn</Label>
          <Button
            type="button"
            variant="outline"
            size="sm"
            className="gap-1.5"
            onClick={handlePreviewToggle}
          >
            {previewSchema ? <EyeOff className="h-3.5 w-3.5" /> : <Eye className="h-3.5 w-3.5" />}
            {previewSchema ? "Ẩn xem trước" : "Xem trước"}
          </Button>
        </div>
        <p className="text-xs text-muted-foreground">
          Nhập JSON dạng mảng field, ví dụ:{" "}
          <code className="text-[11px]">
            {`[{"key":"chief_complaint","label":"Lý do khám","type":"text","required":true}]`}
          </code>
          . Để trống nếu mẫu này không cần biểu mẫu có cấu trúc.
        </p>
        <Textarea
          id="structured-json"
          value={structuredJsonText}
          onChange={(e) => {
            setStructuredJsonText(e.target.value);
            setStructuredJsonError(null);
            setPreviewSchema(null);
          }}
          placeholder='[{"key":"chief_complaint","label":"Lý do khám","type":"text","required":true}]'
          className="min-h-[140px] font-mono text-xs"
        />
        {structuredJsonError && (
          <p className="text-xs text-destructive">{structuredJsonError}</p>
        )}
        {previewSchema && previewSchema.length > 0 && (
          <div className="rounded-lg border bg-muted/30 p-4">
            <p className="mb-3 text-xs font-medium text-muted-foreground">Xem trước biểu mẫu</p>
            <DynamicFormRenderer schema={previewSchema} values={{}} onChange={() => {}} readOnly />
          </div>
        )}
        {previewSchema && previewSchema.length === 0 && (
          <p className="text-xs text-muted-foreground italic">Chưa có field nào để xem trước.</p>
        )}
      </div>
    </form>
  );
}

function TemplateEditorToolbar({ editor }: { editor: ReturnType<typeof useEditor> }) {
  if (!editor) return null;
  return (
    <div className="flex flex-wrap items-center gap-1 rounded-md border bg-muted/40 p-1.5">
      <ToolbarButton onClick={() => editor.chain().focus().toggleBold().run()} active={editor.isActive("bold")} title="Đậm">
        <Bold className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton onClick={() => editor.chain().focus().toggleItalic().run()} active={editor.isActive("italic")} title="Nghiêng">
        <Italic className="h-4 w-4" />
      </ToolbarButton>
      <Separator orientation="vertical" className="mx-1 h-5" />
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 1 }).run()}
        active={editor.isActive("heading", { level: 1 })}
        title="Tiêu đề 1"
      >
        <Heading1 className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleHeading({ level: 2 }).run()}
        active={editor.isActive("heading", { level: 2 })}
        title="Tiêu đề 2"
      >
        <Heading2 className="h-4 w-4" />
      </ToolbarButton>
      <Separator orientation="vertical" className="mx-1 h-5" />
      <ToolbarButton onClick={() => editor.chain().focus().toggleBulletList().run()} active={editor.isActive("bulletList")} title="Danh sách">
        <List className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => editor.chain().focus().toggleOrderedList().run()}
        active={editor.isActive("orderedList")}
        title="Danh sách số"
      >
        <ListOrdered className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton onClick={() => editor.chain().focus().toggleBlockquote().run()} active={editor.isActive("blockquote")} title="Trích dẫn">
        <Quote className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton onClick={() => editor.chain().focus().setHorizontalRule().run()} title="Kẻ ngang">
        <Minus className="h-4 w-4" />
      </ToolbarButton>
      <Separator orientation="vertical" className="mx-1 h-5" />
      <ToolbarButton
        onClick={() => editor.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()}
        title="Chèn bảng"
      >
        <TableIcon className="h-4 w-4" />
      </ToolbarButton>
      <ToolbarButton
        onClick={() => {
          const url = prompt("Nhập URL ảnh:");
          if (url) editor.chain().focus().setImage({ src: url }).run();
        }}
        title="Chèn ảnh"
      >
        <ImageIcon className="h-4 w-4" />
      </ToolbarButton>
    </div>
  );
}

function ToolbarButton({
  children,
  onClick,
  active,
  title,
}: {
  children: React.ReactNode;
  onClick?: () => void;
  active?: boolean;
  title?: string;
}) {
  return (
    <Button
      type="button"
      variant={active ? "secondary" : "ghost"}
      size="sm"
      onClick={onClick}
      title={title}
      className={cn("h-8 w-8 p-0")}
      aria-label={title}
    >
      {children}
    </Button>
  );
}
