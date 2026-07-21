// Quản lý token phiên đăng nhập của bệnh nhân, lưu ở cookie phía client (SameSite=Lax)

export const TOKEN_COOKIE = "portal-token";

/** Số ngày cookie tồn tại (nên khớp expiresIn của accessToken khi có refresh, tạm 30 ngày) */
const COOKIE_MAX_AGE_DAYS = 30;

export function setTokenCookie(token: string) {
  if (typeof document === "undefined") return;
  const maxAge = COOKIE_MAX_AGE_DAYS * 24 * 60 * 60;
  const secure = process.env.NODE_ENV === "production" ? "; Secure" : "";
  document.cookie = `${TOKEN_COOKIE}=${encodeURIComponent(token)}; Path=/; Max-Age=${maxAge}; SameSite=Lax${secure}`;
}

export function clearTokenCookie() {
  if (typeof document === "undefined") return;
  document.cookie = `${TOKEN_COOKIE}=; Path=/; Max-Age=0; SameSite=Lax`;
}

export function getTokenFromClientCookie(): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie
    .split("; ")
    .find((row) => row.startsWith(`${TOKEN_COOKIE}=`));
  if (!match) return null;
  return decodeURIComponent(match.split("=").slice(1).join("="));
}
