"use client";

import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { AppointmentForm } from "../_components/AppointmentForm";
import { useCreateAppointment } from "@/lib/hooks/use-appointments";

const FORM_ID = "appointment-form";

export default function NewAppointmentPage() {
  const router = useRouter();
  const createAppointment = useCreateAppointment();

  return (
    <FullPageFormShell
      title="Tạo lịch hẹn mới"
      description="Đặt lịch hẹn khám cho bệnh nhân"
      backHref="/appointments"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo lịch hẹn"
      isSubmitting={createAppointment.isPending}
    >
      <AppointmentForm
        formId={FORM_ID}
        onSubmit={(body) =>
          createAppointment.mutate(body, {
            onSuccess: () => router.push("/appointments"),
          })
        }
      />
    </FullPageFormShell>
  );
}
