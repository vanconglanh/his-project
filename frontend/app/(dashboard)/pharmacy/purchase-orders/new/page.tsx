"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { PurchaseOrderForm } from "@/components/domain/PurchaseOrderForm";

const FORM_ID = "purchase-order-form";
const RETURN_TO = "/pharmacy?tab=warehouse";

export default function NewPurchaseOrderPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <FullPageFormShell
      title="Tạo đơn đặt hàng"
      description="Đặt hàng nhập kho từ nhà cung cấp"
      backHref={RETURN_TO}
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo đơn đặt hàng"
      isSubmitting={isSubmitting}
    >
      <PurchaseOrderForm
        formId={FORM_ID}
        onSubmittingChange={setIsSubmitting}
        onSuccess={() => router.push(RETURN_TO)}
      />
    </FullPageFormShell>
  );
}
