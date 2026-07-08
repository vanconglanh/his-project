import { NextResponse } from "next/server";
import type { NextRequest } from "next/server";

// NOTE: Next.js 16 đổi tên "middleware.ts" -> "proxy.ts" (xem
// node_modules/next/dist/docs/01-app/02-guides/upgrading/version-16.md).
// File này đóng vai trò guard: chỉ kiểm tra có cookie "portal-token" hay
// không, KHÔNG parse tenant (backend tự resolve theo Host header).

const PUBLIC_PATHS = ["/login", "/activate", "/reset-pin"];

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;
  const hasToken = request.cookies.has("portal-token");
  const isPublicPath = PUBLIC_PATHS.some((path) => pathname === path || pathname.startsWith(`${path}/`));

  if (!hasToken && !isPublicPath) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  if (hasToken && isPublicPath) {
    return NextResponse.redirect(new URL("/", request.url));
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Bỏ qua các route không cần guard: static asset, icon, manifest,
     * service worker, favicon.
     */
    "/((?!_next/static|_next/image|icons|sw\\.js|manifest|favicon\\.ico).*)",
  ],
};
