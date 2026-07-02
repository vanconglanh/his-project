"use client";

import { useEffect } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Checkbox } from "@/components/ui/checkbox";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import type { DiabetesAssessmentRequest, DiabetesType } from "@/lib/api/types";

const schema = z.object({
  hba1c: z.number().min(3).max(20).optional().or(z.literal(undefined)),
  fasting_glucose: z.number().min(0).optional().or(z.literal(undefined)),
  postprandial_glucose: z.number().min(0).optional().or(z.literal(undefined)),
  random_glucose: z.number().min(0).optional().or(z.literal(undefined)),
  egfr: z.number().min(0).optional().or(z.literal(undefined)),
  serum_creatinine: z.number().min(0).optional().or(z.literal(undefined)),
  urine_acr: z.number().min(0).optional().or(z.literal(undefined)),
  bp_systolic: z.number().int().min(50).max(260).optional().or(z.literal(undefined)),
  bp_diastolic: z.number().int().min(30).max(180).optional().or(z.literal(undefined)),
  bmi: z.number().min(10).max(70).optional().or(z.literal(undefined)),
  waist_circumference: z.number().min(30).max(200).optional().or(z.literal(undefined)),
  diabetes_type: z.enum(["TYPE_1", "TYPE_2", "GESTATIONAL", "MODY", "OTHER"]).optional(),
  complications: z.object({
    retinopathy: z.boolean().optional(),
    neuropathy: z.boolean().optional(),
    nephropathy: z.boolean().optional(),
    cad: z.boolean().optional(),
    pad: z.boolean().optional(),
    diabetic_foot: z.boolean().optional(),
  }).optional(),
  treatment_target: z.object({
    hba1c_target: z.number().optional().or(z.literal(undefined)),
    ldl_target: z.number().optional().or(z.literal(undefined)),
    bp_target: z.string().optional(),
  }).optional(),
  note: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface Props {
  defaultValues?: Partial<DiabetesAssessmentRequest>;
  onSubmit: (data: DiabetesAssessmentRequest) => void;
  isLoading?: boolean;
}

const DIABETES_TYPES: { value: DiabetesType; label: string }[] = [
  { value: "TYPE_1", label: "Đái tháo đường type 1" },
  { value: "TYPE_2", label: "Đái tháo đường type 2" },
  { value: "GESTATIONAL", label: "ĐTĐ thai kỳ" },
  { value: "MODY", label: "MODY" },
  { value: "OTHER", label: "Khác" },
];

const COMPLICATIONS = [
  { key: "retinopathy", label: "Võng mạc" },
  { key: "neuropathy", label: "Thần kinh" },
  { key: "nephropathy", label: "Thận" },
  { key: "cad", label: "Bệnh ĐM vành (CAD)" },
  { key: "pad", label: "Bệnh ĐM ngoại biên (PAD)" },
  { key: "diabetic_foot", label: "Bàn chân ĐTĐ" },
] as const;

export function DiabetesAssessmentForm({ defaultValues, onSubmit, isLoading }: Props) {
  const { register, handleSubmit, control, reset, formState: { errors } } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: defaultValues as FormValues ?? {},
  });

  useEffect(() => {
    if (defaultValues) reset(defaultValues as FormValues);
  }, [defaultValues, reset]);

  function handleFormSubmit(data: FormValues) {
    const payload: DiabetesAssessmentRequest = {
      ...data,
      hba1c: data.hba1c !== undefined ? Number(data.hba1c) : undefined,
      fasting_glucose: data.fasting_glucose !== undefined ? Number(data.fasting_glucose) : undefined,
      postprandial_glucose: data.postprandial_glucose !== undefined ? Number(data.postprandial_glucose) : undefined,
    };
    onSubmit(payload);
  }

  return (
    <form onSubmit={handleSubmit(handleFormSubmit)} className="space-y-6" noValidate>
      {/* HbA1c & Đường huyết */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Chỉ số đường huyết</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <FormField label="HbA1c (%)" error={errors.hba1c?.message}>
            <Input type="number" step="0.1" placeholder="7.0" {...register("hba1c", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Đường huyết đói (mg/dL)" error={errors.fasting_glucose?.message}>
            <Input type="number" step="0.1" placeholder="126" {...register("fasting_glucose", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Đường huyết sau ăn (mg/dL)" error={errors.postprandial_glucose?.message}>
            <Input type="number" step="0.1" placeholder="180" {...register("postprandial_glucose", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Đường huyết ngẫu nhiên (mg/dL)" error={errors.random_glucose?.message}>
            <Input type="number" step="0.1" placeholder="200" {...register("random_glucose", { valueAsNumber: true })} />
          </FormField>
        </CardContent>
      </Card>

      {/* Thận */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Chức năng thận</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <FormField label="eGFR (mL/min/1.73m²)">
            <Input type="number" step="0.1" placeholder="90" {...register("egfr", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Creatinine huyết thanh (μmol/L)">
            <Input type="number" step="0.1" placeholder="80" {...register("serum_creatinine", { valueAsNumber: true })} />
          </FormField>
          <FormField label="ACR nước tiểu (mg/g)">
            <Input type="number" step="0.1" placeholder="30" {...register("urine_acr", { valueAsNumber: true })} />
          </FormField>
        </CardContent>
      </Card>

      {/* Tim mạch & nhân trắc */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Tim mạch & Nhân trắc</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-2 gap-4">
          <FormField label="HA tâm thu (mmHg)">
            <Input type="number" placeholder="130" {...register("bp_systolic", { valueAsNumber: true })} />
          </FormField>
          <FormField label="HA tâm trương (mmHg)">
            <Input type="number" placeholder="80" {...register("bp_diastolic", { valueAsNumber: true })} />
          </FormField>
          <FormField label="BMI (kg/m²)">
            <Input type="number" step="0.1" placeholder="23" {...register("bmi", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Vòng eo (cm)">
            <Input type="number" step="0.1" placeholder="85" {...register("waist_circumference", { valueAsNumber: true })} />
          </FormField>
        </CardContent>
      </Card>

      {/* Type ĐTĐ */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Phân loại & Biến chứng</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <FormField label="Loại đái tháo đường">
            <Controller
              name="diabetes_type"
              control={control}
              render={({ field }) => (
                <Select onValueChange={(v) => field.onChange(v ?? "")} value={field.value ?? ""}>
                  <SelectTrigger>
                    <SelectValue placeholder="Chọn loại ĐTĐ" />
                  </SelectTrigger>
                  <SelectContent>
                    {DIABETES_TYPES.map((t) => (
                      <SelectItem key={t.value} value={t.value}>{t.label}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              )}
            />
          </FormField>

          <div>
            <Label className="text-sm mb-2 block">Biến chứng</Label>
            <div className="grid grid-cols-2 gap-2">
              {COMPLICATIONS.map(({ key, label }) => (
                <div key={key} className="flex items-center gap-2">
                  <Controller
                    name={`complications.${key}`}
                    control={control}
                    render={({ field }) => (
                      <Checkbox
                        id={`comp-${key}`}
                        checked={field.value ?? false}
                        onCheckedChange={field.onChange}
                      />
                    )}
                  />
                  <Label htmlFor={`comp-${key}`} className="text-sm cursor-pointer">{label}</Label>
                </div>
              ))}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Mục tiêu điều trị */}
      <Card>
        <CardHeader className="pb-3">
          <CardTitle className="text-base">Mục tiêu điều trị</CardTitle>
        </CardHeader>
        <CardContent className="grid grid-cols-3 gap-4">
          <FormField label="Đích HbA1c (%)">
            <Input type="number" step="0.1" placeholder="7.0" {...register("treatment_target.hba1c_target", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Đích LDL (mmol/L)">
            <Input type="number" step="0.1" placeholder="2.6" {...register("treatment_target.ldl_target", { valueAsNumber: true })} />
          </FormField>
          <FormField label="Đích HA">
            <Input placeholder="130/80" {...register("treatment_target.bp_target")} />
          </FormField>
        </CardContent>
      </Card>

      <FormField label="Ghi chú">
        <Textarea rows={3} {...register("note")} placeholder="Ghi chú thêm..." />
      </FormField>

      <Button type="submit" disabled={isLoading} className="min-h-[44px]">
        {isLoading ? "Đang lưu..." : "Lưu đánh giá ĐTĐ"}
      </Button>
    </form>
  );
}

function FormField({
  label,
  children,
  error,
}: {
  label: string;
  children: React.ReactNode;
  error?: string;
}) {
  return (
    <div className="space-y-1">
      <Label className="text-sm">{label}</Label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}
