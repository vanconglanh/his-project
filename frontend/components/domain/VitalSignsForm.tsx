"use client";

import { useEffect, useRef } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import type { VitalSignsRequest } from "@/lib/api/types";
import { GlucoseUnitToggle } from "@/components/domain/GlucoseUnitToggle";
import { useGlucoseUnitStore } from "@/lib/stores/glucose-unit-store";
import {
  convertGlucose,
  mgdlToMmol,
  mmolToMgdl,
  roundGlucose,
  type GlucoseUnit,
} from "@/lib/utils/glucose";

// BM-69: glucose nhập/hiển thị theo đơn vị người dùng chọn (mặc định mmol/L),
// nhưng LƯU vẫn là mg/dL (cột glucose_mg_dl — nguồn sự thật, không đổi schema).
// Ngưỡng hợp lệ lưu trữ: 20-800 mg/dL ⇔ ~1.1-44.4 mmol/L.
const schema = z.object({
  temperature_c: z.number().min(30).max(45).optional().or(z.literal(undefined)),
  heart_rate_bpm: z.number().int().min(20).max(250).optional().or(z.literal(undefined)),
  respiratory_rate: z.number().int().min(5).max(60).optional().or(z.literal(undefined)),
  bp_systolic: z.number().int().min(50).max(260).optional().or(z.literal(undefined)),
  bp_diastolic: z.number().int().min(30).max(180).optional().or(z.literal(undefined)),
  spo2_percent: z.number().int().min(50).max(100).optional().or(z.literal(undefined)),
  weight_kg: z.number().min(1).max(300).optional().or(z.literal(undefined)),
  height_cm: z.number().min(30).max(250).optional().or(z.literal(undefined)),
  waist_cm: z.number().min(20).max(300).optional().or(z.literal(undefined)),
  pain_scale: z.number().int().min(0).max(10).optional().or(z.literal(undefined)),
  // Giá trị đường huyết theo đơn vị đang chọn trên UI (không phải đơn vị lưu).
  glucose_display: z.number().positive().optional().or(z.literal(undefined)),
  note: z.string().optional(),
});

// Ô số bỏ trống -> undefined (KHÔNG để valueAsNumber sinh NaN làm z.number() fail,
// chặn submit khi bác sĩ/điều dưỡng chỉ nhập vài chỉ số). Dùng thay numReg.
const numReg = { setValueAs: (v: unknown) => (v === "" || v === null || v === undefined ? undefined : Number(v)) };

type FormValues = z.infer<typeof schema>;

interface Props {
  onSubmit: (data: VitalSignsRequest) => void;
  onSubmitAndNext?: (data: VitalSignsRequest) => void;
  isLoading?: boolean;
  // defaultValues theo shape LƯU (glucose_mg_dl mg/dL); glucose sẽ được quy đổi
  // sang đơn vị đang chọn để hiển thị trên ô nhập.
  defaultValues?: Partial<VitalSignsRequest>;
}

function numOrUndef(val: unknown): number | undefined {
  const n = Number(val);
  return isNaN(n) || val === "" || val === null || val === undefined ? undefined : n;
}

// Chuyển defaultValues (lưu mg/dL) -> FormValues (glucose_display theo đơn vị chọn).
function toFormValues(
  dv: Partial<VitalSignsRequest> | undefined,
  unit: GlucoseUnit
): Partial<FormValues> {
  if (!dv) return {};
  const { glucose_mg_dl, ...rest } = dv;
  const glucose_display =
    glucose_mg_dl != null
      ? roundGlucose(unit === "mmol/L" ? mgdlToMmol(glucose_mg_dl) : glucose_mg_dl, unit)
      : undefined;
  return { ...rest, glucose_display };
}

export function VitalSignsForm({ onSubmit, onSubmitAndNext, isLoading, defaultValues }: Props) {
  const glucoseUnit = useGlucoseUnitStore((s) => s.unit);

  const {
    register,
    handleSubmit,
    watch,
    reset,
    getValues,
    setValue,
    formState: { errors },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: toFormValues(defaultValues, glucoseUnit),
  });

  useEffect(() => {
    if (defaultValues) reset(toFormValues(defaultValues, glucoseUnit));
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [defaultValues, reset]);

  // Khi người dùng đổi đơn vị: quy đổi giá trị đang nhập sang đơn vị mới để
  // GIỮ NGUYÊN giá trị lâm sàng (chỉ đổi cách biểu diễn).
  const prevUnit = useRef<GlucoseUnit>(glucoseUnit);
  useEffect(() => {
    if (prevUnit.current === glucoseUnit) return;
    const current = getValues("glucose_display");
    if (current != null && !Number.isNaN(current)) {
      setValue(
        "glucose_display",
        roundGlucose(convertGlucose(Number(current), prevUnit.current, glucoseUnit), glucoseUnit)
      );
    }
    prevUnit.current = glucoseUnit;
  }, [glucoseUnit, getValues, setValue]);

  const weight = watch("weight_kg");
  const height = watch("height_cm");
  const bmi =
    weight && height
      ? (Number(weight) / Math.pow(Number(height) / 100, 2)).toFixed(1)
      : null;

  function coerce(data: FormValues): VitalSignsRequest {
    // LƯU luôn theo mg/dL (nguồn sự thật, không đổi schema) dù UI chọn đơn vị nào.
    const display = numOrUndef(data.glucose_display);
    const glucose_mg_dl =
      display != null
        ? Math.round((glucoseUnit === "mmol/L" ? mmolToMgdl(display) : display) * 100) / 100
        : undefined;
    return {
      temperature_c: numOrUndef(data.temperature_c),
      heart_rate_bpm: numOrUndef(data.heart_rate_bpm),
      respiratory_rate: numOrUndef(data.respiratory_rate),
      bp_systolic: numOrUndef(data.bp_systolic),
      bp_diastolic: numOrUndef(data.bp_diastolic),
      spo2_percent: numOrUndef(data.spo2_percent),
      weight_kg: numOrUndef(data.weight_kg),
      height_cm: numOrUndef(data.height_cm),
      waist_cm: numOrUndef(data.waist_cm),
      pain_scale: numOrUndef(data.pain_scale),
      glucose_mg_dl,
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
        <Field id="vital-temperature" label="Nhiệt độ (°C)" error={errors.temperature_c?.message}>
          <Input
            id="vital-temperature"
            type="number"
            step="0.1"
            placeholder="36.5"
            {...register("temperature_c", numReg)}
          />
        </Field>
        <Field id="vital-heart-rate" label="Mạch (lần/phút)" error={errors.heart_rate_bpm?.message}>
          <Input
            id="vital-heart-rate"
            type="number"
            placeholder="80"
            {...register("heart_rate_bpm", numReg)}
          />
        </Field>
        <Field id="vital-bp-systolic" label="HA tâm thu (mmHg)" error={errors.bp_systolic?.message}>
          <Input
            id="vital-bp-systolic"
            type="number"
            placeholder="120"
            {...register("bp_systolic", numReg)}
          />
        </Field>
        <Field id="vital-bp-diastolic" label="HA tâm trương (mmHg)" error={errors.bp_diastolic?.message}>
          <Input
            id="vital-bp-diastolic"
            type="number"
            placeholder="80"
            {...register("bp_diastolic", numReg)}
          />
        </Field>
        <Field id="vital-spo2" label="SpO2 (%)" error={errors.spo2_percent?.message}>
          <Input
            id="vital-spo2"
            type="number"
            placeholder="98"
            {...register("spo2_percent", numReg)}
          />
        </Field>
        <Field id="vital-respiratory-rate" label="Nhịp thở (lần/phút)" error={errors.respiratory_rate?.message}>
          <Input
            id="vital-respiratory-rate"
            type="number"
            placeholder="16"
            {...register("respiratory_rate", numReg)}
          />
        </Field>
        <Field id="vital-weight" label="Cân nặng (kg)" error={errors.weight_kg?.message}>
          <Input
            id="vital-weight"
            type="number"
            step="0.1"
            placeholder="60"
            {...register("weight_kg", numReg)}
          />
        </Field>
        <Field id="vital-height" label="Chiều cao (cm)" error={errors.height_cm?.message}>
          <Input
            id="vital-height"
            type="number"
            step="0.1"
            placeholder="165"
            {...register("height_cm", numReg)}
          />
        </Field>
        <Field id="vital-waist" label="Vòng eo (cm)" error={errors.waist_cm?.message}>
          <Input
            id="vital-waist"
            type="number"
            step="0.1"
            placeholder="80"
            {...register("waist_cm", numReg)}
          />
        </Field>
        <Field id="vital-pain-scale" label="Đau (0-10)" error={errors.pain_scale?.message}>
          <Input
            id="vital-pain-scale"
            type="number"
            min={0}
            max={10}
            placeholder="0"
            {...register("pain_scale", numReg)}
          />
        </Field>
        <Field
          id="vital-glucose"
          label={`Đường huyết (${glucoseUnit})`}
          error={errors.glucose_display?.message}
        >
          <div className="space-y-1.5">
            <Input
              id="vital-glucose"
              type="number"
              step={glucoseUnit === "mmol/L" ? "0.1" : "1"}
              placeholder={glucoseUnit === "mmol/L" ? "5.6" : "100"}
              {...register("glucose_display", numReg)}
            />
            <GlucoseUnitToggle label="Đơn vị:" />
          </div>
        </Field>
      </div>

      {bmi && (
        <p className="text-sm text-muted-foreground">
          BMI tính tự động: <strong>{bmi}</strong> kg/m²
        </p>
      )}

      <Field id="vital-note" label="Ghi chú" error={errors.note?.message}>
        <Textarea id="vital-note" placeholder="Ghi chú thêm..." rows={2} {...register("note")} />
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
  id,
  label,
  children,
  error,
}: {
  id: string;
  label: string;
  children: React.ReactNode;
  error?: string;
}) {
  return (
    <div className="space-y-1">
      <Label htmlFor={id} className="text-sm">{label}</Label>
      {children}
      {error && <p className="text-xs text-destructive">{error}</p>}
    </div>
  );
}

