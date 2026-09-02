"use client";

import Link from "next/link";
import { useTranslations } from "next-intl";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { NAV_GROUPS } from "@/lib/config/nav-items";

/**
 * Mo ta ngan (tieng Viet) cho tung khu vuc quan tri, key theo href.
 * Giup trang hub /admin tro thanh diem vao ro rang thay vi placeholder rong.
 */
const AREA_DESCRIPTIONS: Record<string, string> = {
  "/admin/tenants": "Quản lý phòng khám (tenant) trong hệ thống SaaS, cấu hình BHYT, mã CSKCB.",
  "/admin/branches": "Quản lý chi nhánh, cơ sở khám chữa bệnh thuộc phòng khám.",
  "/admin/branch-pricing": "Thiết lập bảng giá dịch vụ riêng theo từng chi nhánh.",
  "/admin/users": "Tạo và quản lý tài khoản người dùng, gán vai trò cho nhân viên.",
  "/admin/roles": "Định nghĩa vai trò và phân quyền (RBAC) chi tiết theo chức năng.",
  "/admin/audit": "Nhật ký kiểm toán mọi thao tác trên dữ liệu bệnh nhân, đăng nhập, cấu hình.",
  "/admin/emr-templates": "Mẫu bệnh án điện tử (EMR) dùng lại khi khám bệnh.",
  "/admin/dtqg": "Cấu hình tích hợp Đơn thuốc Quốc gia (donthuocquocgia.vn).",
  "/admin/suppliers": "Danh mục nhà cung cấp thuốc, vật tư cho kho dược.",
  "/admin/einvoice": "Cấu hình phát hành hóa đơn điện tử.",
  "/admin/api-partners": "Quản lý khóa API và đối tác tích hợp bên ngoài.",
  "/admin/notifications-config": "Cấu hình các loại thông báo hệ thống gửi cho người dùng.",
  "/admin/notification-channels": "Quản lý kênh gửi thông báo (email, SMS, in-app...).",
  "/admin/legacy-import": "Nhập dữ liệu từ hệ thống cũ vào Pro-Diab HIS.",
  "/admin/master-codes": "Danh mục mã dùng chung: ICD-10, đơn vị thuốc, lý do khám, danh mục hệ thống.",
  "/admin/settings": "Cấu hình chung: ngưỡng duyệt, gói dịch vụ, bảo mật (2FA bắt buộc theo vai trò)...",
};

export function AdminHubClient() {
  const t = useTranslations("Nav");
  const { hasAny } = usePermissions();

  // Lay dung nhom "He thong" trong nav, bo link /admin tro chinh no.
  const systemGroup = NAV_GROUPS.find((g) => g.labelVi === "Hệ thống");
  const items = (systemGroup?.items ?? []).filter(
    (item) =>
      item.href !== "/admin" &&
      (!item.permissions || item.permissions.length === 0 || hasAny(item.permissions))
  );

  if (items.length === 0) {
    return (
      <div className="flex h-48 flex-col items-center justify-center gap-2 rounded-md border text-muted-foreground">
        <p className="text-sm">Bạn chưa được cấp quyền truy cập khu vực quản trị nào.</p>
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-3">
      {items.map((item) => {
        const Icon = item.icon;
        const label = t(item.labelKey as Parameters<typeof t>[0]);
        const desc = AREA_DESCRIPTIONS[item.href];
        return (
          <Link
            key={item.href}
            href={item.href}
            className={cn(
              "group flex flex-col gap-2 rounded-xl border bg-card p-4 transition-colors",
              "hover:border-primary/50 hover:bg-accent/40"
            )}
          >
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-primary/10 text-primary">
                <Icon className="h-5 w-5" />
              </div>
              <p className="font-medium leading-tight">{label}</p>
              <ChevronRight className="ml-auto h-4 w-4 shrink-0 text-muted-foreground transition-transform group-hover:translate-x-0.5" />
            </div>
            {desc && (
              <p className="text-sm text-muted-foreground leading-snug">{desc}</p>
            )}
          </Link>
        );
      })}
    </div>
  );
}
