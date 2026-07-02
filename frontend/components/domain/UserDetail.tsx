import { Avatar, AvatarFallback, AvatarImage } from "@/components/ui/avatar";
import { StatusBadge } from "@/components/ui/entity-status-badge";
import { RoleBadge } from "@/components/ui/RoleBadge";
import { Badge } from "@/components/ui/badge";
import type { UserResponse } from "@/lib/api/types";
import { formatDateTime } from "@/lib/utils/format";

interface UserDetailProps {
  user: UserResponse;
}

function DetailRow({ label, value }: { label: string; value?: React.ReactNode }) {
  return (
    <div className="grid grid-cols-[140px_1fr] gap-2 py-2 border-b last:border-0">
      <span className="text-sm text-muted-foreground">{label}</span>
      <span className="text-sm font-medium">{value ?? "—"}</span>
    </div>
  );
}

export function UserDetail({ user }: UserDetailProps) {
  return (
    <div className="mt-4 space-y-4">
      <div className="flex items-center gap-4">
        <Avatar className="h-16 w-16">
          <AvatarImage src={user.avatar_url ?? undefined} />
          <AvatarFallback className="text-xl">
            {user.full_name.charAt(0).toUpperCase()}
          </AvatarFallback>
        </Avatar>
        <div>
          <p className="text-lg font-semibold">{user.full_name}</p>
          <p className="text-sm text-muted-foreground">{user.email}</p>
        </div>
      </div>

      <div>
        <DetailRow label="Trạng thái" value={<StatusBadge status={user.status} />} />
        <DetailRow label="Điện thoại" value={user.phone} />
        <DetailRow
          label="Vai trò"
          value={
            <div className="flex flex-wrap gap-1">
              {user.roles.map((r) => <RoleBadge key={r.code} code={r.code} name={r.name} />)}
            </div>
          }
        />
        <DetailRow
          label="2FA"
          value={
            <Badge variant={user.two_fa_enabled ? "default" : "secondary"}>
              {user.two_fa_enabled ? "Đã bật" : "Chưa bật"}
            </Badge>
          }
        />
        <DetailRow
          label="Đăng nhập cuối"
          value={user.last_login_at ? formatDateTime(user.last_login_at) : "Chưa đăng nhập"}
        />
        <DetailRow label="Ngày tạo" value={formatDateTime(user.created_at)} />
      </div>
    </div>
  );
}
