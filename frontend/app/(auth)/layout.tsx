import type { Metadata } from "next";
import { Building2 } from "lucide-react";

export const metadata: Metadata = {
  title: "Đăng nhập",
};

export default function AuthLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-muted/40 p-4">
      <div className="w-full max-w-sm space-y-6">
        {/* Logo */}
        <div className="flex flex-col items-center gap-2 text-center">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary text-primary-foreground">
            <Building2 className="h-6 w-6" />
          </div>
          <div>
            <h1 className="text-xl font-bold">Pro-Diab HIS</h1>
            <p className="text-sm text-muted-foreground">
              Hệ thống quản lý phòng khám đa khoa
            </p>
          </div>
        </div>

        {/* Card */}
        <div className="rounded-xl border bg-card p-6 shadow-sm">{children}</div>

        <p className="text-center text-xs text-muted-foreground">
          &copy; {new Date().getFullYear()} Pro-Diab HIS. All rights reserved.
        </p>
      </div>
    </div>
  );
}
