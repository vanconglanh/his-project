"use client";

import { useMe } from "@/lib/hooks/use-users";

interface CanProps {
  permission?: string;
  role?: string;
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

/**
 * Conditionally render children based on permission or role.
 * Usage:
 *   <Can permission="user.invite"><Button>Mời</Button></Can>
 *   <Can role="admin"><Button>Admin action</Button></Can>
 */
export function Can({ permission, role, children, fallback = null }: CanProps) {
  const { data: me, isLoading } = useMe();

  if (isLoading) return null;

  if (permission && !me?.permissions?.includes(permission)) {
    return <>{fallback}</>;
  }

  if (role && !me?.roles?.some((r) => r.code === role)) {
    return <>{fallback}</>;
  }

  return <>{children}</>;
}
