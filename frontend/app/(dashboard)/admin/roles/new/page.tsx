"use client";

import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { RoleForm } from "@/components/domain/RoleForm";
import { useCreateRole } from "@/lib/hooks/use-roles";
import type { CreateRoleRequest } from "@/lib/api/types";

const FORM_ID = "role-form";

export default function NewRolePage() {
  const router = useRouter();
  const createMutation = useCreateRole();

  return (
    <FullPageFormShell
      title="Tạo vai trò mới"
      description="Định nghĩa vai trò tuỳ chỉnh và ma trận quyền hạn"
      backHref="/admin/roles"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo vai trò"
      isSubmitting={createMutation.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <RoleForm
          formId={FORM_ID}
          hideActions
          isLoading={createMutation.isPending}
          onSubmit={async (values) => {
            await createMutation.mutateAsync(values as CreateRoleRequest);
            router.push("/admin/roles");
          }}
        />
      </div>
    </FullPageFormShell>
  );
}
