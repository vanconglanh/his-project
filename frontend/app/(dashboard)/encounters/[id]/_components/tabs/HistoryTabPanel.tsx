"use client";

import { AllergyList } from "@/components/domain/AllergyList";
import { EmergencyContactList } from "@/components/domain/EmergencyContactList";
import { DiabetesAssessmentForm } from "@/components/domain/DiabetesAssessmentForm";
import { DiabetesTrendChart } from "@/components/domain/DiabetesTrendChart";
import { InBodyHistoryList } from "@/components/domain/InBodyHistoryList";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  useCreateDiabetesAssessment,
  useDiabetesAssessment,
  useUpdateDiabetesAssessment,
} from "@/lib/hooks/use-diabetes";

interface Props {
  encounterId: string;
  patientId: string;
  canEdit: boolean;
}

export function HistoryTabPanel({ encounterId, patientId, canEdit }: Props) {
  const { data: assessment } = useDiabetesAssessment(encounterId);
  const createAssessment = useCreateDiabetesAssessment(encounterId);
  const updateAssessment = useUpdateDiabetesAssessment(encounterId);

  return (
    <div className="space-y-6">
      <AllergyList patientId={patientId} />
      <EmergencyContactList patientId={patientId} />

      <Card>
        <CardHeader className="pb-2 pt-4 px-4">
          <CardTitle className="text-sm font-semibold">Xu hướng đái tháo đường</CardTitle>
        </CardHeader>
        <CardContent className="px-4 pb-4">
          <DiabetesTrendChart patientId={patientId} />
        </CardContent>
      </Card>

      {/* BM-101 (WI-7): hiển thị kết quả InBody (vd lễ tân tải lên khi tiếp đón) ngay ở
          màn khám bệnh — danh sách theo bệnh nhân nên luôn thấy dù gắn encounter nào. */}
      <Card>
        <CardHeader className="pb-2 pt-4 px-4">
          <CardTitle className="text-sm font-semibold">Kết quả InBody</CardTitle>
        </CardHeader>
        <CardContent className="px-4 pb-4">
          <InBodyHistoryList patientId={patientId} />
        </CardContent>
      </Card>

      {canEdit && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm font-semibold">Đánh giá đái tháo đường</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <DiabetesAssessmentForm
              defaultValues={assessment}
              isLoading={createAssessment.isPending || updateAssessment.isPending}
              onSubmit={(data) => {
                if (assessment) updateAssessment.mutate(data);
                else createAssessment.mutate(data);
              }}
            />
          </CardContent>
        </Card>
      )}
    </div>
  );
}
