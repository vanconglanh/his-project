"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { useEffect } from "react";
import { Button } from "@/components/ui/button";

export default function NotFound() {
  const pathname = usePathname();

  useEffect(() => {
    // Log path 404 vào console để dev tracking
    console.warn(`[404] Route không tồn tại: ${pathname}`);

    // Lưu localStorage list 100 path 404 gần nhất
    try {
      const key = "prodiab-404-log";
      const raw = localStorage.getItem(key);
      const list: { path: string; at: string }[] = raw ? JSON.parse(raw) : [];
      list.unshift({ path: pathname, at: new Date().toISOString() });
      localStorage.setItem(key, JSON.stringify(list.slice(0, 100)));
    } catch {}
  }, [pathname]);

  return (
    <div className="flex min-h-[60vh] flex-col items-center justify-center gap-6 p-8 text-center">
      <div className="text-7xl font-bold text-muted-foreground">404</div>
      <div>
        <h1 className="text-2xl font-bold tracking-tight">Không tìm thấy trang</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Đường dẫn <code className="rounded bg-muted px-2 py-0.5 font-mono text-xs">{pathname}</code> không tồn tại trong hệ thống.
        </p>
      </div>
      <div className="flex gap-2">
        <Link
          href="/"
          className="inline-flex items-center justify-center rounded-md border border-input bg-background px-4 py-2 text-sm font-medium hover:bg-accent"
        >
          Về trang chủ
        </Link>
        <Button
          variant="ghost"
          onClick={() => {
            const log = localStorage.getItem("prodiab-404-log");
            console.log("Lịch sử 404:", log ? JSON.parse(log) : []);
            alert("Đã in lịch sử 404 vào DevTools Console (F12)");
          }}
        >
          Xem lịch sử 404
        </Button>
      </div>
      <p className="text-xs text-muted-foreground">
        Nếu nghĩ đây là lỗi, báo team dev với đường dẫn ở trên.
      </p>
    </div>
  );
}
