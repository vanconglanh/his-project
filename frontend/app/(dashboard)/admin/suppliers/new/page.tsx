"use client";

import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { SupplierForm } from "@/components/domain/SupplierForm";
import { useCreateSupplier } from "@/lib/hooks/use-suppliers";

const FORM_ID = "supplier-form";

export default function NewSupplierPage() {
  const router = useRouter();
  const createSupplier = useCreateSupplier();

  return (
    <FullPageFormShell
      title="Tạo nhà cung cấp"
      description="Thêm nhà cung cấp mới vào danh mục"
      backHref="/admin/suppliers"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo nhà cung cấp"
      isSubmitting={createSupplier.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <SupplierForm
          formId={FORM_ID}
          isPending={createSupplier.isPending}
          onSubmit={(data) =>
            createSupplier.mutate(data, {
              onSuccess: () => router.push("/admin/suppliers"),
            })
          }
        />
      </div>
    </FullPageFormShell>
  );
}
