"use client";

import { Suspense, useState, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { Skeleton } from "@/components/ui/skeleton";
import { PatientEditorLayout } from "../_components/PatientEditorLayout";
import { useCreatePatient } from "@/lib/hooks/use-patients";
import type { CreatePatientRequest } from "@/lib/api/types";
import { isPossibleDuplicateResult } from "@/lib/api/types";
import type { CccdQrData } from "@/lib/utils/cccd-qr";

/** Phải khớp key dùng trong ReceptionCheckInForm.tsx */
const CCCD_PREFILL_KEY = "reception-cccd-prefill";

function NewPatientContent() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const returnTo = searchParams.get("returnTo");
  const createMutation = useCreatePatient();
  const [cccdPrefill, setCccdPrefill] = useState<CccdQrData | null>(null);

  // Đọc dữ liệu đã quét CCCD từ quầy tiếp đón (nếu có) — dùng 1 lần
  useEffect(() => {
    const raw = sessionStorage.getItem(CCCD_PREFILL_KEY);
    if (!raw) return;
    sessionStorage.removeItem(CCCD_PREFILL_KEY);
    try {
      setCccdPrefill(JSON.parse(raw) as CccdQrData);
    } catch {
      // du lieu hong -> bo qua, khong chan luong tao thu cong
    }
  }, []);

  const handleSubmit = async (data: CreatePatientRequest) => {
    const result = await createMutation.mutateAsync(data);

    // FR-101: backend nghi trung -> chua tao ho so, hoi lai le tan truoc khi tao that
    if (isPossibleDuplicateResult(result)) {
      const names = result.duplicate_candidates
        .map((c) => `- ${c.full_name} (${c.code}${c.date_of_birth ? `, sinh ${c.date_of_birth}` : ""})`)
        .join("\n");
      const confirmed = window.confirm(
        `Phát hiện hồ sơ có thể trùng:\n${names}\n\nBạn có chắc muốn tạo bệnh nhân mới không?`
      );
      if (!confirmed) return;
      const confirmedResult = await createMutation.mutateAsync({
        ...data,
        confirm_create_despite_duplicate: true,
      });
      if (isPossibleDuplicateResult(confirmedResult)) return; // an toàn, không nên xảy ra
      const target = returnTo
        ? `${returnTo}?selectPatient=${confirmedResult.id}`
        : `/patients/${confirmedResult.id}`;
      router.push(target);
      return;
    }

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
      initialCccdData={cccdPrefill}
    />
  );
}

export default function NewPatientPage() {
  return (
    <Suspense
      fallback={
        <div className="p-6 space-y-4">
          <Skeleton className="h-8 w-64" />
          <Skeleton className="h-64 w-full" />
        </div>
      }
    >
      <NewPatientContent />
    </Suspense>
  );
}
