"use client";

import { ClsUploadList } from "@/components/domain/ClsUploadList";

interface Props {
  patientId: string;
  encounterId?: string;
}

export function FileTabPanel({ patientId, encounterId }: Props) {
  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Kéo thả ảnh, PDF hoặc file DICOM để đính kèm vào hồ sơ bệnh nhân. Với ảnh lâm sàng, bạn
        có thể mở công cụ &quot;Chú thích ảnh&quot; để vẽ, đánh dấu vị trí tổn thương trước khi lưu.
      </p>
      <ClsUploadList patientId={patientId} encounterId={encounterId} />
    </div>
  );
}
