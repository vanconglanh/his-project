"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { AdjustmentForm } from "@/components/domain/AdjustmentForm";

const FORM_ID = "adjustment-form";
const RETURN_TO = "/pharmacy?tab=adjustment";

export default function NewAdjustmentPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <FullPageFormShell
      title="Tạo điều chỉnh tồn kho"
      description="Ghi nhận kiểm kê, hư hỏng, hết hạn hoặc thất thoát"
      backHref={RETURN_TO}
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Điều chỉnh tồn kho"
      isSubmitting={isSubmitting}
    >
      <AdjustmentForm
        formId={FORM_ID}
        onSubmittingChange={setIsSubmitting}
        onSuccess={() => router.push(RETURN_TO)}
      />
    </FullPageFormShell>
  );
}
