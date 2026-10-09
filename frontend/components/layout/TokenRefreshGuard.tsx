"use client";

import { useTokenRefresh } from "@/lib/hooks/use-token-refresh";

/**
 * BM-86: Mount proactive token refresh trong dashboard layout.
 * Component này không render gì, chỉ kích hoạt hook useTokenRefresh.
 */
export function TokenRefreshGuard() {
  useTokenRefresh();
  return null;
}
