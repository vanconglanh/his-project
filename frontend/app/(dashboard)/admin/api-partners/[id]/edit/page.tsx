"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FormSkeleton } from "@/components/ui/PageSkeleton";
import { ApiPartnerForm } from "@/components/domain/ApiPartnerForm";
import { useApiPartner, useUpdateApiPartner } from "@/lib/hooks/use-api-partners";

const FORM_ID = "api-partner-form";

export default function EditApiPartnerPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { data: partner, isLoading, isError } = useApiPartner(id);
  const updateMutation = useUpdateApiPartner(id);

  return (
    <FullPageFormShell
      title="Sửa đối tác API"
      description="Cập nhật thông tin và quyền truy cập của đối tác"
      backHref="/admin/api-partners"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Cập nhật"
      isSubmitting={updateMutation.isPending}
    >
      {isLoading ? (
        <FormSkeleton />
      ) : isError || !partner ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được đối tác. Vui lòng thử lại.
        </div>
      ) : (
        <div className="max-w-2xl rounded-lg border bg-card p-6">
          <ApiPartnerForm
            formId={FORM_ID}
            hideSubmit
            defaultValues={partner}
            isLoading={updateMutation.isPending}
            submitLabel="Cập nhật"
            onSubmit={(data) =>
              updateMutation.mutate(data, {
                onSuccess: () => router.push("/admin/api-partners"),
              })
            }
          />
        </div>
      )}
    </FullPageFormShell>
  );
}
