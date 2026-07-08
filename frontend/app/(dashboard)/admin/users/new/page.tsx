"use client";

import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { InviteUserForm } from "@/components/domain/InviteUserForm";
import { useInviteUser } from "@/lib/hooks/use-users";

const FORM_ID = "invite-user-form";

export default function NewUserPage() {
  const router = useRouter();
  const inviteMutation = useInviteUser();

  return (
    <FullPageFormShell
      title="Mời người dùng mới"
      description="Gửi lời mời tham gia phòng khám qua email"
      backHref="/admin/users"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Gửi lời mời"
      isSubmitting={inviteMutation.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <InviteUserForm
          formId={FORM_ID}
          hideActions
          isLoading={inviteMutation.isPending}
          onSubmit={async (values) => {
            await inviteMutation.mutateAsync(values);
            router.push("/admin/users");
          }}
        />
      </div>
    </FullPageFormShell>
  );
}
