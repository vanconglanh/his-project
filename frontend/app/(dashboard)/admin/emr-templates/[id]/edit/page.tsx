"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FormSkeleton } from "@/components/ui/PageSkeleton";
import { EmrTemplateForm } from "../../_components/EmrTemplateForm";
import { useEmrTemplates, useUpdateEmrTemplate } from "@/lib/hooks/use-emr";

const FORM_ID = "emr-template-form";

export default function EditEmrTemplatePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { data: templates, isLoading, isError } = useEmrTemplates();
  const template = templates?.find((t) => t.id === id) ?? null;
  const updateTemplate = useUpdateEmrTemplate(id);
  const isSystemTemplate = !!template?.is_system;

  return (
    <FullPageFormShell
      title="Sửa mẫu bệnh án"
      description="Cập nhật thông tin và nội dung mẫu bệnh án"
      backHref="/admin/emr-templates"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Cập nhật"
      isSubmitting={updateTemplate.isPending}
      hideSubmit={isLoading || isSystemTemplate}
    >
      {isLoading ? (
        <FormSkeleton />
      ) : isError || !template ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được mẫu bệnh án. Vui lòng thử lại.
        </div>
      ) : isSystemTemplate ? (
        <div className="rounded-lg border border-amber-300 bg-amber-50 p-6 text-center text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950 dark:text-amber-200">
          Đây là mẫu bệnh án hệ thống và không thể chỉnh sửa. Vui lòng tạo mẫu tùy chỉnh mới nếu cần thay đổi nội dung.
        </div>
      ) : (
        <EmrTemplateForm
          formId={FORM_ID}
          template={template}
          onSubmit={async (payload) => {
            await updateTemplate.mutateAsync(payload);
            router.push("/admin/emr-templates");
          }}
        />
      )}
    </FullPageFormShell>
  );
}
