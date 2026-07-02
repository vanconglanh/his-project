"use client";

import { useEffect, useRef, useCallback } from "react";
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
  Undo,
  Redo,
  Heading1,
  Heading2,
  Minus,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { cn } from "@/lib/utils";
import { useSaveEmrDraft } from "@/lib/hooks/use-emr";
import { toast } from "sonner";
import { format } from "date-fns";
import { vi } from "date-fns/locale";

interface Props {
  encounterId: string;
  initialContent?: Record<string, unknown>;
  isSigned?: boolean;
  onSaved?: (savedAt: Date) => void;
  templateId?: string;
}

export function EmrEditor({ encounterId, initialContent, isSigned, onSaved, templateId }: Props) {
  const saveDraft = useSaveEmrDraft(encounterId);
  const lastSavedRef = useRef<Date | null>(null);
  const autoSaveTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  const editor = useEditor({
    extensions: [
      StarterKit,
      Table.configure({ resizable: true }),
      TableRow,
      TableCell,
      TableHeader,
      Image,
      Placeholder.configure({
        placeholder: "Ghi nội dung bệnh án...",
      }),
    ],
    content: initialContent ?? { type: "doc", content: [] },
    editable: !isSigned,
    onUpdate: () => {
      if (isSigned) return;
      if (autoSaveTimerRef.current) clearTimeout(autoSaveTimerRef.current);
      autoSaveTimerRef.current = setTimeout(() => {
        triggerAutoSave();
      }, 30_000);
    },
  });

  useEffect(() => {
    if (editor && initialContent && !editor.isDestroyed) {
      editor.commands.setContent(initialContent);
    }
  }, [initialContent, editor]);

  useEffect(() => {
    return () => {
      if (autoSaveTimerRef.current) clearTimeout(autoSaveTimerRef.current);
    };
  }, []);

  const triggerAutoSave = useCallback(async () => {
    if (!editor || isSigned) return;
    const content_json = editor.getJSON() as Record<string, unknown>;
    const content_html = editor.getHTML();
    await saveDraft.mutateAsync(
      { content_json, content_html, template_id: templateId },
      {
        onSuccess: () => {
          lastSavedRef.current = new Date();
          onSaved?.(lastSavedRef.current);
        },
      }
    );
  }, [editor, isSigned, saveDraft, templateId, onSaved]);

  function handleManualSave() {
    if (autoSaveTimerRef.current) clearTimeout(autoSaveTimerRef.current);
    triggerAutoSave();
    toast.promise(
      saveDraft.mutateAsync({
        content_json: editor?.getJSON() as Record<string, unknown>,
        content_html: editor?.getHTML(),
        template_id: templateId,
      }),
      {
        loading: "Đang lưu...",
        success: () => {
          lastSavedRef.current = new Date();
          onSaved?.(lastSavedRef.current!);
          return "Đã lưu bệnh án";
        },
        error: "Lưu thất bại",
      }
    );
  }

  if (!editor) return null;

  return (
    <div className="flex flex-col gap-2">
      {/* Toolbar */}
      {!isSigned && (
        <div className="flex flex-wrap items-center gap-1 rounded-md border bg-muted/40 p-1.5">
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleBold().run()}
            active={editor.isActive("bold")}
            title="Đậm"
          >
            <Bold className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleItalic().run()}
            active={editor.isActive("italic")}
            title="Nghiêng"
          >
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
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleBulletList().run()}
            active={editor.isActive("bulletList")}
            title="Danh sách"
          >
            <List className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleOrderedList().run()}
            active={editor.isActive("orderedList")}
            title="Danh sách số"
          >
            <ListOrdered className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().toggleBlockquote().run()}
            active={editor.isActive("blockquote")}
            title="Trích dẫn"
          >
            <Quote className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().setHorizontalRule().run()}
            title="Kẻ ngang"
          >
            <Minus className="h-4 w-4" />
          </ToolbarButton>
          <Separator orientation="vertical" className="mx-1 h-5" />
          <ToolbarButton
            onClick={() =>
              editor
                .chain()
                .focus()
                .insertTable({ rows: 3, cols: 3, withHeaderRow: true })
                .run()
            }
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
          <Separator orientation="vertical" className="mx-1 h-5" />
          <ToolbarButton
            onClick={() => editor.chain().focus().undo().run()}
            disabled={!editor.can().undo()}
            title="Hoàn tác"
          >
            <Undo className="h-4 w-4" />
          </ToolbarButton>
          <ToolbarButton
            onClick={() => editor.chain().focus().redo().run()}
            disabled={!editor.can().redo()}
            title="Làm lại"
          >
            <Redo className="h-4 w-4" />
          </ToolbarButton>
          <div className="ml-auto flex items-center gap-2">
            {lastSavedRef.current && (
              <span className="text-xs text-muted-foreground">
                Đã lưu lúc {format(lastSavedRef.current, "HH:mm", { locale: vi })}
              </span>
            )}
            <Button
              size="sm"
              variant="outline"
              onClick={handleManualSave}
              disabled={saveDraft.isPending}
              className="min-h-[32px]"
            >
              {saveDraft.isPending ? "Đang lưu..." : "Lưu nháp"}
            </Button>
          </div>
        </div>
      )}

      {/* Editor area */}
      <div
        className={cn(
          "min-h-[400px] rounded-md border bg-background p-4 prose prose-sm max-w-none dark:prose-invert focus-within:ring-2 focus-within:ring-ring",
          isSigned && "bg-muted/30 opacity-80"
        )}
      >
        <EditorContent editor={editor} />
      </div>

      {isSigned && (
        <p className="text-xs text-muted-foreground italic text-center">
          Bệnh án đã được ký số — chỉ đọc
        </p>
      )}
    </div>
  );
}

function ToolbarButton({
  children,
  onClick,
  active,
  disabled,
  title,
}: {
  children: React.ReactNode;
  onClick?: () => void;
  active?: boolean;
  disabled?: boolean;
  title?: string;
}) {
  return (
    <Button
      type="button"
      variant={active ? "secondary" : "ghost"}
      size="sm"
      onClick={onClick}
      disabled={disabled}
      title={title}
      className="h-8 w-8 p-0"
      aria-label={title}
    >
      {children}
    </Button>
  );
}
