"use client";

import { Card, CardContent } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import { HisStatusBadge } from "@/components/ui/status-badge";
import { SimpleAvatar } from "@/components/domain/SimpleAvatar";
import { formatVnDate } from "@/lib/utils/encounter-format";

export interface PatientIdentityCardProps {
  fullName: string;
  patientCode?: string | null;
  gender?: string | null;
  dob?: string | null;
  yearOfBirth?: number | null;
  avatarUrl?: string | null;
  phone?: string | null;
  bhytCardNo?: string | null;
  bhytValidTo?: string | null;
  doctorName?: string | null;
  roomName?: string | null;
  reasonForVisit?: string | null;
}

function genderLabel(gender?: string | null): string {
  if (!gender) return "Chưa rõ giới tính";
  const g = gender.toUpperCase();
  if (g === "MALE" || g === "M" || g === "NAM") return "Nam";
  if (g === "FEMALE" || g === "F" || g === "NỮ" || g === "NU") return "Nữ";
  return "Khác";
}

function calcAge(dob?: string | null, yearOfBirth?: number | null): string | null {
  if (dob) {
    const d = new Date(dob);
    if (!Number.isNaN(d.getTime())) {
      const now = new Date();
      let age = now.getFullYear() - d.getFullYear();
      const m = now.getMonth() - d.getMonth();
      if (m < 0 || (m === 0 && now.getDate() < d.getDate())) age -= 1;
      return `${age} tuổi`;
    }
  }
  if (yearOfBirth) return `${new Date().getFullYear() - yearOfBirth} tuổi`;
  return null;
}

function InfoRow({ label, value }: { label: string; value?: string | null }) {
  return (
    <div className="flex justify-between gap-2">
      <span className="text-muted-foreground shrink-0">{label}</span>
      <span className="text-right truncate" title={value ?? undefined}>
        {value || "—"}
      </span>
    </div>
  );
}

export function PatientIdentityCard({
  fullName,
  patientCode,
  gender,
  dob,
  yearOfBirth,
  avatarUrl,
  phone,
  bhytCardNo,
  bhytValidTo,
  doctorName,
  roomName,
  reasonForVisit,
}: PatientIdentityCardProps) {
  const age = calcAge(dob, yearOfBirth);
  const bhytExpired =
    !!bhytValidTo && new Date(bhytValidTo).getTime() < Date.now();

  return (
    <Card>
      <CardContent className="pt-4 space-y-3">
        <div className="flex items-center gap-3">
          <SimpleAvatar name={fullName} avatarUrl={avatarUrl} size="lg" />
          <div className="min-w-0">
            <p className="font-semibold truncate" title={fullName}>
              {fullName}
            </p>
            <p className="text-xs text-muted-foreground">
              {genderLabel(gender)}
              {age ? ` · ${age}` : ""}
            </p>
            {patientCode && (
              <p className="text-xs text-muted-foreground font-mono tabular-nums">{patientCode}</p>
            )}
            {phone && <p className="text-xs text-muted-foreground">{phone}</p>}
          </div>
        </div>

        <div>
          {bhytCardNo ? (
            <HisStatusBadge variant={bhytExpired ? "warning" : "insurance"}>
              {bhytExpired
                ? `BHYT hết hạn ${formatVnDate(bhytValidTo)}`
                : `BHYT còn hạn${bhytValidTo ? ` đến ${formatVnDate(bhytValidTo)}` : ""}`}
            </HisStatusBadge>
          ) : (
            <span className="inline-flex items-center rounded-full border border-border bg-muted/40 px-2 py-0.5 text-xs font-medium text-[color:var(--text-muted)]">
              Không có BHYT
            </span>
          )}
        </div>

        <Separator />

        <div className="space-y-1 text-sm">
          <InfoRow label="Bác sĩ" value={doctorName ?? "Chưa phân công"} />
          <InfoRow label="Phòng" value={roomName ?? "Chưa phân phòng"} />
          <InfoRow label="Lý do khám" value={reasonForVisit} />
        </div>
      </CardContent>
    </Card>
  );
}
