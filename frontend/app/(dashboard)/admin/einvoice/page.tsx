import type { Metadata } from "next";
import { EInvoiceAdminClient } from "./_components/EInvoiceAdminClient";

export const metadata: Metadata = { title: "Quản lý HĐĐT" };

export default function EInvoiceAdminPage() {
  return <EInvoiceAdminClient />;
}
