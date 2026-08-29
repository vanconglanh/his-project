"use client";

import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { BranchForm } from "@/components/domain/BranchForm";
import { useCreateBranch } from "@/lib/hooks/use-branches";

const FORM_ID = "branch-form";

export default function NewBranchPage() {
  const router = useRouter();
  const createBranch = useCreateBranch();

  return (
    <FullPageFormShell
      title="Tạo chi nhánh"
      description="Thêm chi nhánh mới cho phòng khám"
      backHref="/admin/branches"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo chi nhánh"
      isSubmitting={createBranch.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <BranchForm
          formId={FORM_ID}
          isPending={createBranch.isPending}
          onSubmit={(data) =>
            createBranch.mutate(data, {
              onSuccess: () => router.push("/admin/branches"),
            })
          }
        />
      </div>
    </FullPageFormShell>
  );
}
