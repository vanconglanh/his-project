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
        href: "/reception",
        labelKey: "reception",
        icon: UserPlus,
        permissions: ["reception.read"],
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
        permissions: ["lab.read", "rad.read"],
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
        permissions: ["dispense.read"],
      },
      {
        href: "/drugs",
        labelKey: "drugs",
        icon: Store,
        permissions: ["drug.read"],
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
        permissions: ["cashier.read"],
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
        permissions: ["report.read"],
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
        href: "/admin/users",
        labelKey: "users",
        icon: Users,
        permissions: ["user.read"],
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
    ],
  },
];
