"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import * as codesApi from "@/lib/api/codes";
import type { CodeItem } from "@/lib/api/codes";

// Danh muc it thay doi -> cache dai (30 phut).
const CODES_STALE_TIME = 30 * 60_000;

export const codeKeys = {
  groups: () => ["codes", "groups"] as const,
  items: (groupId: string) => ["codes", "items", groupId] as const,
};

/** Danh sach nhom ma (code_master). */
export function useCodeGroups() {
  return useQuery({
    queryKey: codeKeys.groups(),
    queryFn: codesApi.getCodeGroups,
    staleTime: CODES_STALE_TIME,
  });
}

/** Danh sach ma trong 1 nhom: tra ve list { code, name }. */
export function useCodes(groupId: string) {
  return useQuery<CodeItem[]>({
    queryKey: codeKeys.items(groupId),
    queryFn: () => codesApi.getCodeDetails(groupId),
    enabled: !!groupId,
    staleTime: CODES_STALE_TIME,
  });
}

/**
 * Tra ve Record<code, name> de truyen thang vao prop `items` cua <Select>.
 * Khi dang load hoac loi -> tra `fallback` (neu co) de UI khong vo.
 */
export function useCodeItems(
  groupId: string,
  fallback?: Record<string, string>
): Record<string, string> {
  const { data, isSuccess } = useCodes(groupId);

  return useMemo(() => {
    if (isSuccess && data && data.length > 0) {
      return Object.fromEntries(data.map((c) => [c.code, c.name]));
    }
    return fallback ?? {};
  }, [isSuccess, data, fallback]);
}
