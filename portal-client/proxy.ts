import { NextResponse, type NextRequest } from "next/server";

const TOKEN_COOKIE = "portal-token";

/** Các route công khai, không cần đăng nhập */
const PUBLIC_PATHS = ["/login", "/activate"];

function isPublicPath(pathname: string): boolean {
  return PUBLIC_PATHS.some((p) => pathname === p || pathname.startsWith(`${p}/`));
}

export function proxy(request: NextRequest) {
  const { pathname } = request.nextUrl;

  if (isPublicPath(pathname)) {
    return NextResponse.next();
  }

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
     * Áp dụng proxy cho mọi route trừ:
     * - api routes nội bộ của Next
     * - static file (_next/static, _next/image)
     * - favicon, manifest, sw.js, icons
     */
    "/((?!_next/static|_next/image|favicon.ico|manifest.webmanifest|sw.js|icons).*)",
  ],
};
