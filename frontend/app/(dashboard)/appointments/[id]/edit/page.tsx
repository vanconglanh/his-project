"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { FormSkeleton } from "@/components/ui/PageSkeleton";
import { AppointmentForm } from "../../_components/AppointmentForm";
import { useAppointment, useUpdateAppointment } from "@/lib/hooks/use-appointments";

const FORM_ID = "appointment-form";

export default function EditAppointmentPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = use(params);
  const router = useRouter();
  const { data: appointment, isLoading, isError } = useAppointment(id);
  const updateAppointment = useUpdateAppointment();

  return (
    <FullPageFormShell
      title="Sửa lịch hẹn"
      description="Cập nhật thông tin lịch hẹn khám"
      backHref="/appointments"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Lưu thay đổi"
      isSubmitting={updateAppointment.isPending}
    >
      {isLoading ? (
        <FormSkeleton />
      ) : isError || !appointment ? (
        <div className="rounded-lg border border-destructive/30 bg-destructive/5 p-6 text-center text-sm text-destructive">
          Không tải được lịch hẹn. Vui lòng thử lại.
        </div>
      ) : (
        <AppointmentForm
          formId={FORM_ID}
          defaultValues={appointment}
          onSubmit={(body) =>
            updateAppointment.mutate(
              { id: appointment.id, body },
              { onSuccess: () => router.push("/appointments") }
            )
          }
        />
      )}
    </FullPageFormShell>
  );
}
