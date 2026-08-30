import apiClient from "./client";

// Casing xac nhan tu backend/src/ProDiabHis.Application/Billing/InterBranchDebts/InterBranchDebtDtos.cs
// + InterBranchDebtsController.cs (meta: page/page_size/total).

export interface InterBranchDebt {
  id: string;
  tenant_id: number;
  debtor_branch_id: number;
  debtor_branch_name: string | null;
  creditor_branch_id: number;
  creditor_branch_name: string | null;
  amount: number;
  source_type: "CROSS_BRANCH_PAYMENT" | "STOCK_TRANSFER";
  source_ref_id: string | null;
  source_ref_code: string | null;
  status: "OPEN" | "SETTLED";
  note: string | null;
  settled_at: string | null;
  created_at: string;
}

export interface InterBranchDebtListMeta {
  page: number;
  page_size: number;
  total: number;
}

export interface InterBranchDebtListResponse {
  data: InterBranchDebt[];
  meta: InterBranchDebtListMeta;
}

export interface ListInterBranchDebtsParams {
  debtor_branch_id?: number;
  creditor_branch_id?: number;
  status?: string;
  page?: number;
  page_size?: number;
}

export async function listInterBranchDebts(
  params: ListInterBranchDebtsParams
): Promise<InterBranchDebtListResponse> {
  const { data } = await apiClient.get<InterBranchDebtListResponse>("/inter-branch-debts", {
    params,
  });
  return data;
}

export async function settleInterBranchDebt(id: string, note?: string): Promise<InterBranchDebt> {
  const { data } = await apiClient.post<{ data: InterBranchDebt }>(
    `/inter-branch-debts/${id}/settle`,
    { note: note ?? null }
  );
  return data.data;
}
