"use client";

import { useTranslations } from "next-intl";
import { useRouter } from "next/navigation";
import { LogOut, User, Bell, ShieldCheck } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { useAuthStore } from "@/lib/stores/auth-store";
import { useAuth } from "@/lib/hooks/use-auth";
import { ROLE_LABELS } from "@/lib/utils/constants";

export function UserMenu() {
  const t = useTranslations("Common");
  const router = useRouter();
  const { user } = useAuthStore();
  const { logout } = useAuth();

  if (!user) return null;

  const initials = user.fullName
    .split(" ")
    .map((n) => n[0])
    .slice(-2)
    .join("")
    .toUpperCase();

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        aria-label="Menu tài khoản"
        className="inline-flex h-10 items-center gap-2 rounded-md px-2 text-sm font-medium transition-colors hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
      >
        <Avatar className="h-8 w-8">
          <AvatarImage src={user.avatarUrl} alt={user.fullName} />
          <AvatarFallback className="text-xs">{initials}</AvatarFallback>
        </Avatar>
        <div className="hidden sm:flex flex-col items-start leading-tight">
          <span className="text-sm font-medium truncate max-w-32">
            {user.fullName}
          </span>
          <span className="text-xs text-muted-foreground">
            {ROLE_LABELS[user.role] ?? user.role}
          </span>
        </div>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-56">
        <DropdownMenuGroup>
          <DropdownMenuLabel className="flex flex-col gap-0.5">
            <span>{user.fullName}</span>
            <span className="text-xs font-normal text-muted-foreground">
              {user.email}
            </span>
          </DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem onClick={() => router.push("/account/profile")}>
            <User className="mr-2 h-4 w-4" />
            Hồ sơ cá nhân
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => router.push("/account/security")}>
            <ShieldCheck className="mr-2 h-4 w-4" />
            Bảo mật
          </DropdownMenuItem>
          <DropdownMenuItem onClick={() => router.push("/account/notifications")}>
            <Bell className="mr-2 h-4 w-4" />
            Thông báo
          </DropdownMenuItem>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem
            onClick={logout}
            className="text-destructive focus:text-destructive"
          >
            <LogOut className="mr-2 h-4 w-4" />
            {t("logout")}
          </DropdownMenuItem>
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
