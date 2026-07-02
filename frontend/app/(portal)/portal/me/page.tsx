"use client";

import Link from "next/link";
import { Calendar, FileText, FlaskConical, Clock, User } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { PortalLayout } from "@/components/domain/PortalLayout";
import { usePortalMe, usePortalEncounters, usePortalAppointments } from "@/lib/hooks/use-portal";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";

export default function PortalMePage() {
  const { data: me, isLoading: meLoading } = usePortalMe();
  const { data: encounters } = usePortalEncounters({ page_size: 3 });
  const { data: appointments } = usePortalAppointments();

  const upcomingAppointments = appointments?.data?.filter(
    (a) => a.status === "BOOKED" || a.status === "CONFIRMED"
  ) ?? [];

  return (
    <PortalLayout>
      {/* Profile header */}
      <div className="mb-6 flex items-start gap-4 rounded-xl border bg-card p-5">
        <div className="flex h-14 w-14 items-center justify-center rounded-full bg-primary/10 shrink-0">
          <User className="h-7 w-7 text-primary" />
        </div>
        {meLoading ? (
          <div className="space-y-2 flex-1">
            <div className="h-5 w-40 animate-pulse rounded bg-muted" />
            <div className="h-4 w-32 animate-pulse rounded bg-muted" />
          </div>
        ) : me ? (
          <div>
            <h1 className="text-xl font-bold">{me.full_name}</h1>
            <p className="text-sm text-muted-foreground">
              Mã BN: <span className="font-medium">{me.patient_code}</span>
            </p>
            {me.bhyt_number && (
              <p className="text-xs text-muted-foreground mt-1">BHYT: {me.bhyt_number}</p>
            )}
            {me.dob && (
              <p className="text-xs text-muted-foreground">
                Ngày sinh: {format(parseISO(me.dob), "dd/MM/yyyy")}
              </p>
            )}
          </div>
        ) : null}
      </div>

      {/* Quick cards */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 mb-6">
        {[
          {
            href: "/portal/encounters",
            icon: FileText,
            label: "Lịch sử khám",
            count: encounters?.meta?.total,
          },
          { href: "/portal/prescriptions", icon: Clock, label: "Đơn thuốc", count: undefined },
          {
            href: "/portal/lab-results",
            icon: FlaskConical,
            label: "Kết quả XN",
            count: undefined,
          },
          {
            href: "/portal/appointments",
            icon: Calendar,
            label: "Lịch hẹn",
            count: upcomingAppointments.length || undefined,
          },
        ].map((card) => (
          <Link key={card.href} href={card.href}>
            <Card className="hover:shadow-md transition-shadow cursor-pointer h-full">
              <CardContent className="flex flex-col items-center justify-center py-5 gap-2 text-center">
                <card.icon className="h-7 w-7 text-primary" />
                <span className="text-sm font-medium leading-tight">{card.label}</span>
                {card.count !== undefined && (
                  <Badge variant="secondary" className="text-xs">
                    {card.count}
                  </Badge>
                )}
              </CardContent>
            </Card>
          </Link>
        ))}
      </div>

      {/* Recent encounters */}
      {encounters?.data && encounters.data.length > 0 && (
        <Card>
          <CardHeader className="pb-3">
            <div className="flex items-center justify-between">
              <CardTitle className="text-base">Lần khám gần nhất</CardTitle>
              <Link href="/portal/encounters" className="text-xs text-primary hover:underline">
                Xem tất cả
              </Link>
            </div>
          </CardHeader>
          <CardContent className="space-y-3">
            {encounters.data.map((enc) => (
              <Link key={enc.id} href={`/portal/encounters/${enc.id}`}>
                <div className="flex items-start justify-between rounded-lg border p-3 hover:bg-muted/30 transition-colors">
                  <div>
                    <p className="text-sm font-medium">{enc.doctor_name}</p>
                    <p className="text-xs text-muted-foreground mt-0.5">
                      {format(parseISO(enc.visited_at), "dd/MM/yyyy HH:mm", { locale: vi })}
                    </p>
                    {enc.chief_complaint && (
                      <p className="text-xs text-muted-foreground mt-1 line-clamp-1">
                        {enc.chief_complaint}
                      </p>
                    )}
                  </div>
                  <Badge variant="outline" className="text-xs shrink-0">
                    {enc.encounter_code}
                  </Badge>
                </div>
              </Link>
            ))}
          </CardContent>
        </Card>
      )}
    </PortalLayout>
  );
}
