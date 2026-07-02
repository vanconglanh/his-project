"use client";

import { useState } from "react";
import { useEmrTemplates, useDeleteEmrTemplate, useCreateEmrTemplate } from "@/lib/hooks/use-emr";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { ConfirmDialog } from "@/components/domain/ConfirmDialog";
import { Trash2, Plus, FileText } from "lucide-react";
import type { EmrTemplateResponse } from "@/lib/api/types";

export function EmrTemplatesPageClient() {
  const { data: templates, isLoading } = useEmrTemplates();
  const deleteTemplate = useDeleteEmrTemplate();
  const [pendingDelete, setPendingDelete] = useState<string | null>(null);

  const systemTemplates = templates?.filter((t) => t.is_system) ?? [];
  const customTemplates = templates?.filter((t) => !t.is_system) ?? [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-2xl font-bold tracking-tight">Mẫu bệnh án</h2>
          <p className="text-sm text-muted-foreground">Quản lý template bệnh án cho từng chuyên khoa</p>
        </div>
        <Button size="sm" className="gap-2">
          <Plus className="h-4 w-4" />
          Tạo mẫu mới
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-2">{[1, 2, 3].map((i) => <Skeleton key={i} className="h-16 w-full" />)}</div>
      ) : (
        <>
          <TemplateSection title="Mẫu hệ thống" templates={systemTemplates} onDelete={null} />
          <TemplateSection
            title="Mẫu tùy chỉnh"
            templates={customTemplates}
            onDelete={(id) => setPendingDelete(id)}
          />
          {customTemplates.length === 0 && (
            <div className="flex flex-col items-center gap-3 py-12 text-muted-foreground border rounded-xl border-dashed">
              <FileText className="h-10 w-10 opacity-20" />
              <p className="text-sm">Chưa có mẫu tùy chỉnh</p>
            </div>
          )}
        </>
      )}

      <ConfirmDialog
        open={!!pendingDelete}
        onOpenChange={(v) => { if (!v) setPendingDelete(null); }}
        title="Xóa mẫu bệnh án"
        description="Bạn có chắc muốn xóa mẫu này? Hành động không thể hoàn tác."
        variant="destructive"
        onConfirm={() => {
          if (pendingDelete) deleteTemplate.mutate(pendingDelete);
          setPendingDelete(null);
        }}
      />
    </div>
  );
}

function TemplateSection({
  title,
  templates,
  onDelete,
}: {
  title: string;
  templates: EmrTemplateResponse[];
  onDelete: ((id: string) => void) | null;
}) {
  if (templates.length === 0 && !onDelete) return null;

  return (
    <div>
      <h3 className="text-sm font-medium text-muted-foreground uppercase tracking-wide mb-3">{title}</h3>
      <div className="space-y-2">
        {templates.map((t) => (
          <div key={t.id} className="flex items-center gap-3 rounded-lg border bg-card p-4">
            <FileText className="h-5 w-5 text-muted-foreground shrink-0" />
            <div className="flex-1 min-w-0">
              <p className="font-medium">{t.name}</p>
              <p className="text-xs text-muted-foreground">
                {t.speciality} · Tạo bởi {t.created_by}
              </p>
            </div>
            <Badge variant={t.is_system ? "default" : "outline"} className="text-xs shrink-0">
              {t.is_system ? "Hệ thống" : "Tùy chỉnh"}
            </Badge>
            {onDelete && !t.is_system && (
              <Button
                variant="ghost"
                size="icon"
                className="h-8 w-8 text-destructive hover:text-destructive shrink-0"
                onClick={() => onDelete(t.id)}
              >
                <Trash2 className="h-4 w-4" />
              </Button>
            )}
          </div>
        ))}
      </div>
    </div>
  );
}
