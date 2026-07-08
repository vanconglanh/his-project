import type { Metadata, Viewport } from "next";
import "./globals.css";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "Cổng bệnh nhân",
  description: "Cổng thông tin bệnh nhân — Pro-Diab HIS",
  appleWebApp: {
    capable: true,
    statusBarStyle: "default",
    title: "Cổng bệnh nhân",
  },
};

export const viewport: Viewport = {
  width: "device-width",
  initialScale: 1,
  maximumScale: 1,
  themeColor: "#0d6efd",
};

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="vi">
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
