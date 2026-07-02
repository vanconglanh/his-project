"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { LogOut, Activity, User } from "lucide-react";
import { Button } from "@/components/ui/button";
import { portalLogout } from "@/lib/api/portal";
import { toast } from "sonner";
import { cn } from "@/lib/utils";

const NAV_ITEMS = [
  { href: "/portal/me", label: "Hồ sơ" },
  { href: "/portal/encounters", label: "Lịch sử khám" },
  { href: "/portal/prescriptions", label: "Đơn thuốc" },
  { href: "/portal/lab-results", label: "Kết quả XN" },
  { href: "/portal/appointments", label: "Lịch hẹn" },
];

interface PortalLayoutProps {
  children: React.ReactNode;
}

export function PortalLayout({ children }: PortalLayoutProps) {
  const pathname = usePathname();
  const router = useRouter();

  async function handleLogout() {
    try {
      await portalLogout();
    } catch {
      // ignore
    }
    if (typeof window !== "undefined") {
      localStorage.removeItem("portal-token");
      localStorage.removeItem("portal-user");
    }
    toast.info("Đã đăng xuất");
    router.push("/portal/login");
  }

  return (
    <div className="min-h-screen bg-background flex flex-col">
      {/* Header */}
      <header className="border-b bg-card sticky top-0 z-30">
        <div className="container mx-auto flex items-center justify-between h-16 px-4">
          <Link href="/portal/me" className="flex items-center gap-2">
            <Activity className="h-6 w-6 text-primary" />
            <span className="font-bold text-sm">Cổng bệnh nhân</span>
            <span className="text-muted-foreground text-xs hidden sm:block">— Pro-Diab HIS</span>
          </Link>

          <div className="flex items-center gap-2">
            <Button variant="ghost" size="icon" aria-label="Tài khoản">
              <User className="h-4 w-4" />
            </Button>
            <Button
              variant="ghost"
              size="icon"
              onClick={handleLogout}
              aria-label="Đăng xuất"
              title="Đăng xuất"
            >
              <LogOut className="h-4 w-4" />
            </Button>
          </div>
        </div>

        {/* Sub navigation */}
        <nav className="container mx-auto px-4 pb-0">
          <ul className="flex gap-1 overflow-x-auto">
            {NAV_ITEMS.map((item) => (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className={cn(
                    "inline-flex items-center px-3 py-2 text-sm font-medium border-b-2 -mb-px transition-colors whitespace-nowrap",
                    pathname === item.href || pathname.startsWith(item.href + "/")
                      ? "border-primary text-primary"
                      : "border-transparent text-muted-foreground hover:text-foreground"
                  )}
                >
                  {item.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      </header>

      {/* Content */}
      <main className="flex-1 container mx-auto px-4 py-6 max-w-4xl">{children}</main>

      {/* Footer */}
      <footer className="border-t py-4 text-center text-xs text-muted-foreground">
        Pro-Diab HIS — Hệ thống quản lý phòng khám
      </footer>
    </div>
  );
}
