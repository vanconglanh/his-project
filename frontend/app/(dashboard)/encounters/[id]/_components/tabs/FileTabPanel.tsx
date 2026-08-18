"use client";

import { ClsUploadList } from "@/components/domain/ClsUploadList";

interface Props {
  patientId: string;
}

export function FileTabPanel({ patientId }: Props) {
  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Kéo thả ảnh, PDF hoặc file DICOM để đính kèm vào hồ sơ bệnh nhân.
      </p>
      <ClsUploadList patientId={patientId} />
    </div>
  );
}
