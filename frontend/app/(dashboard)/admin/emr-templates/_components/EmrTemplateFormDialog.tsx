"use client";

import { useEffect, useRef } from "react";
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
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription,
  DialogFooter,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Separator } from "@/components/ui/separator";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { cn } from "@/lib/utils";
import { useCreateEmrTemplate, useUpdateEmrTemplate } from "@/lib/hooks/use-emr";
import type { EmrTemplateResponse, EmrTemplateSpeciality } from "@/lib/api/types";

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
  open: boolean;
  onOpenChange: (open: boolean) => void;
  /** Truyền vào khi ở chế độ Sửa; để trống khi Tạo mới */
  template?: EmrTemplateResponse | null;
}

export function EmrTemplateFormDialog({ open, onOpenChange, template }: Props) {
  const isEdit = !!template;
  const createTemplate = useCreateEmrTemplate();
  const updateTemplate = useUpdateEmrTemplate(template?.id ?? "");
  const isPending = createTemplate.isPending || updateTemplate.isPending;

  const { register, handleSubmit, reset, setValue, watch, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: { name: "", speciality: "GENERAL" },
  });
  const speciality = watch("speciality");

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
    content: EMPTY_DOC,
  });
  const contentRef = useRef(EMPTY_DOC as Record<string, unknown>);

  // Reset form + nội dung mỗi khi mở dialog hoặc đổi mẫu đang sửa
  useEffect(() => {
    if (!open) return;
    reset({
      name: template?.name ?? "",
      speciality: (template?.speciality as EmrTemplateSpeciality) ?? "GENERAL",
    });
    const initial = (template?.content_json as Record<string, unknown>) ?? EMPTY_DOC;
    contentRef.current = initial;
    if (editor && !editor.isDestroyed) {
      editor.commands.setContent(initial);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, template, editor]);

  async function onSubmit(data: FormData) {
    const content_json = (editor?.getJSON() as Record<string, unknown>) ?? EMPTY_DOC;
    const payload = { name: data.name, speciality: data.speciality, content_json };

    if (isEdit && template) {
      await updateTemplate.mutateAsync(payload);
    } else {
      await createTemplate.mutateAsync(payload);
    }
    onOpenChange(false);
  }

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent fullScreen>
        <DialogHeader>
          <DialogTitle>{isEdit ? "Sửa mẫu bệnh án" : "Tạo mẫu bệnh án mới"}</DialogTitle>
          <DialogDescription>
            {isEdit
              ? "Cập nhật thông tin và nội dung mẫu bệnh án."
              : "Soạn mẫu bệnh án dùng lại khi khám bệnh."}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit(onSubmit)} className="flex flex-1 flex-col gap-4 overflow-y-auto">
          <div className="grid grid-cols-2 gap-4">
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

          <DialogFooter>
            <Button type="button" variant="outline" onClick={() => onOpenChange(false)} disabled={isPending}>
              Hủy
            </Button>
            <Button type="submit" disabled={isPending}>
              {isPending ? "Đang lưu..." : isEdit ? "Cập nhật" : "Tạo mẫu"}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
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
