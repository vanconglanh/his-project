import type { Metadata, Viewport } from "next";
import { Providers } from "@/components/Providers";
import { AppShell } from "@/components/AppShell";
import "./globals.css";

export const metadata: Metadata = {
  title: {
    default: "Cổng bệnh nhân",
    template: "%s | Cổng bệnh nhân",
  },
  description: "Cổng thông tin bệnh nhân — tra cứu hồ sơ, đặt lịch, kết quả khám",
  manifest: "/manifest.webmanifest",
  appleWebApp: {
    capable: true,
    statusBarStyle: "default",
    title: "Cổng bệnh nhân",
  },
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 3,
  themeColor: "#01645A",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="vi">
      <body className="antialiased">
        <Providers>
          <AppShell>{children}</AppShell>
        </Providers>
      </body>
    </html>
  );
}
