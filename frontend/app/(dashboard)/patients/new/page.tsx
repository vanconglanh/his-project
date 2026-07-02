"use client";

import { Suspense } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Skeleton } from "@/components/ui/skeleton";
import { PatientEditorLayout } from "../_components/PatientEditorLayout";
import { useCreatePatient } from "@/lib/hooks/use-patients";
import type { CreatePatientRequest } from "@/lib/api/types";

function NewPatientContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnTo = searchParams.get("returnTo");
  const createMutation = useCreatePatient();

  const handleSubmit = async (data: CreatePatientRequest) => {
    const result = await createMutation.mutateAsync(data);
    const target = returnTo
      ? `${returnTo}?selectPatient=${result.id}`
      : `/patients/${result.id}`;
    router.push(target);
  };

  const handleCancel = () => {
    if (returnTo) {
      router.push(returnTo);
    } else {
      router.back();
    }
  };

  return (
    <PatientEditorLayout
      mode="create"
      onSubmit={handleSubmit}
      onCancel={handleCancel}
      isLoading={createMutation.isPending}
    />
  );
}

export default function NewPatientPage() {
  return (
    <Suspense
      fallback={
        <div className="p-8 space-y-4">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-64 w-full" />
        </div>
      }
    >
      <NewPatientContent />
    </Suspense>
  );
}
