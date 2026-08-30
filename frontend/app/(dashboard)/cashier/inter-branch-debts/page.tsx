import type { Metadata } from "next";
import { InterBranchDebtsClient } from "./_components/InterBranchDebtsClient";

export const metadata: Metadata = { title: "Công nợ nội bộ giữa các chi nhánh" };

export default function InterBranchDebtsPage() {
  return <InterBranchDebtsClient />;
}
