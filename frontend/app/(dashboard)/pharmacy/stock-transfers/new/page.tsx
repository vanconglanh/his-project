"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { StockTransferForm } from "@/components/domain/StockTransferForm";

const FORM_ID = "stock-transfer-form";
const RETURN_TO = "/pharmacy/stock-transfers";

export default function NewStockTransferPage() {
  const router = useRouter();
  const [isSubmitting, setIsSubmitting] = useState(false);

  return (
    <FullPageFormShell
      title="Tạo phiếu điều chuyển kho"
      description="Điều chuyển thuốc/vật tư nội bộ giữa 2 chi nhánh cùng tenant"
      backHref={RETURN_TO}
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo phiếu"
      isSubmitting={isSubmitting}
    >
      <StockTransferForm
        formId={FORM_ID}
        onSubmittingChange={setIsSubmitting}
        onSuccess={(id) => router.push(`/pharmacy/stock-transfers/${id}`)}
      />
    </FullPageFormShell>
  );
}
