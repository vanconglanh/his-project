// Xác định subdomain phòng khám (chỉ dùng cho dev/local — production backend tự resolve theo Host)

/** Header dev để backend resolve tenant khi chạy trên localhost thường (không phải *.localhost) */
export const DEV_SUBDOMAIN_HEADER = "X-Portal-Subdomain";

export function getDevSubdomain(): string | undefined {
  return process.env.NEXT_PUBLIC_DEV_SUBDOMAIN;
}

/** Trả về headers bổ sung cần gắn khi gọi API ở môi trường dev (localhost không phải *.localhost) */
export function getDevTenantHeaders(): Record<string, string> {
  if (typeof window === "undefined") return {};
  const host = window.location.hostname;
  const isDevPlainLocalhost = host === "localhost" || host === "127.0.0.1";
  const devSubdomain = getDevSubdomain();
  if (isDevPlainLocalhost && devSubdomain) {
    return { [DEV_SUBDOMAIN_HEADER]: devSubdomain };
  }
  return {};
}
