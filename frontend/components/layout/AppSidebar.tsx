"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useTranslations } from "next-intl";
import { useEffect } from "react";
import { ChevronLeft, ChevronRight, Building2 } from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/lib/stores/ui-store";
import { usePermissions } from "@/lib/hooks/use-permissions";
import { NAV_GROUPS } from "@/lib/config/nav-items";
import {
  Tooltip,
  TooltipContent,
  TooltipTrigger,
} from "@/components/ui/tooltip";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { APP_NAME } from "@/lib/utils/constants";

/** Auto-collapse sidebar on tablet widths (<1024px) */
function useTabletCollapse() {
  const { setSidebarCollapsed } = useUiStore();

  useEffect(() => {
    function check() {
      if (window.innerWidth < 1024) {
        setSidebarCollapsed(true);
      }
    }
    check();
    window.addEventListener("resize", check);
    return () => window.removeEventListener("resize", check);
  }, [setSidebarCollapsed]);
}

export function AppSidebar() {
  const t = useTranslations("Nav");
  const pathname = usePathname();
  const { sidebarCollapsed, toggleSidebar } = useUiStore();
  const { has, hasAny } = usePermissions();
  useTabletCollapse();

  function isActive(href: string) {
    if (href === "/") return pathname === "/";
    return pathname.startsWith(href);
  }

  function isItemVisible(permissions?: string[]): boolean {
    if (!permissions || permissions.length === 0) return true;
    return hasAny(permissions);
  }

  return (
    <aside
      className={cn(
        "flex flex-col border-r bg-card transition-all duration-300 shrink-0",
        sidebarCollapsed ? "w-14" : "w-60"
      )}
    >
      {/* Logo */}
      <div className="flex h-16 items-center border-b px-3 shrink-0">
        <Building2 className="h-6 w-6 text-primary shrink-0" />
        {!sidebarCollapsed && (
          <span className="ml-2 font-bold text-sm leading-tight truncate">
            {APP_NAME}
          </span>
        )}
      </div>

      {/* Nav */}
      <nav
        className="flex-1 overflow-y-auto overflow-x-hidden py-4 space-y-4"
        aria-label="Điều hướng chính"
      >
        {NAV_GROUPS.map((group) => {
          const visibleItems = group.items.filter((item) =>
            isItemVisible(item.permissions)
          );
          if (visibleItems.length === 0) return null;

          return (
            <div key={group.labelVi}>
              {!sidebarCollapsed && (
                <p className="px-3 mb-1 text-xs font-medium text-muted-foreground uppercase tracking-wider">
                  {group.labelVi}
                </p>
              )}
              <ul className="space-y-0.5">
                {visibleItems.map((item) => {
                  const Icon = item.icon;
                  const active = isActive(item.href);
                  const label = t(item.labelKey as Parameters<typeof t>[0]);

                  return (
                    <li key={`${item.href}-${item.labelKey}`}>
                      {sidebarCollapsed ? (
                        <Tooltip>
                          <TooltipTrigger
                            className={cn(
                              "flex h-11 w-full items-center justify-center rounded-md mx-1 transition-colors",
                              active
                                ? "bg-primary text-primary-foreground"
                                : "text-muted-foreground hover:bg-accent hover:text-foreground"
                            )}
                            aria-label={label}
                            render={
                              <Link href={item.href} aria-label={label} />
                            }
                          >
                            <Icon className="h-5 w-5" aria-hidden="true" />
                          </TooltipTrigger>
                          <TooltipContent side="right">{label}</TooltipContent>
                        </Tooltip>
                      ) : (
                        <Link
                          href={item.href}
                          className={cn(
                            "flex h-11 items-center gap-3 rounded-md px-3 mx-1 text-sm font-medium transition-colors",
                            active
                              ? "bg-primary text-primary-foreground"
                              : "text-muted-foreground hover:bg-accent hover:text-foreground"
                          )}
                          aria-current={active ? "page" : undefined}
                        >
                          <Icon className="h-5 w-5 shrink-0" aria-hidden="true" />
                          <span className="truncate">{label}</span>
                        </Link>
                      )}
                    </li>
                  );
                })}
              </ul>
              <Separator className="mt-4" />
            </div>
          );
        })}
      </nav>

      {/* Collapse toggle */}
      <div className="border-t p-2">
        <Button
          variant="ghost"
          size="sm"
          onClick={toggleSidebar}
          className="w-full justify-center h-9"
          aria-label={sidebarCollapsed ? "Mở sidebar" : "Thu sidebar"}
        >
          {sidebarCollapsed ? (
            <ChevronRight className="h-4 w-4" aria-hidden="true" />
          ) : (
            <ChevronLeft className="h-4 w-4" aria-hidden="true" />
          )}
        </Button>
      </div>
    </aside>
  );
}
