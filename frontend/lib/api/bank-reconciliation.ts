import apiClient from "./client";

// Contract: POST/GET /api/v1/bil/bank-statements/*
// Ghi chu: base cua apiClient da la `${API_BASE_URL}/api/v1`, nen cac ham o day
// dung tiep tiep dau `/bil/bank-statements` cho tron URL cuoi cung.

export type BankStatementLineMatchStatus =
  | "MATCHED"
  | "UNMATCHED"
  | "MANUAL_MATCHED"
  | "IGNORED";

export interface BankStatementImportResult {
  id: string;
  file_name: string;
  bank_code: string | null;
  statement_date: string | null;
  total_lines: number;
  matched_lines: number;
  unmatched_lines: number;
}

export interface BankStatementListItem {
  id: string;
  file_name: string;
  bank_code: string | null;
  statement_date: string | null;
  total_lines: number;
  matched_lines: number;
  unmatched_lines: number;
  uploaded_at: string;
  uploaded_by_name: string | null;
}

export interface BankStatementListMeta {
  page: number;
  page_size: number;
  total: number;
}

export interface BankStatementListResponse {
  data: BankStatementListItem[];
  meta: BankStatementListMeta;
}

export interface ListBankStatementsParams {
  page?: number;
  page_size?: number;
  from_date?: string;
  to_date?: string;
}

export interface MatchedPaymentSummary {
  id: string;
  reference: string;
  method: string;
  amount: number;
  paid_at: string;
  billing_id: string;
}

export interface BankStatementLine {
  id: string;
  transaction_date: string;
  amount: number;
  reference_no: string | null;
  description: string | null;
  match_status: BankStatementLineMatchStatus;
  matched_payment_id: string | null;
  matched_payment: MatchedPaymentSummary | null;
}

export interface BankStatementDetail {
  id: string;
  file_name: string;
  bank_code: string | null;
  statement_date: string | null;
  total_lines: number;
  matched_lines: number;
  unmatched_lines: number;
  uploaded_at: string;
}

export interface BankStatementLinesResponse {
  data: {
    statement: BankStatementDetail;
    lines: BankStatementLine[];
  };
}

export interface PaymentCandidate {
  id: string;
  reference: string;
  method: string;
  amount: number;
  paid_at: string;
  billing_id: string;
}

export async function importBankStatement(params: {
  file: File;
  bank_code?: string;
  statement_date?: string;
}): Promise<BankStatementImportResult> {
  const formData = new FormData();
  formData.append("file", params.file);
  if (params.bank_code) formData.append("bank_code", params.bank_code);
  if (params.statement_date) formData.append("statement_date", params.statement_date);

  const { data } = await apiClient.post<{ data: BankStatementImportResult }>(
    "/bil/bank-statements/import",
    formData,
    { headers: { "Content-Type": "multipart/form-data" } }
  );
  return data.data;
}

export async function listBankStatements(
  params: ListBankStatementsParams
): Promise<BankStatementListResponse> {
  const { data } = await apiClient.get<BankStatementListResponse>("/bil/bank-statements", {
    params,
  });
  return data;
}

export async function getBankStatementLines(
  statementId: string
): Promise<BankStatementLinesResponse["data"]> {
  const { data } = await apiClient.get<BankStatementLinesResponse>(
    `/bil/bank-statements/${statementId}/lines`
  );
  return data.data;
}

export async function getBankStatementLineCandidates(
  lineId: string
): Promise<PaymentCandidate[]> {
  const { data } = await apiClient.get<{ data: PaymentCandidate[] }>(
    `/bil/bank-statements/lines/${lineId}/candidates`
  );
  return data.data;
}

export async function manualMatchBankStatementLine(
  lineId: string,
  paymentId: string
): Promise<BankStatementLine> {
  const { data } = await apiClient.post<{ data: BankStatementLine }>(
    `/bil/bank-statements/lines/${lineId}/manual-match`,
    { payment_id: paymentId }
  );
  return data.data;
}

export async function ignoreBankStatementLine(lineId: string): Promise<BankStatementLine> {
  const { data } = await apiClient.post<{ data: BankStatementLine }>(
    `/bil/bank-statements/lines/${lineId}/ignore`
  );
  return data.data;
}

export async function unmatchBankStatementLine(lineId: string): Promise<BankStatementLine> {
  const { data } = await apiClient.post<{ data: BankStatementLine }>(
    `/bil/bank-statements/lines/${lineId}/unmatch`
  );
  return data.data;
}
