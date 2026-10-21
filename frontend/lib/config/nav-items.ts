import {
  LayoutDashboard,
  UserPlus,
  CalendarClock,
  Users,
  Stethoscope,
  ClipboardList,
  Pill,
  Receipt,
  BarChart3,
  Settings,
  FlaskConical,
  ShieldCheck,
  Building2,
  Activity,
  BookOpen,
  TestTube2,
  Link2,
  Package,
  Warehouse,
  Store,
  FileText,
  AlertCircle,
  FileCheck2,
  Layers,
  Key,
  Bell,
  ShieldAlert,
  BellRing,
  Wand2,
  ArrowLeftRight,
  Landmark,
  ScanLine,
  ListTree,
  SlidersHorizontal,
  DoorOpen,
  Ticket,
} from "lucide-react";
import type { LucideIcon } from "lucide-react";

export interface NavItemConfig {
  href: string;
  labelKey: string;
  icon: LucideIcon;
  /** permission code(s) bắt buộc — undefined = hiển thị cho tất cả đã đăng nhập */
  permissions?: string[];
}

export interface NavGroupConfig {
  labelVi: string;
  items: NavItemConfig[];
}

export const NAV_GROUPS: NavGroupConfig[] = [
  {
    labelVi: "Khám bệnh",
    items: [
      {
        // BM-01 (Lark Bug-Feedback): "reception.read" la permission chet, khong controller nao
        // enforce (xem ReceptionController.cs - dung reception.checkin/queue.manage/rooms.read/
        // stats.read/ticket.reassign). Le Tan da co san reception.checkin nhung khong thay nav
        // "Tiep don" vi sai permission gate - cung pattern voi BM-24/BM-29.
        href: "/reception",
        labelKey: "reception",
        icon: UserPlus,
        // BM-58: bac_si co reception.queue.manage (goi BN vao kham) nhung khong co
        // reception.checkin (tiep nhan) nen menu bi an. Dung hasAny = hien khi co bat ky quyen nao.
        permissions: ["reception.checkin", "reception.queue.manage"],
      },
      {
        href: "/appointments",
        labelKey: "appointments",
        icon: CalendarClock,
        permissions: ["appointment.read"],
      },
      {
        href: "/patients",
        labelKey: "patients",
        icon: Users,
        permissions: ["patient.read"],
      },
      {
        href: "/encounters",
        labelKey: "encounters",
        icon: Stethoscope,
        permissions: ["encounter.read"],
      },
      {
        href: "/labrad",
        labelKey: "labrad",
        icon: FlaskConical,
        // BM-58: KTV co lab_result.read/rad_result.read nhung khong co lab.read/rad.read
        permissions: ["lab.read", "rad.read", "lab_order.read", "lab_result.read", "rad_order.read", "rad_result.read"],
      },
      {
        href: "/labrad/results",
        labelKey: "labradResults",
        icon: TestTube2,
        permissions: ["lab.read"],
      },
      {
        href: "/labrad/partners",
        labelKey: "labPartners",
        icon: Link2,
        permissions: ["lab.read"],
      },
      {
        href: "/nurse",
        labelKey: "nurse",
        icon: Activity,
        permissions: ["nursing.read"],
      },
      {
        href: "/diabetes/risk-list",
        labelKey: "riskList",
        icon: ShieldAlert,
        permissions: ["risk.read"],
      },
      {
        href: "/recall",
        labelKey: "recall",
        icon: BellRing,
        permissions: ["recall.read"],
      },
      {
        href: "/encounters/referrals",
        labelKey: "internalReferrals",
        icon: ArrowLeftRight,
        permissions: ["internal_referral.read"],
      },
    ],
  },
  {
    labelVi: "Dược",
    items: [
      {
        href: "/prescriptions",
        labelKey: "prescriptions",
        icon: ClipboardList,
        permissions: ["prescription.read"],
      },
      {
        href: "/pharmacy",
        labelKey: "pharmacy",
        icon: Pill,
        permissions: ["pharmacy.read"],
      },
      {
        href: "/pharmacy/dispense",
        labelKey: "dispense",
        icon: Package,
        // BM-29 (Lark Bug-Feedback): "dispense.read" la permission CHET - khong
        // controller nao enforce (PharmacyDispensingController dung
        // "dispense.queue" cho GET queue/history, "dispense.perform/reject/
        // return" cho cac hanh dong). Duoc si duoc migration 9139 cap du
        // dispense.perform/queue/reject/return nhung KHONG co dispense.read nen
        // menu "Phat thuoc" khong hien du co the thao tac binh thuong qua API.
        // Doi gate sang "dispense.queue" - dung permission BE that su enforce
        // cho man hinh hang cho phat thuoc.
        permissions: ["dispense.queue"],
      },
      {
        href: "/drugs",
        labelKey: "drugs",
        icon: Store,
        permissions: ["drug.read"],
      },
      {
        href: "/pharmacy/stock-transfers",
        labelKey: "stockTransfers",
        icon: ArrowLeftRight,
        permissions: ["stock_transfer.read"],
      },
    ],
  },
  {
    labelVi: "Tài chính",
    items: [
      {
        href: "/cashier",
        labelKey: "cashier",
        icon: Receipt,
        // BM-58: ke_toan co cashier.shift_open/payment.collect nhung khong co cashier.read
        permissions: ["cashier.read", "cashier.shift_open", "payment.collect"],
      },
      {
        // BM-24 (Lark Bug-Feedback): "cashier.read" la permission chet, khong co controller nao
        // enforce. Endpoint that su GET /api/v1/billings/pending-encounters dung "billing.read"
        // (BillingsController.PendingEncounters) - Le Tan/Ke Toan da co san quyen nay nhung
        // khong thay nav vi sai permission gate, dan den khong biet man hinh "Hang cho thu ngan"
        // ton tai (noi chua danh sach BN da duoc chi dinh dich vu, cho lap hoa don).
        href: "/cashier/pending",
        labelKey: "cashierPending",
        icon: ClipboardList,
        permissions: ["billing.read"],
      },
      {
        href: "/billings",
        labelKey: "billings",
        icon: FileText,
        permissions: ["billing.read"],
      },
      {
        href: "/cashier/debts",
        labelKey: "debts",
        icon: AlertCircle,
        permissions: ["cashier.read"],
      },
      {
        href: "/services",
        labelKey: "services",
        icon: Layers,
        permissions: ["service.read"],
      },
      {
        href: "/bhyt",
        labelKey: "bhyt",
        icon: ShieldCheck,
        permissions: ["bhyt.read"],
      },
      {
        href: "/cashier/inter-branch-debts",
        labelKey: "interBranchDebts",
        icon: ArrowLeftRight,
        permissions: ["inter_branch_debt.read"],
      },
      {
        href: "/cashier/bank-reconciliation",
        labelKey: "bankReconciliation",
        icon: Landmark,
        permissions: ["payment.read"],
      },
    ],
  },
  {
    labelVi: "Phân tích",
    items: [
      {
        href: "/",
        labelKey: "overview",
        icon: LayoutDashboard,
        // Dashboard: mọi role đã đăng nhập đều thấy
        permissions: undefined,
      },
      {
        href: "/reports",
        labelKey: "reports",
        icon: BarChart3,
        // BM-56: Dược sĩ chỉ còn report.pharmacy (đã gỡ report.read) -> vẫn thấy trang có tab Kho dược
        permissions: ["report.read", "report.pharmacy"],
      },
      {
        href: "/reports/builder",
        labelKey: "reportBuilder",
        icon: Wand2,
        permissions: ["report.build"],
      },
      {
        href: "/reports/dashboards",
        labelKey: "reportDashboards",
        icon: LayoutDashboard,
        permissions: ["report.read"],
      },
      {
        href: "/reports/chain-dashboard",
        labelKey: "chainDashboard",
        icon: Building2,
        // BM-10: doi tu "dashboard.read" (cap rong cho moi role, ke ca ky_thuat_vien)
        // sang "report.read" - khop voi permission BE that su enforce (xem
        // DashboardController.GetBranchRanking/GetBranchDetail).
        // BM-01 (tiep, Lark 2026-09-16): "report.read" van chua du - le_tan cung co
        // report.read (de xem bao cao doanh thu ngay) nen lai lot vao duoc trang nay
        // (du lieu xuyen chi nhanh, ngoai pham vi le tan). Doi tiep sang
        // "branch.group_view" - khop voi permission BE that su enforce sau khi sua.
        permissions: ["branch.group_view"],
      },
      {
        href: "/reports/schedules",
        labelKey: "reportSchedules",
        icon: CalendarClock,
        permissions: ["report.build"],
      },
    ],
  },
  {
    labelVi: "Hệ thống",
    items: [
      {
        href: "/admin",
        labelKey: "admin",
        icon: Settings,
        permissions: ["tenant.read"],
      },
      {
        href: "/admin/tenants",
        labelKey: "tenants",
        icon: Building2,
        permissions: ["tenant.read"],
      },
      {
        href: "/admin/branches",
        labelKey: "branches",
        icon: Building2,
        permissions: ["tenant.read"],
      },
      {
        href: "/admin/rooms",
        labelKey: "rooms",
        icon: DoorOpen,
        // BM-93: RoomsController enforce room.read/room.write/room.delete that su
        permissions: ["room.read"],
      },
      {
        href: "/admin/vouchers",
        labelKey: "vouchers",
        icon: Ticket,
        // BM-95: VouchersController enforce voucher.read/write/delete that su
        permissions: ["voucher.read"],
      },
      {
        href: "/admin/users",
        labelKey: "users",
        icon: Users,
        // BM-01 (tiep, Lark 2026-09-16): "user.read" duoc cap rong cho ca le_tan/bac_si
        // (BE can permission nay cho GET /users de lam dropdown chon bac_si khi dat
        // lich/tao luot kham - xem ghi chu B5 trong migration 9141) nen menu quan tri
        // "Nguoi dung" bi lo ra ngay ca voi Le tan du khong thuoc pham vi cong viec.
        // Doi gate cua MENU (khong dong voi API GET /users) sang "user.write" - chi
        // admin moi co, dung dung y "vao duoc man quan tri nguoi dung" thay vi "goi
        // duoc API doc danh sach". KHONG thu hoi user.read cua le_tan/bac_si vi se
        // hong dropdown chon bac_si (can lam endpoint /doctors/lookup rieng truoc,
        // xem 9141 B5 - chua lam trong task nay).
        permissions: ["user.write"],
      },
      {
        href: "/admin/roles",
        labelKey: "roles",
        icon: ShieldCheck,
        permissions: ["role.read"],
      },
      {
        href: "/admin/audit",
        labelKey: "audit",
        icon: ClipboardList,
        permissions: ["audit.read"],
      },
      {
        href: "/admin/emr-templates",
        labelKey: "emrTemplates",
        icon: BookOpen,
        permissions: ["encounter.write"],
      },
      {
        href: "/admin/dtqg",
        labelKey: "dtqg",
        icon: Warehouse,
        permissions: ["drug.write"],
      },
      {
        href: "/admin/suppliers",
        labelKey: "suppliers",
        icon: Package,
        permissions: ["pharmacy.write"],
      },
      {
        href: "/admin/einvoice",
        labelKey: "einvoice",
        icon: FileCheck2,
        permissions: ["billing.write"],
      },
      {
        href: "/admin/api-partners",
        labelKey: "apiPartners",
        icon: Key,
        permissions: ["tenant.read"],
      },
      {
        href: "/admin/notifications-config",
        labelKey: "notificationsConfig",
        icon: Bell,
        permissions: ["tenant.read"],
      },
      {
        href: "/admin/notification-channels",
        labelKey: "notificationChannels",
        icon: BellRing,
        permissions: ["notification_channel.read"],
      },
      {
        href: "/admin/legacy-import",
        labelKey: "legacyImport",
        icon: ScanLine,
        permissions: ["legacy_import.write"],
      },
      {
        href: "/admin/master-codes",
        labelKey: "masterCodes",
        icon: ListTree,
        permissions: ["code.read"],
      },
      {
        href: "/admin/settings",
        labelKey: "systemSettings",
        icon: SlidersHorizontal,
        permissions: ["setting.manage"],
      },
    ],
  },
];
