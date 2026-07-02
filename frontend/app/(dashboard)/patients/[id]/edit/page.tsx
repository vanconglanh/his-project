"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { PatientEditorLayout } from "../../_components/PatientEditorLayout";
import { usePatient, useUpdatePatient } from "@/lib/hooks/use-patients";
import type { CreatePatientRequest } from "@/lib/api/types";

interface EditPatientPageProps {
  params: Promise<{ id: string }>;
}

export default function EditPatientPage({ params }: EditPatientPageProps) {
  const { id } = use(params);
  const router = useRouter();
  const { data: patient, isLoading, error } = usePatient(id);
  const updateMutation = useUpdatePatient(id);

  if (isLoading) {
    return (
      <div className="p-6 space-y-4">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-14 w-full" />
        <div className="grid grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Skeleton key={i} className="h-20" />
          ))}
        </div>
      </div>
    );
  }

  if (error || !patient) {
    return (
      <div className="flex flex-col items-center justify-center h-64 gap-4 text-muted-foreground">
        <AlertTriangle className="h-10 w-10" />
        <p>Không tìm thấy bệnh nhân</p>
        <Button variant="outline" onClick={() => router.push("/patients")}>
          Quay lại danh sách
        </Button>
      </div>
    );
  }

  const handleSubmit = async (data: CreatePatientRequest) => {
    await updateMutation.mutateAsync(data);
    router.push(`/patients/${id}`);
  };

  return (
    <PatientEditorLayout
      mode="edit"
      defaultValues={patient}
      onSubmit={handleSubmit}
      onCancel={() => router.push(`/patients/${id}`)}
      isLoading={updateMutation.isPending}
    />
  );
}
