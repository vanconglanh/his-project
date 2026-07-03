"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { toast } from "sonner";
import { Skeleton } from "@/components/ui/skeleton";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { GrnForm } from "@/components/domain/GrnForm";

const FORM_ID = "grn-form";
const RETURN_TO = "/pharmacy?tab=warehouse";

function LoadingFallback() {
  return (
    <div className="p-6 space-y-4">
      <Skeleton className="h-8 w-64" />
      <Skeleton className="h-64 w-full" />
    </div>
  );
}

function NewGrnContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const poId = searchParams.get("poId");
  const poNo = searchParams.get("poNo");
  const [isSubmitting, setIsSubmitting] = useState(false);

  // Guard: GRN luôn phải gắn với 1 đơn đặt hàng (PO). Vào thẳng route không có poId → điều hướng về + toast.
  useEffect(() => {
    if (!poId) {
      toast.warning("Chọn đơn đặt hàng trước khi nhập kho");
      router.replace(RETURN_TO);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [poId]);

  if (!poId) {
    return <LoadingFallback />;
  }

  return (
    <FullPageFormShell
      title="Nhập kho (GRN)"
      description={poNo ? `Từ đơn đặt hàng ${poNo}` : `Từ đơn đặt hàng #${poId}`}
      backHref={RETURN_TO}
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Nhập kho"
      isSubmitting={isSubmitting}
    >
      <GrnForm
        poId={poId}
        formId={FORM_ID}
        onSubmittingChange={setIsSubmitting}
        onSuccess={() => router.push(RETURN_TO)}
      />
    </FullPageFormShell>
  );
}

export default function NewGrnPage() {
  return (
    <Suspense fallback={<LoadingFallback />}>
      <NewGrnContent />
    </Suspense>
  );
}
