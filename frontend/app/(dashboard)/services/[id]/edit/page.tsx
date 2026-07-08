"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FormSkeleton } from "@/components/ui/PageSkeleton";
import { ServiceForm } from "@/components/domain/ServiceForm";
import { useService, useUpdateService } from "@/lib/hooks/use-services";

const FORM_ID = "service-form";

export default function EditServicePage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { data: service, isLoading, isError } = useService(id);
  const updateService = useUpdateService();

  return (
    <FullPageFormShell
      title="Sửa dịch vụ"
      description="Cập nhật thông tin dịch vụ"
      backHref="/services"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Lưu dịch vụ"
      isSubmitting={updateService.isPending}
    >
      {isLoading ? (
        <FormSkeleton />
      ) : isError || !service ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được dịch vụ. Vui lòng thử lại.
        </div>
      ) : (
        <div className="max-w-2xl rounded-lg border bg-card p-6">
          <ServiceForm
            formId={FORM_ID}
            editTarget={service}
            onSubmit={async (body) => {
              try {
                await updateService.mutateAsync({ id: service.id, body });
                toast.success("Đã cập nhật dịch vụ");
                router.push("/services");
              } catch (e: unknown) {
                const err = e as { response?: { data?: { error?: { code?: string } } } };
                if (err?.response?.data?.error?.code === "SERVICE_CODE_EXISTS") {
                  toast.error("Mã dịch vụ đã tồn tại");
                } else {
                  toast.error("Lưu dịch vụ thất bại");
                }
              }
            }}
          />
        </div>
      )}
    </FullPageFormShell>
  );
}
