import type { Metadata } from "next";
import ClsOrderPrintClient from "./_components/ClsOrderPrintClient";

export const metadata: Metadata = { title: "In phiếu chỉ định CLS — Pro-Diab HIS" };

interface Props {
  params: Promise<{ id: string }>;
}

export default async function ClsPrintPage({ params }: Props) {
  const { id } = await params;
  return <ClsOrderPrintClient encounterId={id} />;
}
