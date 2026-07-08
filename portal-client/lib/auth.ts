// Quản lý token phía client qua cookie "portal-token"
// Dùng cookie (không phải localStorage) để middleware/proxy có thể đọc được khi điều hướng.

const TOKEN_COOKIE = "portal-token";
const MAX_AGE_SECONDS = 60 * 60 * 24 * 30; // 30 ngày

export function saveToken(token: string) {
  if (typeof document === "undefined") return;
  document.cookie = `${TOKEN_COOKIE}=${encodeURIComponent(
    token
  )}; path=/; max-age=${MAX_AGE_SECONDS}; SameSite=Lax`;
}

export function getToken(): string | null {
  if (typeof document === "undefined") return null;
  const match = document.cookie
    .split("; ")
    .find((row) => row.startsWith(`${TOKEN_COOKIE}=`));
  if (!match) return null;
  return decodeURIComponent(match.split("=").slice(1).join("="));
}

export function clearToken() {
  if (typeof document === "undefined") return;
  document.cookie = `${TOKEN_COOKIE}=; path=/; max-age=0; SameSite=Lax`;
}

export const PORTAL_TOKEN_COOKIE = TOKEN_COOKIE;
