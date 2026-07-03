"use client";

import { Suspense, useEffect } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { UserPlus, Users, Activity, CheckCircle, XCircle } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { ReceptionCheckInForm } from "@/components/domain/ReceptionCheckInForm";
import { ReceptionQueueBoard } from "@/components/domain/ReceptionQueueBoard";
import { useReceptionStats } from "@/lib/hooks/use-reception";

/** Đọc `?selectPatient=` (quay về từ /patients/new) rồi strip khỏi URL sau khi truyền xuống form. */
function ReceptionCheckInPanel() {
  const router = useRouter();
  const searchParams = useSearchParams();
  const preselectPatientId = searchParams.get("selectPatient") ?? undefined;

  useEffect(() => {
    if (preselectPatientId) {
      router.replace("/reception", { scroll: false });
    }
  }, [preselectPatientId, router]);

  return <ReceptionCheckInForm preselectPatientId={preselectPatientId} />;
}

export default function ReceptionPage() {
  const router = useRouter();
  const { data: stats, isLoading: statsLoading } = useReceptionStats();

  // Keyboard shortcut F2 → navigate to /patients/new
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === "F2") {
        e.preventDefault();
        router.push("/patients/new?returnTo=/reception");
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [router]);

  const statsCards = [
    {
      label: "Đang chờ",
      value: stats?.waiting,
      icon: Users,
      color: "text-yellow-600",
    },
    {
      label: "Đang khám",
      value: stats?.in_progress,
      icon: Activity,
      color: "text-blue-600",
    },
    {
      label: "Đã khám hôm nay",
      value: stats?.done,
      icon: CheckCircle,
      color: "text-green-600",
    },
    {
      label: "Đã huỷ",
      value: stats?.cancelled,
      icon: XCircle,
      color: "text-muted-foreground",
    },
  ];

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Tiếp đón bệnh nhân</h2>
          <p className="text-sm text-muted-foreground">
            Quản lý danh sách bệnh nhân chờ khám
          </p>
        </div>
        <Link href="/patients/new?returnTo=/reception" className={cn(buttonVariants({ variant: "default" }), "gap-2")}>
          <UserPlus className="h-4 w-4" />
          Thêm bệnh nhân
          <kbd className="ml-1 text-xs opacity-60 border rounded px-1 py-0.5">F2</kbd>
        </Link>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
        {statsCards.map(({ label, value, icon: Icon, color }) => (
          <Card key={label}>
            <CardHeader className="flex flex-row items-center justify-between pb-2 space-y-0">
              <CardTitle className="text-sm font-medium text-muted-foreground">{label}</CardTitle>
              <Icon className={`h-4 w-4 ${color}`} />
            </CardHeader>
            <CardContent>
              {statsLoading ? (
                <Skeleton className="h-8 w-16" />
              ) : (
                <p className="text-[length:var(--text-kpi)] leading-[var(--text-kpi--line-height)] font-bold tabular-nums">{value ?? 0}</p>
              )}
            </CardContent>
          </Card>
        ))}
      </div>

      {/* Main split layout */}
      <div className="grid grid-cols-1 lg:grid-cols-[360px_1fr] gap-6">
        {/* Left: Check-in form */}
        <div className="border rounded-lg p-4 bg-card">
          <Suspense fallback={<Skeleton className="h-96 w-full" />}>
            <ReceptionCheckInPanel />
          </Suspense>
        </div>

        {/* Right: Queue board */}
        <div className="border rounded-lg p-4 bg-card">
          <h3 className="font-semibold mb-4">Bảng hàng đợi</h3>
          <ReceptionQueueBoard />
        </div>
      </div>

    </div>
  );
}
