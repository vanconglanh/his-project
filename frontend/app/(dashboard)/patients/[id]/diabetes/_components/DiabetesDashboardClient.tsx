"use client";

import Link from "next/link";
import { ArrowLeft } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { usePatient } from "@/lib/hooks/use-patients";
import { useDiabetesTrajectory, useDeteriorationFlags } from "@/lib/hooks/use-diabetes-dashboard";
import { HbA1cTrajectoryChart } from "@/components/domain/diabetes/HbA1cTrajectoryChart";
import { GlucoseBpBmiChart } from "@/components/domain/diabetes/GlucoseBpBmiChart";
import { DeteriorationBanner } from "@/components/domain/diabetes/DeteriorationBanner";
import { AiSuggestionPanel } from "@/components/domain/diabetes/AiSuggestionPanel";

interface Props {
  patientId: string;
}

export function DiabetesDashboardClient({ patientId }: Props) {
  const { data: patient, isLoading: patientLoading } = usePatient(patientId);
  const { data: trajectory, isLoading: trajectoryLoading } = useDiabetesTrajectory(patientId);
  const { data: flagsData } = useDeteriorationFlags(patientId);

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-2">
        <Link href={`/patients/${patientId}`}>
          <Button variant="ghost" size="sm" className="gap-1.5 -ml-2">
            <ArrowLeft className="h-4 w-4" />
            Hồ sơ bệnh nhân
          </Button>
        </Link>
      </div>

      <div>
        <h1 className="text-xl font-bold">Xu hướng điều trị đái tháo đường</h1>
        {patientLoading ? (
          <Skeleton className="h-5 w-48 mt-1" />
        ) : (
          <p className="text-sm text-muted-foreground">
            {patient?.full_name} — {patient?.code}
          </p>
        )}
      </div>

      <DeteriorationBanner flags={flagsData?.flags} />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <CardTitle className="text-base">HbA1c</CardTitle>
          </CardHeader>
          <CardContent>
            <HbA1cTrajectoryChart data={trajectory} isLoading={trajectoryLoading} />
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <CardTitle className="text-base">Đường huyết đói / Huyết áp / BMI</CardTitle>
          </CardHeader>
          <CardContent>
            <GlucoseBpBmiChart data={trajectory} isLoading={trajectoryLoading} />
          </CardContent>
        </Card>
      </div>

      <AiSuggestionPanel patientId={patientId} />
    </div>
  );
}
