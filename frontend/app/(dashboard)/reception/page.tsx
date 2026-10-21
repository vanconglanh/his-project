"use client";

import { Suspense, useEffect, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import Link from "next/link";
import { UserPlus, ClipboardList, Users, Activity, CheckCircle, XCircle } from "lucide-react";
import { Button, buttonVariants } from "@/components/ui/button";
import { ServiceOnlyDialog } from "@/components/domain/ServiceOnlyDialog";
import { cn } from "@/lib/utils";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";
import { PageHeader } from "@/components/ui/page-header";
import { ReceptionCheckInForm, type PrefillAppointment } from "@/components/domain/ReceptionCheckInForm";
import { ReceptionQueueBoard } from "@/components/domain/ReceptionQueueBoard";
import { ReceptionTodayAppointments } from "@/components/domain/ReceptionTodayAppointments";
import { useReceptionStats } from "@/lib/hooks/use-reception";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { isNursingRole } from "@/lib/utils/roles";
import { toast } from "sonner";
import type { AppointmentTodayResponse } from "@/lib/api/types";

/** Đọc `?selectPatient=` (quay về từ /patients/new) rồi strip khỏi URL sau khi truyền xuống form. */
function ReceptionCheckInPanel({
  prefillAppointment,
  onPrefillAppointmentConsumed,
}: {
  prefillAppointment: PrefillAppointment | null;
  onPrefillAppointmentConsumed: () => void;
}) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const preselectPatientId = searchParams.get("selectPatient") ?? undefined;

  useEffect(() => {
    if (preselectPatientId) {
      router.replace("/reception", { scroll: false });
    }
  }, [preselectPatientId, router]);

  return (
    <ReceptionCheckInForm
      preselectPatientId={preselectPatientId}
      prefillAppointment={prefillAppointment}
      onPrefillAppointmentConsumed={onPrefillAppointmentConsumed}
    />
  );
}

export default function ReceptionPage() {
  const router = useRouter();
  const { data: stats, isLoading: statsLoading } = useReceptionStats();
  const { roles } = usePermissions();
  // BM-33: lich hen CONFIRMED duoc chon "Check-in" tu danh sach -> prefill vao form tiep don ben trai.
  const [prefillAppointment, setPrefillAppointment] = useState<PrefillAppointment | null>(null);

  const [serviceOnlyOpen, setServiceOnlyOpen] = useState(false);

  // BM-100: trước đây bấm "Check-in" trên lịch hẹn chỉ prefill form bên dưới, không có phản hồi
  // (và im lặng khi lịch hẹn chưa gắn hồ sơ bệnh nhân) -> lễ tân tưởng nút không bấm được.
  // Nay luôn phản hồi: cuộn tới form tiếp đón + toast hướng dẫn bước tiếp theo.
  const scrollToCheckInForm = () => {
    if (typeof document === "undefined") return;
    document
      .querySelector('[data-tour="reception-checkin-form"]')
      ?.scrollIntoView({ behavior: "smooth", block: "center" });
  };

  const handleCheckInFromAppointment = (appt: AppointmentTodayResponse) => {
    if (!appt.patient_ref) {
      toast.info(
        "Lịch hẹn chưa gắn hồ sơ bệnh nhân. Vui lòng tìm chọn bệnh nhân ở form tiếp đón bên dưới.",
      );
      scrollToCheckInForm();
      return;
    }
    setPrefillAppointment({ appointmentId: appt.appointment_id, patientId: appt.patient_ref });
    toast.success(
      `Đã chọn ${appt.patient_name ?? "bệnh nhân"} từ lịch hẹn. Chọn phòng khám rồi bấm Tiếp đón để bắt đầu khám.`,
    );
    scrollToCheckInForm();
  };
  // BM-05: Dieu duong/KTV chi theo doi + goi benh nhan trong "Bang hang doi", KHONG lam
  // nghiep vu tiep don (dang ky kham/tao benh nhan moi) - an han panel ben trai.
  const isNursing = isNursingRole(roles);

  // Keyboard shortcut F2 → navigate to /patients/new (khong ap dung cho Dieu duong/KTV,
  // dang ky benh nhan moi khong thuoc nghiep vu cua ho - BM-05).
  useEffect(() => {
    if (isNursing) return;
    const handler = (e: KeyboardEvent) => {
      if (e.key === "F2") {
        e.preventDefault();
        router.push("/patients/new?returnTo=/reception");
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [router, isNursing]);

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
      <PageHeader
        title={isNursing ? "Hàng đợi tiếp đón" : "Tiếp đón bệnh nhân"}
        description={
          isNursing
            ? "Theo dõi bệnh nhân đang chờ và đưa vào khám"
            : "Quản lý danh sách bệnh nhân chờ khám"
        }
        actions={
          !isNursing && (
            <div className="flex items-center gap-2">
              <Button
                type="button"
                variant="outline"
                className="gap-2"
                onClick={() => setServiceOnlyOpen(true)}
              >
                <ClipboardList className="h-4 w-4" />
                Không khám
              </Button>
              <Link
                href="/patients/new?returnTo=/reception"
                className={cn(buttonVariants({ variant: "default" }), "gap-2")}
                data-tour="reception-add-patient"
              >
                <UserPlus className="h-4 w-4" />
                Thêm bệnh nhân
                <kbd className="ml-1 text-xs opacity-60 border rounded px-1 py-0.5">F2</kbd>
              </Link>
            </div>
          )
        }
      />

      {/* Stats */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4" data-tour="reception-stats">
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

      {/* Lịch hẹn hôm nay (BM-33) - chỉ le tan moi tiep don, Dieu duong/KTV khong can thay */}
      {!isNursing && (
        <div className="border rounded-lg p-4 bg-card" data-tour="reception-today-appointments">
          <h3 className="font-semibold mb-4">Lịch hẹn hôm nay</h3>
          <ReceptionTodayAppointments onCheckIn={handleCheckInFromAppointment} />
        </div>
      )}

      {/* Main split layout - BM-05: Dieu duong/KTV chi thay "Bang hang doi", khong thay
          panel Tiep don (nghiep vu cua Le tan) nen dung 1 cot toan chieu rong. */}
      <div className={cn("grid grid-cols-1 gap-6", !isNursing && "lg:grid-cols-[360px_1fr]")}>
        {/* Left: Check-in form */}
        {!isNursing && (
          <div className="border rounded-lg p-4 bg-card" data-tour="reception-checkin-form">
            <Suspense fallback={<Skeleton className="h-96 w-full" />}>
              <ReceptionCheckInPanel
                prefillAppointment={prefillAppointment}
                onPrefillAppointmentConsumed={() => setPrefillAppointment(null)}
              />
            </Suspense>
          </div>
        )}

        {/* Right: Queue board */}
        <div className="border rounded-lg p-4 bg-card" data-tour="reception-queue">
          <h3 className="font-semibold mb-4">Bảng hàng đợi</h3>
          <ReceptionQueueBoard />
        </div>
      </div>

      <ServiceOnlyDialog open={serviceOnlyOpen} onOpenChange={setServiceOnlyOpen} />
    </div>
  );
}
