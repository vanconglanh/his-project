"use client";

import type {
  UseFormRegister,
  FieldErrors,
  UseFormWatch,
  UseFormSetValue,
} from "react-hook-form";
import type { PatientFormValues } from "./patient-schema";

interface PatientAllergiesTabProps {
  register: UseFormRegister<PatientFormValues>;
  errors: FieldErrors<PatientFormValues>;
  watch: UseFormWatch<PatientFormValues>;
  setValue: UseFormSetValue<PatientFormValues>;
}

export function PatientAllergiesTab(_props: PatientAllergiesTabProps) {
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
          d="M12 9v3.75m-9.303 3.376c-.866 1.5.217 3.374 1.948 3.374h14.71c1.73 0 2.813-1.874 1.948-3.374L13.949 3.378c-.866-1.5-3.032-1.5-3.898 0L2.697 16.126zM12 15.75h.007v.008H12v-.008z"
        />
      </svg>
      <p className="font-medium text-foreground">Thông tin dị ứng</p>
      <p className="text-center max-w-sm">
        Thông tin dị ứng có thể thêm sau khi tạo bệnh nhân xong, thông qua tab{" "}
        <span className="font-medium text-foreground">Dị ứng</span> trong hồ sơ bệnh nhân.
      </p>
    </div>
  );
}
