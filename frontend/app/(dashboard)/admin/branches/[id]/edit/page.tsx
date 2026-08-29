"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { BranchForm } from "@/components/domain/BranchForm";
import { useBranch, useUpdateBranch } from "@/lib/hooks/use-branches";
import { Skeleton } from "@/components/ui/skeleton";

const FORM_ID = "branch-form";

export default function EditBranchPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);
  const router = useRouter();
  const { data: branch, isLoading } = useBranch(id);
  const updateBranch = useUpdateBranch(id);

  return (
    <FullPageFormShell
      title="Sửa chi nhánh"
      description="Cập nhật thông tin chi nhánh"
      backHref="/admin/branches"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Cập nhật"
      isSubmitting={updateBranch.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        {isLoading ? (
          <div className="space-y-4">
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
            <Skeleton className="h-10 w-full" />
          </div>
        ) : (
          <BranchForm
            formId={FORM_ID}
            branch={branch}
            isPending={updateBranch.isPending}
            onSubmit={(data) =>
              updateBranch.mutate(data, {
                onSuccess: () => router.push("/admin/branches"),
              })
            }
          />
        )}
      </div>
    </FullPageFormShell>
  );
}
