"use client";

import { useAuthStore } from "@/lib/stores/auth-store";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";

export default function ProfilePage() {
  const user = useAuthStore((s) => s.user);
  const roles = useAuthStore((s) => s.roles);

  if (!user) {
    return (
      <div className="p-6 text-center text-muted-foreground">
        Vui lòng đăng nhập để xem hồ sơ.
      </div>
    );
  }

  const initials = user.fullName
    .split(" ")
    .map((n) => n[0])
    .slice(-2)
    .join("")
    .toUpperCase();

  return (
    <div className="max-w-3xl mx-auto p-6 space-y-6">
      <h1 className="text-2xl font-semibold">Hồ sơ cá nhân</h1>
      <Card>
        <CardHeader>
          <CardTitle>Thông tin tài khoản</CardTitle>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-center gap-4">
            <Avatar className="h-16 w-16">
              <AvatarImage src={user.avatarUrl} alt={user.fullName} />
              <AvatarFallback>{initials}</AvatarFallback>
            </Avatar>
            <div>
              <div className="font-medium text-lg">{user.fullName}</div>
              <div className="text-sm text-muted-foreground">{user.email}</div>
            </div>
          </div>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-3 text-sm">
            <div>
              <dt className="text-muted-foreground">Mã nhân viên</dt>
              <dd className="font-mono">{user.id}</dd>
            </div>
            <div>
              <dt className="text-muted-foreground">Phòng khám</dt>
              <dd>{user.clinicName ?? "—"}</dd>
            </div>
            <div className="sm:col-span-2">
              <dt className="text-muted-foreground">Vai trò</dt>
              <dd className="flex flex-wrap gap-1.5 pt-1">
                {roles.length === 0 ? (
                  <span className="text-muted-foreground">Chưa gán vai trò</span>
                ) : (
                  roles.map((r) => (
                    <span
                      key={r}
                      className="inline-flex items-center rounded-md bg-teal-50 dark:bg-teal-900/30 px-2 py-0.5 text-xs font-medium text-teal-700 dark:text-teal-300"
                    >
                      {r}
                    </span>
                  ))
                )}
              </dd>
            </div>
          </dl>
        </CardContent>
      </Card>
    </div>
  );
}
