"use client";

import { useEffect } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import type { VitalSignsRequest } from "@/lib/api/types";



const schema = z.object({
  temperature_c: z.number().min(30).max(45).optional().or(z.literal(undefined)),
  heart_rate_bpm: z.number().int().min(20).max(250).optional().or(z.literal(undefined)),
  respiratory_rate: z.number().int().min(5).max(60).optional().or(z.literal(undefined)),
  bp_systolic: z.number().int().min(50).max(260).optional().or(z.literal(undefined)),
  bp_diastolic: z.number().int().min(30).max(180).optional().or(z.literal(undefined)),
  spo2_percent: z.number().int().min(50).max(100).optional().or(z.literal(undefined)),
  weight_kg: z.number().min(1).max(300).optional().or(z.literal(undefined)),
  height_cm: z.number().min(30).max(250).optional().or(z.literal(undefined)),
  pain_scale: z.number().int().min(0).max(10).optional().or(z.literal(undefined)),
  glucose_mg_dl: z.number().min(20).max(800).optional().or(z.literal(undefined)),
  note: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

interface Props {
  onSubmit: (data: VitalSignsRequest) => void;
  onSubmitAndNext?: (data: VitalSignsRequest) => void;
  isLoading?: boolean;
  defaultValues?: Partial<FormValues>;
}

function numOrUndef(val: unknown): number | undefined {
  const n = Number(val);
  return isNaN(n) || val === "" || val === null || val === undefined ? undefined : n;
}

export function VitalSignsForm({ onSubmit, onSubmitAndNext, isLoading, defaultValues }: Props) {
  const {
    register,
    handleSubmit,
    watch,
    reset,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: defaultValues ?? {},
  });

  useEffect(() => {
    if (defaultValues) reset(defaultValues);
  }, [defaultValues, reset]);

  const weight = watch("weight_kg");
  const height = watch("height_cm");
  const bmi =
    weight && height
      ? (Number(weight) / Math.pow(Number(height) / 100, 2)).toFixed(1)
      : null;

  function coerce(data: FormValues): VitalSignsRequest {
    return {
      temperature_c: numOrUndef(data.temperature_c),
      heart_rate_bpm: numOrUndef(data.heart_rate_bpm),
      respiratory_rate: numOrUndef(data.respiratory_rate),
      bp_systolic: numOrUndef(data.bp_systolic),
      bp_diastolic: numOrUndef(data.bp_diastolic),
      spo2_percent: numOrUndef(data.spo2_percent),
      weight_kg: numOrUndef(data.weight_kg),
      height_cm: numOrUndef(data.height_cm),
      pain_scale: numOrUndef(data.pain_scale),
      glucose_mg_dl: numOrUndef(data.glucose_mg_dl),
      note: data.note,
    };
  }

  return (
    <form
      onSubmit={handleSubmit((data) => onSubmit(coerce(data)))}
      className="space-y-4"
      noValidate
    >
      <div className="grid grid-cols-2 gap-4">
        <Field label="Nhiệt độ (°C)" error={errors.temperature_c?.message}>
          <Input
            type="number"
            step="0.1"
            placeholder="36.5"
            {...register("temperature_c", { valueAsNumber: true })}
          />
        </Field>
        <Field label="Mạch (lần/phút)" error={errors.heart_rate_bpm?.message}>
          <Input
            type="number"
            placeholder="80"
            {...register("heart_rate_bpm", { valueAsNumber: true })}
          />
        </Field>
        <Field label="HA tâm thu (mmHg)" error={errors.bp_systolic?.message}>
          <Input
            type="number"
            placeholder="120"
            {...register("bp_systolic", { valueAsNumber: true })}
          />
        </Field>
        <Field label="HA tâm trương (mmHg)" error={errors.bp_diastolic?.message}>
          <Input
            type="number"
            placeholder="80"
            {...register("bp_diastolic", { valueAsNumber: true })}
          />
        </Field>
        <Field label="SpO2 (%)" error={errors.spo2_percent?.message}>
          <Input
            type="number"
            placeholder="98"
            {...register("spo2_percent", { valueAsNumber: true })}
          />
        </Field>
        <Field label="Nhịp thở (lần/phút)" error={errors.respiratory_rate?.message}>
          <Input
            type="number"
            placeholder="16"
            {...register("respiratory_rate", { valueAsNumber: true })}
          />
        </Field>
        <Field label="Cân nặng (kg)" error={errors.weight_kg?.message}>
          <Input
            type="number"
            step="0.1"
            placeholder="60"
            {...register("weight_kg", { valueAsNumber: true })}
          />
        </Field>
        <Field label="Chiều cao (cm)" error={errors.height_cm?.message}>
          <Input
            type="number"
            step="0.1"
            placeholder="165"
            {...register("height_cm", { valueAsNumber: true })}
          />
        </Field>
        <Field label="Đau (0-10)" error={errors.pain_scale?.message}>
          <Input
            type="number"
            min={0}
            max={10}
            placeholder="0"
            {...register("pain_scale", { valueAsNumber: true })}
          />
        </Field>
        <Field label="Đường huyết (mg/dL)" error={errors.glucose_mg_dl?.message}>
          <Input
            type="number"
            step="0.1"
            placeholder="100"
            {...register("glucose_mg_dl", { valueAsNumber: true })}
          />
        </Field>
      </div>

      {bmi && (
        <p className="text-sm text-muted-foreground">
          BMI tính tự động: <strong>{bmi}</strong> kg/m²
        </p>
      )}

      <Field label="Ghi chú" error={errors.note?.message}>
        <Textarea placeholder="Ghi chú thêm..." rows={2} {...register("note")} />
      </Field>

      <div className="flex gap-3 pt-2">
        <Button type="submit" disabled={isLoading} className="min-h-[44px]">
          {isLoading ? "Đang lưu..." : "Lưu sinh hiệu"}
        </Button>
        {onSubmitAndNext && (
          <Button
            type="button"
            variant="outline"
            disabled={isLoading}
            className="min-h-[44px]"
            onClick={handleSubmit((data) => onSubmitAndNext(coerce(data)))}
          >
            Lưu và nhập tiếp
          </Button>
        )}
      </div>
    </form>
  );
}

function Field({
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

