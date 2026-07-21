import { NextRequest, NextResponse } from "next/server";

/**
 * Frontend dashboard (HIS nội bộ) — auth guard server-side.
 *
 * Auth state được lưu trong Zustand persist (localStorage key "auth-store").
 * Middleware chạy ở Edge nên không đọc được localStorage. Thay vào đó,
 * Next.js route handler /api/auth/refresh set accessToken dưới dạng cookie
 * "his-access-token" (httpOnly) khi refresh thành công — hoặc client tự set
 * cookie đó sau khi login.
 *
 * Nếu cookie vắng → redirect /login để tránh render dữ liệu bệnh nhân cho
 * unauthenticated user (BUG-002).
 */
const AUTH_COOKIE = "his-access-token";

const PUBLIC_PATHS = ["/login", "/session"];

const BYPASS_PREFIXES = ["/_next/", "/favicon", "/icons/", "/manifest", "/session/"];

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Bỏ qua static assets và Next.js internals
  if (BYPASS_PREFIXES.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  // Bỏ qua các path công khai
  if (PUBLIC_PATHS.some((p) => pathname === p || pathname.startsWith(p + "/"))) {
    return NextResponse.next();
  }

  // Chỉ check cookie tồn tại; verify signature/expiry ở API layer (backend trả 401 → 401 handler redirect). (NEW-002)
  const token = request.cookies.get(AUTH_COOKIE)?.value;
  if (!token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico).*)"],
};
