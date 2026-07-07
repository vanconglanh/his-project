import type { Metadata } from "next";

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
        <div className="flex flex-col items-center gap-3 text-center">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src="/brand/diab-logo.svg" alt="diaB" className="h-16 w-auto" />
          <p className="text-sm text-muted-foreground">
            Hệ thống quản lý phòng khám đa khoa
          </p>
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
