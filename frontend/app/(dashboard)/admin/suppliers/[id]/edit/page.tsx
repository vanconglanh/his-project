"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FormSkeleton } from "@/components/ui/PageSkeleton";
import { SupplierForm } from "@/components/domain/SupplierForm";
import { useSupplier, useUpdateSupplier } from "@/lib/hooks/use-suppliers";

const FORM_ID = "supplier-form";

export default function EditSupplierPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { data: supplier, isLoading, isError } = useSupplier(id);
  const updateSupplier = useUpdateSupplier(id);

  return (
    <FullPageFormShell
      title="Sửa nhà cung cấp"
      description="Cập nhật thông tin nhà cung cấp"
      backHref="/admin/suppliers"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Lưu thay đổi"
      isSubmitting={updateSupplier.isPending}
    >
      {isLoading ? (
        <FormSkeleton />
      ) : isError || !supplier ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được nhà cung cấp. Vui lòng thử lại.
        </div>
      ) : (
        <div className="max-w-2xl rounded-lg border bg-card p-6">
          <SupplierForm
            formId={FORM_ID}
            supplier={supplier}
            isPending={updateSupplier.isPending}
            onSubmit={(data) =>
              updateSupplier.mutate(data, {
                onSuccess: () => router.push("/admin/suppliers"),
              })
            }
          />
        </div>
      )}
    </FullPageFormShell>
  );
}
