"use client";

import { useState, useCallback } from "react";
import { useEncounter, useStartEncounter, useCloseEncounter, useAddDiagnosis, useDeleteDiagnosis } from "@/lib/hooks/use-encounters";
import { useEmr, useSignEmr } from "@/lib/hooks/use-emr";
import { useDiabetesAssessment, useCreateDiabetesAssessment, useUpdateDiabetesAssessment } from "@/lib/hooks/use-diabetes";
import { useVitalSigns, useCreateVitalSigns } from "@/lib/hooks/use-vital-signs";
import { Skeleton } from "@/components/ui/skeleton";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { EncounterStatusBadge } from "@/components/domain/EncounterStatusBadge";
import { EncounterAlertBanner } from "@/components/domain/EncounterAlertBanner";
import { EmrEditor } from "@/components/domain/EmrEditor";
import { EmrTemplateSelector } from "@/components/domain/EmrTemplateSelector";
import { EmrSignDialog } from "@/components/domain/EmrSignDialog";
import { Icd10Picker } from "@/components/domain/Icd10Picker";
import { DiagnosesList } from "@/components/domain/DiagnosesList";
import { DiabetesAssessmentForm } from "@/components/domain/DiabetesAssessmentForm";
import { DiabetesTrendChart } from "@/components/domain/DiabetesTrendChart";
import { LabOrderForm } from "@/components/domain/LabOrderForm";
import { RadOrderForm } from "@/components/domain/RadOrderForm";
import { LabRadOrderList } from "@/components/domain/LabRadOrderList";
import { EncounterTimeline } from "@/components/domain/EncounterTimeline";
import { VitalSignsHistoryDrawer } from "@/components/domain/VitalSignsHistoryDrawer";
import { SimpleAvatar } from "@/components/domain/SimpleAvatar";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { Alert, AlertDescription } from "@/components/ui/alert";
import {
  AlertTriangle,
  ArrowLeft,
  CheckCircle,
  Play,
  Printer,
  PenTool,
  Activity,
  Save,
  Plus,
  Trash2,
} from "lucide-react";
import Link from "next/link";
import type { Icd10Response, DiagnosisType, EmrTemplateResponse, VitalSignsResponse } from "@/lib/api/types";
import { format } from "date-fns";
import { vi } from "date-fns/locale";
import { ClsUploadList } from "@/components/domain/ClsUploadList";
import { PrescriptionForm } from "@/components/domain/PrescriptionForm";
import { usePrescriptions } from "@/lib/hooks/use-prescriptions";

function PrescriptionTabContent({ encounterId, patientId }: { encounterId: string; patientId: string }) {
  const { data } = usePrescriptions({ encounter_id: encounterId, page_size: 1 });
  const existingId = data?.data?.[0]?.id;
  return (
    <PrescriptionForm
      encounterId={encounterId}
      patientId={patientId}
      existingPrescriptionId={existingId}
    />
  );
}

interface Props {
  encounterId: string;
}

export function EncounterDetailClient({ encounterId }: Props) {
  const { data: encounter, isLoading } = useEncounter(encounterId);
  const { data: emr } = useEmr(encounterId);
  const { data: assessment } = useDiabetesAssessment(encounterId);

  const startEncounter = useStartEncounter(encounterId);
  const closeEncounter = useCloseEncounter(encounterId);
  const addDiagnosis = useAddDiagnosis(encounterId);
  const deleteDiagnosis = useDeleteDiagnosis(encounterId);
  const signEmr = useSignEmr(encounterId);
  const createAssessment = useCreateDiabetesAssessment(encounterId);
  const updateAssessment = useUpdateDiabetesAssessment(encounterId);

  const [signDialogOpen, setSignDialogOpen] = useState(false);
  const [vitalDrawerOpen, setVitalDrawerOpen] = useState(false);
  const [selectedTemplateId, setSelectedTemplateId] = useState<string | undefined>();
  const [emrContent, setEmrContent] = useState<Record<string, unknown> | undefined>(
    emr?.content_json
  );
  const [lastSaved, setLastSaved] = useState<Date | null>(null);
  const [activeTab, setActiveTab] = useState("emr");

  const handleTemplateSelect = useCallback((template: EmrTemplateResponse) => {
    setEmrContent(template.content_json);
    setSelectedTemplateId(template.id);
  }, []);

  const handleAddDiagnosis = useCallback(
    (item: Icd10Response, type: DiagnosisType) => {
      addDiagnosis.mutate({ icd10_code: item.code, type, note: undefined });
    },
    [addDiagnosis]
  );

  const isSigned = !!emr?.signed_at;
  const isInProgress = encounter?.status === "IN_PROGRESS";
  const isWaiting = encounter?.status === "WAITING";
  const isDone = encounter?.status === "DONE";

  if (isLoading) {
    return (
      <div className="space-y-4 p-4">
        <Skeleton className="h-8 w-48" />
        <div className="grid grid-cols-12 gap-4">
          <Skeleton className="col-span-3 h-96" />
          <Skeleton className="col-span-6 h-96" />
          <Skeleton className="col-span-3 h-96" />
        </div>
      </div>
    );
  }

  if (!encounter) {
    return (
      <div className="flex flex-col items-center gap-4 py-20">
        <AlertTriangle className="h-12 w-12 text-muted-foreground" />
        <p className="text-muted-foreground">Không tìm thấy lượt khám</p>
        <Link href="/encounters">
          <Button variant="outline">Quay lại danh sách</Button>
        </Link>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-muted-foreground">
        <Link href="/encounters" className="hover:text-foreground flex items-center gap-1">
          <ArrowLeft className="h-4 w-4" />
          Khám bệnh
        </Link>
        <span>/</span>
        <span className="text-foreground font-medium">
          {encounter.patient_summary?.full_name ?? "Chi tiết lượt khám"}
        </span>
      </div>

      {/* Over 12h alert */}
      {encounter.alert_over_12h && encounter.started_at && (
        <EncounterAlertBanner
          hoursOpen={
            (Date.now() - new Date(encounter.started_at).getTime()) / 3_600_000
          }
          startedAt={encounter.started_at}
        />
      )}

      {/* 3-column layout */}
      <div className="grid grid-cols-12 gap-4">
        {/* Left — Patient summary */}
        <div className="col-span-12 lg:col-span-3 space-y-4">
          <Card>
            <CardContent className="pt-4 space-y-3">
              <div className="flex items-center gap-3">
                <SimpleAvatar name={encounter.patient_summary?.full_name ?? "?"} size="lg" />
                <div>
                  <p className="font-semibold">{encounter.patient_summary?.full_name}</p>
                  <p className="text-xs text-muted-foreground">
                    {encounter.patient_summary?.year_of_birth} · {encounter.patient_summary?.gender}
                  </p>
                  {encounter.patient_summary?.phone && (
                    <p className="text-xs text-muted-foreground">{encounter.patient_summary.phone}</p>
                  )}
                </div>
              </div>
              <Separator />
              <div className="space-y-1 text-sm">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Bác sĩ</span>
                  <span>{encounter.doctor_name ?? "Chưa phân công"}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Phòng</span>
                  <span>{encounter.room_name ?? "Chưa phân phòng"}</span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Lý do khám</span>
                  <span className="text-right max-w-[140px] truncate" title={encounter.reason_for_visit}>
                    {encounter.reason_for_visit}
                  </span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Vital signs widget */}
          {encounter.vital_signs_latest && Object.keys(encounter.vital_signs_latest).length > 0 ? (
            <Card>
              <CardHeader className="pb-2 pt-3 px-4">
                <CardTitle className="text-sm flex items-center justify-between">
                  Sinh hiệu gần nhất
                  <Button
                    variant="ghost"
                    size="sm"
                    onClick={() => setVitalDrawerOpen(true)}
                    className="text-xs h-7"
                  >
                    <Activity className="h-3.5 w-3.5 mr-1" />
                    Xem tất cả
                  </Button>
                </CardTitle>
              </CardHeader>
              <CardContent className="px-4 pb-3">
                <VitalSummary vital={encounter.vital_signs_latest as Record<string, number>} />
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardContent className="pt-4 text-sm text-muted-foreground">
                Chưa có sinh hiệu.{" "}
                <button
                  onClick={() => setVitalDrawerOpen(true)}
                  className="text-primary hover:underline"
                >
                  Xem lịch sử
                </button>
              </CardContent>
            </Card>
          )}

          {/* Reception note */}
          {encounter.chief_complaint && (
            <Card>
              <CardHeader className="pb-1 pt-3 px-4">
                <CardTitle className="text-sm">Lý do chính</CardTitle>
              </CardHeader>
              <CardContent className="px-4 pb-3 text-sm text-muted-foreground">
                {encounter.chief_complaint}
              </CardContent>
            </Card>
          )}
        </div>

        {/* Middle — Tabs */}
        <div className="col-span-12 lg:col-span-6">
          <Tabs value={activeTab} onValueChange={setActiveTab}>
            <TabsList className="w-full flex-wrap h-auto gap-1">
              <TabsTrigger value="emr">Khám bệnh</TabsTrigger>
              <TabsTrigger value="vitals">Sinh hiệu</TabsTrigger>
              <TabsTrigger value="diagnosis">Chẩn đoán</TabsTrigger>
              <TabsTrigger value="diabetes">Đánh giá ĐTĐ</TabsTrigger>
              <TabsTrigger value="cls">CLS</TabsTrigger>
              <TabsTrigger value="cls-results">Kết quả CLS</TabsTrigger>
              <TabsTrigger value="prescription">Đơn thuốc</TabsTrigger>
              <TabsTrigger value="timeline">Timeline</TabsTrigger>
            </TabsList>

            {/* EMR Tab */}
            <TabsContent value="emr" className="mt-3 space-y-3">
              <div className="flex items-center gap-2">
                <EmrTemplateSelector onSelect={handleTemplateSelect} />
                {isSigned && (
                  <Badge className="bg-green-100 text-green-800 border-green-200">
                    <CheckCircle className="h-3.5 w-3.5 mr-1" />
                    Đã ký số
                    {emr?.signed_by_name && ` — ${emr.signed_by_name}`}
                  </Badge>
                )}
                {lastSaved && !isSigned && (
                  <span className="text-xs text-muted-foreground ml-auto">
                    Đã lưu lúc {format(lastSaved, "HH:mm", { locale: vi })}
                  </span>
                )}
              </div>
              <EmrEditor
                encounterId={encounterId}
                initialContent={emrContent ?? emr?.content_json}
                isSigned={isSigned}
                onSaved={setLastSaved}
                templateId={selectedTemplateId}
              />
            </TabsContent>

            {/* Vital Signs Tab */}
            <TabsContent value="vitals" className="mt-3 space-y-4">
              <VitalSignsTabContent encounterId={encounterId} readOnly={!isInProgress} />
            </TabsContent>

            {/* Diagnosis Tab */}
            <TabsContent value="diagnosis" className="mt-3 space-y-4">
              <DiagnosisTabContent
                encounterId={encounterId}
                diagnoses={encounter.diagnoses}
                isInProgress={isInProgress}
                isDone={isDone}
                onAddSingle={handleAddDiagnosis}
                onDelete={(id) => deleteDiagnosis.mutate(id)}
              />
            </TabsContent>

            {/* Diabetes Assessment Tab */}
            <TabsContent value="diabetes" className="mt-3 space-y-6">
              {encounter.patient_id && (
                <DiabetesTrendChart patientId={encounter.patient_id} />
              )}
              <DiabetesAssessmentForm
                defaultValues={assessment}
                isLoading={createAssessment.isPending || updateAssessment.isPending}
                onSubmit={(data) => {
                  if (assessment) {
                    updateAssessment.mutate(data);
                  } else {
                    createAssessment.mutate(data);
                  }
                }}
              />
            </TabsContent>

            {/* CLS Tab */}
            <TabsContent value="cls" className="mt-3 space-y-6">
              <div>
                <h4 className="text-sm font-semibold mb-3">Chỉ định xét nghiệm</h4>
                <LabOrderForm encounterId={encounterId} />
              </div>
              <Separator />
              <div>
                <h4 className="text-sm font-semibold mb-3">Chỉ định chẩn đoán hình ảnh</h4>
                <RadOrderForm encounterId={encounterId} />
              </div>
              <Separator />
              <div>
                <h4 className="text-sm font-semibold mb-3">Danh sách chỉ định</h4>
                <LabRadOrderList encounterId={encounterId} />
              </div>
            </TabsContent>

            {/* CLS Upload Results Tab */}
            <TabsContent value="cls-results" className="mt-3">
              <ClsUploadList patientId={encounter.patient_id} />
            </TabsContent>

            {/* Prescription Tab */}
            <TabsContent value="prescription" className="mt-3">
              <PrescriptionTabContent encounterId={encounterId} patientId={encounter.patient_id} />
            </TabsContent>

            {/* Timeline Tab */}
            <TabsContent value="timeline" className="mt-3">
              <EncounterTimeline encounterId={encounterId} />
            </TabsContent>
          </Tabs>
        </div>

        {/* Right — Action panel */}
        <div className="col-span-12 lg:col-span-3">
          <div className="sticky top-4 space-y-4">
            <Card>
              <CardHeader className="pb-2 pt-4 px-4">
                <CardTitle className="text-sm">Trạng thái</CardTitle>
              </CardHeader>
              <CardContent className="px-4 pb-4 space-y-3">
                <EncounterStatusBadge status={encounter.status} className="w-full justify-center py-1.5 text-sm" />

                {isWaiting && (
                  <Button
                    className="w-full min-h-[44px] gap-2"
                    onClick={() => startEncounter.mutate()}
                    disabled={startEncounter.isPending}
                  >
                    <Play className="h-4 w-4" />
                    Bắt đầu khám
                  </Button>
                )}

                {isInProgress && (
                  <>
                    <Button
                      variant="outline"
                      className="w-full min-h-[44px] gap-2"
                      onClick={() => setSignDialogOpen(true)}
                      disabled={isSigned}
                    >
                      <PenTool className="h-4 w-4" />
                      {isSigned ? "Đã ký số BA" : "Ký số bệnh án"}
                    </Button>
                    <Button
                      variant="default"
                      className="w-full min-h-[44px] gap-2 bg-green-600 hover:bg-green-700"
                      onClick={() => closeEncounter.mutate()}
                      disabled={closeEncounter.isPending}
                    >
                      <CheckCircle className="h-4 w-4" />
                      Đóng lượt khám
                    </Button>
                  </>
                )}

                {isDone && (
                  <Alert className="border-green-200 bg-green-50 text-green-800">
                    <CheckCircle className="h-4 w-4 text-green-600" />
                    <AlertDescription className="text-xs">
                      Lượt khám hoàn thành
                      {encounter.finished_at && (
                        <> — {format(new Date(encounter.finished_at), "HH:mm, dd/MM/yyyy", { locale: vi })}</>
                      )}
                    </AlertDescription>
                  </Alert>
                )}

                <Separator />

                <Button
                  variant="outline"
                  className="w-full gap-2"
                  onClick={() => window.open(`/encounters/${encounterId}/print`, "_blank")}
                >
                  <Printer className="h-4 w-4" />
                  In phiếu khám
                </Button>

                <Button
                  variant="outline"
                  className="w-full gap-2"
                  onClick={() => window.open(`/encounters/${encounterId}/cls-print`, "_blank")}
                >
                  <Printer className="h-4 w-4" />
                  In phiếu chỉ định CLS
                </Button>

                {encounter.alert_over_12h && (
                  <Alert className="border-red-200 bg-red-50 text-red-800 text-xs">
                    <AlertTriangle className="h-4 w-4 text-red-600" />
                    <AlertDescription>Quá 12h — TT 46/2018/TT-BYT</AlertDescription>
                  </Alert>
                )}
              </CardContent>
            </Card>

            {/* Summary info */}
            <Card>
              <CardContent className="pt-4 pb-3 px-4 space-y-2 text-xs">
                <div className="flex justify-between">
                  <span className="text-muted-foreground">EMR ký số</span>
                  <span className={encounter.has_emr_signed ? "text-green-600" : "text-muted-foreground"}>
                    {encounter.has_emr_signed ? "Đã ký" : "Chưa ký"}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Đơn thuốc</span>
                  <span className={encounter.has_prescription ? "text-green-600" : "text-muted-foreground"}>
                    {encounter.has_prescription ? "Có" : "Chưa"}
                  </span>
                </div>
                <div className="flex justify-between">
                  <span className="text-muted-foreground">Chẩn đoán</span>
                  <span>{encounter.diagnoses.length} chẩn đoán</span>
                </div>
                {encounter.created_at && (
                  <div className="flex justify-between">
                    <span className="text-muted-foreground">Tạo lúc</span>
                    <span>{format(new Date(encounter.created_at), "HH:mm, dd/MM", { locale: vi })}</span>
                  </div>
                )}
              </CardContent>
            </Card>
          </div>
        </div>
      </div>

      {/* Vital history drawer */}
      <VitalSignsHistoryDrawer
        encounterId={encounterId}
        open={vitalDrawerOpen}
        onClose={() => setVitalDrawerOpen(false)}
      />

      {/* EMR sign dialog */}
      <EmrSignDialog
        open={signDialogOpen}
        onClose={() => setSignDialogOpen(false)}
        isLoading={signEmr.isPending}
        onSign={(sigData, certId) => {
          signEmr.mutate(
            { signature_data: sigData, certificate_id: certId },
            { onSuccess: () => setSignDialogOpen(false) }
          );
        }}
      />
    </div>
  );
}

// ─── Vital Signs Tab ─────────────────────────────────────────────────────────

interface VitalFormState {
  heart_rate_bpm: string;
  bp_systolic: string;
  bp_diastolic: string;
  temperature_c: string;
  spo2_percent: string;
  weight_kg: string;
  height_cm: string;
}

const VITAL_EMPTY: VitalFormState = {
  heart_rate_bpm: "",
  bp_systolic: "",
  bp_diastolic: "",
  temperature_c: "",
  spo2_percent: "",
  weight_kg: "",
  height_cm: "",
};

function isAbnormal(v: VitalSignsResponse): boolean {
  if (v.bp_systolic != null && v.bp_systolic > 140) return true;
  if (v.bp_diastolic != null && v.bp_diastolic > 90) return true;
  if (v.temperature_c != null && v.temperature_c > 38) return true;
  if (v.spo2_percent != null && v.spo2_percent < 95) return true;
  return false;
}

function VitalSignsTabContent({ encounterId, readOnly }: { encounterId: string; readOnly: boolean }) {
  const { data: history, isLoading } = useVitalSigns(encounterId);
  const createVital = useCreateVitalSigns(encounterId);
  const [vals, setVals] = useState<VitalFormState>(VITAL_EMPTY);

  const set = (k: keyof VitalFormState) => (e: React.ChangeEvent<HTMLInputElement>) =>
    setVals((prev) => ({ ...prev, [k]: e.target.value }));

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const payload: Record<string, number> = {};
    if (vals.heart_rate_bpm !== "") payload.heart_rate_bpm = Number(vals.heart_rate_bpm);
    if (vals.bp_systolic !== "") payload.bp_systolic = Number(vals.bp_systolic);
    if (vals.bp_diastolic !== "") payload.bp_diastolic = Number(vals.bp_diastolic);
    if (vals.temperature_c !== "") payload.temperature_c = Number(vals.temperature_c);
    if (vals.spo2_percent !== "") payload.spo2_percent = Number(vals.spo2_percent);
    if (vals.weight_kg !== "") payload.weight_kg = Number(vals.weight_kg);
    if (vals.height_cm !== "") payload.height_cm = Number(vals.height_cm);
    createVital.mutate(payload, { onSuccess: () => setVals(VITAL_EMPTY) });
  };

  return (
    <div className="space-y-6">
      {!readOnly && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm">Nhập sinh hiệu mới</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label htmlFor="v-hr">Mạch (lần/phút)</Label>
                  <Input id="v-hr" type="number" placeholder="vd: 80" value={vals.heart_rate_bpm} onChange={set("heart_rate_bpm")} className="min-h-[44px]" />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="v-temp">Nhiệt độ (°C)</Label>
                  <Input id="v-temp" type="number" step="0.1" placeholder="vd: 37.0" value={vals.temperature_c} onChange={set("temperature_c")} className="min-h-[44px]" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label>Huyết áp (mmHg)</Label>
                  <div className="flex items-center gap-2">
                    <Input type="number" placeholder="Tâm thu" value={vals.bp_systolic} onChange={set("bp_systolic")} className="min-h-[44px] flex-1" aria-label="HA tâm thu" />
                    <span className="text-muted-foreground">/</span>
                    <Input type="number" placeholder="Tâm trương" value={vals.bp_diastolic} onChange={set("bp_diastolic")} className="min-h-[44px] flex-1" aria-label="HA tâm trương" />
                  </div>
                </div>
                <div className="space-y-1">
                  <Label htmlFor="v-spo2">SpO2 (%)</Label>
                  <Input id="v-spo2" type="number" placeholder="vd: 98" value={vals.spo2_percent} onChange={set("spo2_percent")} className="min-h-[44px]" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-3">
                <div className="space-y-1">
                  <Label htmlFor="v-wt">Cân nặng (kg)</Label>
                  <Input id="v-wt" type="number" step="0.1" placeholder="vd: 65.0" value={vals.weight_kg} onChange={set("weight_kg")} className="min-h-[44px]" />
                </div>
                <div className="space-y-1">
                  <Label htmlFor="v-ht">Chiều cao (cm)</Label>
                  <Input id="v-ht" type="number" placeholder="vd: 170" value={vals.height_cm} onChange={set("height_cm")} className="min-h-[44px]" />
                </div>
              </div>
              <Button type="submit" disabled={createVital.isPending} className="gap-2 min-h-[44px]">
                <Save className="h-4 w-4" />
                {createVital.isPending ? "Đang lưu..." : "Lưu sinh hiệu"}
              </Button>
            </form>
          </CardContent>
        </Card>
      )}

      {/* History table */}
      <Card>
        <CardHeader className="pb-2 pt-4 px-4">
          <CardTitle className="text-sm flex items-center gap-2">
            <Activity className="h-4 w-4" />
            Lịch sử sinh hiệu
          </CardTitle>
        </CardHeader>
        <CardContent className="px-0 pb-2 overflow-x-auto">
          {isLoading ? (
            <div className="px-4 space-y-2">
              {[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : !history || history.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Chưa có dữ liệu sinh hiệu</p>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="text-xs">Thời gian</TableHead>
                  <TableHead className="text-xs">Mạch</TableHead>
                  <TableHead className="text-xs">HA</TableHead>
                  <TableHead className="text-xs">Nhiệt độ</TableHead>
                  <TableHead className="text-xs">SpO2</TableHead>
                  <TableHead className="text-xs">CN/CC</TableHead>
                  <TableHead className="text-xs">Người ghi</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {history.map((record) => {
                  const abnormal = isAbnormal(record);
                  return (
                    <TableRow key={record.id} className={abnormal ? "bg-red-50" : undefined}>
                      <TableCell className="text-xs whitespace-nowrap">
                        {format(new Date(record.created_at), "HH:mm dd/MM", { locale: vi })}
                      </TableCell>
                      <TableCell className="text-xs">
                        {record.heart_rate_bpm != null ? `${record.heart_rate_bpm} lần/ph` : "—"}
                      </TableCell>
                      <TableCell className="text-xs">
                        {record.bp_systolic != null && record.bp_diastolic != null ? (
                          <span className={
                            (record.bp_systolic > 140 || record.bp_diastolic > 90)
                              ? "text-red-600 font-semibold"
                              : undefined
                          }>
                            {record.bp_systolic}/{record.bp_diastolic}
                          </span>
                        ) : "—"}
                      </TableCell>
                      <TableCell className="text-xs">
                        {record.temperature_c != null ? (
                          <span className={record.temperature_c > 38 ? "text-red-600 font-semibold" : undefined}>
                            {record.temperature_c}°C
                          </span>
                        ) : "—"}
                      </TableCell>
                      <TableCell className="text-xs">
                        {record.spo2_percent != null ? (
                          <span className={record.spo2_percent < 95 ? "text-red-600 font-semibold" : undefined}>
                            {record.spo2_percent}%
                          </span>
                        ) : "—"}
                      </TableCell>
                      <TableCell className="text-xs">
                        {record.weight_kg != null ? `${record.weight_kg}kg` : ""}
                        {record.weight_kg != null && record.height_cm != null ? "/" : ""}
                        {record.height_cm != null ? `${record.height_cm}cm` : ""}
                        {record.weight_kg == null && record.height_cm == null ? "—" : ""}
                      </TableCell>
                      <TableCell className="text-xs">{record.recorded_by_name}</TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}

// ─── Diagnosis Tab ────────────────────────────────────────────────────────────

interface DiagnosisRow {
  icd10_code: string;
  icd10_name: string;
  type: DiagnosisType;
  note: string;
}

interface DiagnosisTabProps {
  encounterId: string;
  diagnoses: Array<{ id: string; icd10_code: string; name: string; type: DiagnosisType; note?: string | null; created_at: string }>;
  isInProgress: boolean;
  isDone: boolean;
  onAddSingle: (item: Icd10Response, type: DiagnosisType) => void;
  onDelete: (id: string) => void;
}

const DIAG_EMPTY: DiagnosisRow = { icd10_code: "", icd10_name: "", type: "PRIMARY", note: "" };

function DiagnosisTabContent({ diagnoses, isInProgress, isDone, onAddSingle, onDelete }: DiagnosisTabProps) {
  const [rows, setRows] = useState<DiagnosisRow[]>([{ ...DIAG_EMPTY }]);

  const updateRow = (index: number, key: keyof DiagnosisRow, value: string | DiagnosisType) => {
    setRows((prev) => prev.map((r, i) => i === index ? { ...r, [key]: value } : r));
  };

  const handleBulkSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    rows.filter((r) => r.icd10_code.trim()).forEach((row) => {
      onAddSingle(
        { code: row.icd10_code.trim(), name_vi: row.icd10_name, name_en: "", category: "", is_billable: false },
        row.type
      );
    });
    setRows([{ ...DIAG_EMPTY }]);
  };

  return (
    <div className="space-y-6">
      {isInProgress && !isDone && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm">Thêm chẩn đoán ICD-10</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4 space-y-3">
            <Icd10Picker onSelect={onAddSingle} />
          </CardContent>
        </Card>
      )}

      {/* Multi-row form */}
      {isInProgress && !isDone && (
        <Card>
          <CardHeader className="pb-2 pt-4 px-4">
            <CardTitle className="text-sm">Nhập nhanh nhiều chẩn đoán</CardTitle>
          </CardHeader>
          <CardContent className="px-4 pb-4">
            <form onSubmit={handleBulkSubmit} className="space-y-3">
              {rows.map((row, index) => (
                <div key={index} className="grid grid-cols-12 gap-2 items-end border rounded-lg p-3">
                  <div className="col-span-3 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-code-${index}`}>Mã ICD-10</Label>
                    <Input
                      id={`diag-code-${index}`}
                      placeholder="VD: E11, I10..."
                      value={row.icd10_code}
                      onChange={(e) => updateRow(index, "icd10_code", e.target.value)}
                      className="min-h-[44px] text-sm"
                    />
                  </div>
                  <div className="col-span-4 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-name-${index}`}>Tên bệnh</Label>
                    <Input
                      id={`diag-name-${index}`}
                      placeholder="Tên chẩn đoán"
                      value={row.icd10_name}
                      onChange={(e) => updateRow(index, "icd10_name", e.target.value)}
                      className="min-h-[44px] text-sm"
                    />
                  </div>
                  <div className="col-span-2 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-type-${index}`}>Loại</Label>
                    <Select
                      value={row.type}
                      onValueChange={(v) => updateRow(index, "type", v as DiagnosisType)}
                    >
                      <SelectTrigger id={`diag-type-${index}`} className="min-h-[44px] text-sm">
                        <SelectValue />
                      </SelectTrigger>
                      <SelectContent>
                        <SelectItem value="PRIMARY">Chính</SelectItem>
                        <SelectItem value="SECONDARY">Phụ</SelectItem>
                      </SelectContent>
                    </Select>
                  </div>
                  <div className="col-span-2 space-y-1">
                    <Label className="text-xs" htmlFor={`diag-note-${index}`}>Mô tả thêm</Label>
                    <Textarea
                      id={`diag-note-${index}`}
                      placeholder="Ghi chú..."
                      value={row.note}
                      onChange={(e) => updateRow(index, "note", e.target.value)}
                      className="min-h-[44px] text-sm resize-none"
                      rows={1}
                    />
                  </div>
                  <div className="col-span-1">
                    {rows.length > 1 && (
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => setRows((prev) => prev.filter((_, i) => i !== index))}
                        aria-label="Xóa dòng"
                        className="min-h-[44px] w-full text-red-500 hover:text-red-600"
                      >
                        <Trash2 className="h-4 w-4" />
                      </Button>
                    )}
                  </div>
                </div>
              ))}
              <div className="flex gap-2">
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  className="gap-1 min-h-[44px]"
                  onClick={() => setRows((prev) => [...prev, { ...DIAG_EMPTY }])}
                >
                  <Plus className="h-4 w-4" />
                  Thêm dòng
                </Button>
                <Button type="submit" size="sm" className="gap-1 min-h-[44px]">
                  <Save className="h-4 w-4" />
                  Lưu chẩn đoán
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      {/* List */}
      <div className="space-y-2">
        <h4 className="text-sm font-semibold">Danh sách chẩn đoán ({diagnoses.length})</h4>
        {diagnoses.length === 0 ? (
          <p className="text-sm text-muted-foreground">Chưa có chẩn đoán</p>
        ) : (
          <div className="space-y-2">
            {diagnoses.map((d) => (
              <div key={d.id} className="flex items-center gap-2 rounded-lg border p-2">
                <Badge variant={d.type === "PRIMARY" ? "default" : "outline"} className="shrink-0">
                  {d.icd10_code}
                </Badge>
                <span className="text-sm flex-1">{d.name}</span>
                <Badge variant={d.type === "PRIMARY" ? "default" : "outline"} className="text-xs shrink-0">
                  {d.type === "PRIMARY" ? "Chính" : "Phụ"}
                </Badge>
                {d.note && <span className="text-xs text-muted-foreground hidden sm:block">{d.note}</span>}
                {isInProgress && (
                  <Button
                    variant="ghost"
                    size="sm"
                    className="shrink-0 text-red-500 hover:text-red-600 min-h-[44px]"
                    onClick={() => onDelete(d.id)}
                    aria-label="Xóa chẩn đoán"
                  >
                    <Trash2 className="h-3.5 w-3.5" />
                  </Button>
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function VitalSummary({ vital }: { vital: Record<string, number> }) {
  const items = [
    { label: "Nhiệt độ", value: vital.temperature_c != null ? `${vital.temperature_c}°C` : null },
    { label: "Mạch", value: vital.heart_rate_bpm != null ? `${vital.heart_rate_bpm} l/ph` : null },
    {
      label: "HA",
      value:
        vital.bp_systolic != null && vital.bp_diastolic != null
          ? `${vital.bp_systolic}/${vital.bp_diastolic}`
          : null,
    },
    { label: "SpO2", value: vital.spo2_percent != null ? `${vital.spo2_percent}%` : null },
    { label: "Cân nặng", value: vital.weight_kg != null ? `${vital.weight_kg} kg` : null },
    { label: "ĐH", value: vital.glucose_mg_dl != null ? `${vital.glucose_mg_dl} mg/dL` : null },
  ].filter((x) => x.value !== null);

  return (
    <div className="grid grid-cols-2 gap-2">
      {items.map((item) => (
        <div key={item.label}>
          <p className="text-xs text-muted-foreground">{item.label}</p>
          <p className="text-sm font-medium">{item.value}</p>
        </div>
      ))}
    </div>
  );
}
