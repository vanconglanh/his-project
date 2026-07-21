import { NextRequest, NextResponse } from "next/server";

/** Cookie lưu portal token (khớp với TOKEN_COOKIE trong lib/auth.ts) */
const TOKEN_COOKIE = "portal-token";

/** Các path công khai — không cần auth */
const PUBLIC_PATHS = ["/login", "/activate"];

/** Prefix luôn bỏ qua (Next.js internals + static assets) */
const BYPASS_PREFIXES = ["/_next/", "/favicon", "/icons/", "/manifest"];

export function middleware(request: NextRequest) {
  const { pathname } = request.nextUrl;

  // Bỏ qua static assets và Next.js internals
  if (BYPASS_PREFIXES.some((p) => pathname.startsWith(p))) {
    return NextResponse.next();
  }

  // Bỏ qua các path công khai
  if (PUBLIC_PATHS.some((p) => pathname === p || pathname.startsWith(p + "/"))) {
    return NextResponse.next();
  }

  // Kiểm tra cookie token
  const token = request.cookies.get(TOKEN_COOKIE)?.value;
  if (!token) {
    const loginUrl = new URL("/login", request.url);
    loginUrl.searchParams.set("redirect", pathname);
    return NextResponse.redirect(loginUrl);
  }

  return NextResponse.next();
}

export const config = {
  matcher: [
    /*
     * Match tất cả path trừ:
     * - _next/static (static files)
     * - _next/image (image optimization)
     * - favicon.ico
     */
    "/((?!_next/static|_next/image|favicon.ico).*)",
  ],
};
