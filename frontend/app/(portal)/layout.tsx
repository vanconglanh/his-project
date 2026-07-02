import type { Metadata } from "next";

export const metadata: Metadata = {
  title: "Cổng bệnh nhân — Pro-Diab HIS",
  description: "Xem lịch sử khám bệnh, đơn thuốc và kết quả xét nghiệm",
};

export default function PortalRootLayout({ children }: { children: React.ReactNode }) {
  return children;
}
