import { cookies } from "next/headers";
import { NextRequest, NextResponse } from "next/server";

export async function POST(request: NextRequest) {
  try {
    const body = await request.json();
    const { accessToken, expiresIn } = body as {
      accessToken?: string;
      expiresIn?: number;
    };

    if (!accessToken || typeof accessToken !== "string") {
      return NextResponse.json(
        { error: { code: "INVALID_TOKEN", message: "Token không hợp lệ." } },
        { status: 400 }
      );
    }

    const cookieStore = await cookies();
    cookieStore.set("his-access-token", accessToken, {
      httpOnly: true,
      secure: process.env.NODE_ENV === "production",
      sameSite: "lax",
      path: "/",
      maxAge: expiresIn ?? 86400,
    });

    return NextResponse.json({ ok: true });
  } catch {
    return NextResponse.json(
      { error: { code: "SERVER_ERROR", message: "Lỗi máy chủ nội bộ." } },
      { status: 500 }
    );
  }
}
