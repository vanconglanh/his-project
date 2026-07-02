"use client";

import { useQuery } from "@tanstack/react-query";
import * as icd10Api from "@/lib/api/icd10";

export const icd10Keys = {
  search: (params: object) => ["icd10", "search", params] as const,
  categories: () => ["icd10", "categories"] as const,
};

export function useIcd10Search(params: {
  q: string;
  type?: "code" | "name" | "all";
  category?: string;
  billable_only?: boolean;
  limit?: number;
}) {
  return useQuery({
    queryKey: icd10Keys.search(params),
    queryFn: () => icd10Api.searchIcd10(params),
    enabled: params.q.trim().length >= 1,
    staleTime: 5 * 60_000,
    retry: 1,
  });
}

export function useIcd10Categories() {
  return useQuery({
    queryKey: icd10Keys.categories(),
    queryFn: icd10Api.listIcd10Categories,
    staleTime: 60 * 60_000,
  });
}
