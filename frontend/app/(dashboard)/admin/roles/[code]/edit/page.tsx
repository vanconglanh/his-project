"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FormSkeleton } from "@/components/ui/PageSkeleton";
import { RoleForm } from "@/components/domain/RoleForm";
import { useRole, useUpdateRole } from "@/lib/hooks/use-roles";
import type { UpdateRoleRequest } from "@/lib/api/types";

const FORM_ID = "role-form";

export default function EditRolePage({
  params,
}: {
  params: Promise<{ code: string }>;
}) {
  const { code } = use(params);
  const router = useRouter();
  const { data: role, isLoading, isError } = useRole(code);
  const updateMutation = useUpdateRole();

  return (
    <FullPageFormShell
      title="Sửa vai trò"
      description="Cập nhật thông tin và quyền hạn của vai trò"
      backHref="/admin/roles"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Lưu thay đổi"
      isSubmitting={updateMutation.isPending}
    >
      {isLoading ? (
        <FormSkeleton />
      ) : isError || !role ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được vai trò. Vui lòng thử lại.
        </div>
      ) : (
        <div className="max-w-2xl rounded-lg border bg-card p-6">
          <RoleForm
            formId={FORM_ID}
            initialValues={role}
            isEdit
            hideActions
            isLoading={updateMutation.isPending}
            onSubmit={async (values) => {
              await updateMutation.mutateAsync({
                code: role.code,
                payload: values as UpdateRoleRequest,
              });
              router.push("/admin/roles");
            }}
          />
        </div>
      )}
    </FullPageFormShell>
  );
}
