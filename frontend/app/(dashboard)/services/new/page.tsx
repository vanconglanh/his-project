"use client";

import { useRouter } from "next/navigation";
import { toast } from "sonner";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { ServiceForm } from "@/components/domain/ServiceForm";
import { useCreateService } from "@/lib/hooks/use-services";

const FORM_ID = "service-form";

export default function NewServicePage() {
  const router = useRouter();
  const createService = useCreateService();

  return (
    <FullPageFormShell
      title="Tạo dịch vụ mới"
      description="Thêm dịch vụ vào bảng giá phòng khám"
      backHref="/services"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Lưu dịch vụ"
      isSubmitting={createService.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <ServiceForm
          formId={FORM_ID}
          onSubmit={async (body) => {
            try {
              await createService.mutateAsync(body);
              toast.success("Đã tạo dịch vụ");
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
    </FullPageFormShell>
  );
}
