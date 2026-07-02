"use client";

import type {
  UseFormRegister,
  FieldErrors,
  UseFormWatch,
  UseFormSetValue,
} from "react-hook-form";
import type { PatientFormValues } from "./patient-schema";

interface PatientEmergencyTabProps {
  register: UseFormRegister<PatientFormValues>;
  errors: FieldErrors<PatientFormValues>;
  watch: UseFormWatch<PatientFormValues>;
  setValue: UseFormSetValue<PatientFormValues>;
}

export function PatientEmergencyTab(_props: PatientEmergencyTabProps) {
  return (
    <div className="flex flex-col items-center justify-center py-16 text-muted-foreground text-sm gap-2">
      <svg
        xmlns="http://www.w3.org/2000/svg"
        className="h-12 w-12 text-muted-foreground/40"
        fill="none"
        viewBox="0 0 24 24"
        stroke="currentColor"
        strokeWidth={1}
      >
        <path
          strokeLinecap="round"
          strokeLinejoin="round"
          d="M15.182 15.182a4.5 4.5 0 01-6.364 0M21 12a9 9 0 11-18 0 9 9 0 0118 0zM9.75 9.75c0 .414-.168.75-.375.75S9 10.164 9 9.75 9.168 9 9.375 9s.375.336.375.75zm-.375 0h.008v.015h-.008V9.75zm5.625 0c0 .414-.168.75-.375.75s-.375-.336-.375-.75.168-.75.375-.75.375.336.375.75zm-.375 0h.008v.015h-.008V9.75z"
        />
      </svg>
      <p className="font-medium text-foreground">Liên hệ khẩn cấp</p>
      <p className="text-center max-w-sm">
        Liên hệ khẩn cấp có thể thêm sau khi tạo bệnh nhân xong, thông qua tab{" "}
        <span className="font-medium text-foreground">Liên hệ khẩn cấp</span> trong hồ sơ bệnh nhân.
      </p>
    </div>
  );
}
