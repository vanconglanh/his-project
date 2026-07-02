import type { Metadata } from "next";
import { ServicesPageClient } from "./_components/ServicesPageClient";

export const metadata: Metadata = { title: "Danh mục dịch vụ" };

export default function ServicesPage() {
  return <ServicesPageClient />;
}
