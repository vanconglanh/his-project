"use client";

import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { EmrTemplateForm } from "../_components/EmrTemplateForm";
import { useCreateEmrTemplate } from "@/lib/hooks/use-emr";

const FORM_ID = "emr-template-form";

export default function NewEmrTemplatePage() {
  const router = useRouter();
  const createTemplate = useCreateEmrTemplate();

  return (
    <FullPageFormShell
      title="Tạo mẫu bệnh án mới"
      description="Soạn mẫu bệnh án dùng lại khi khám bệnh"
      backHref="/admin/emr-templates"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo mẫu"
      isSubmitting={createTemplate.isPending}
    >
      <EmrTemplateForm
        formId={FORM_ID}
        onSubmit={async (payload) => {
          await createTemplate.mutateAsync(payload);
          router.push("/admin/emr-templates");
        }}
      />
    </FullPageFormShell>
  );
}
