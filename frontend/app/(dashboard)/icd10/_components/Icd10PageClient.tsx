"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { useIcd10Search } from "@/lib/hooks/use-icd10";
import { Search } from "lucide-react";
import type { Icd10Response } from "@/lib/api/types";

export function Icd10PageClient() {
  const [query, setQuery] = useState("");
  const { data, isLoading } = useIcd10Search({ q: query, limit: 50 });

  return (
    <div className="space-y-4">
      <div>
        <h2 className="text-xl font-bold tracking-tight">Tra cứu ICD-10</h2>
        <p className="text-sm text-muted-foreground">Tìm mã bệnh theo tiếng Việt hoặc mã ICD-10</p>
      </div>

      <div className="relative max-w-xl">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="VD: đái tháo đường, E11..."
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          autoFocus
        />
      </div>

      {query.length >= 1 && (
        <div className="rounded-xl border overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b bg-muted/40">
                <th className="text-left px-4 py-2 font-medium">Mã ICD-10</th>
                <th className="text-left px-4 py-2 font-medium">Tên tiếng Việt</th>
                <th className="text-left px-4 py-2 font-medium">Nhóm</th>
                <th className="text-left px-4 py-2 font-medium">Thanh toán</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {isLoading ? (
                Array.from({ length: 10 }).map((_, i) => (
                  <tr key={i}>
                    {[1, 2, 3, 4].map((j) => (
                      <td key={j} className="px-4 py-2"><Skeleton className="h-4 w-full" /></td>
                    ))}
                  </tr>
                ))
              ) : !data || data.length === 0 ? (
                <tr>
                  <td colSpan={4} className="px-4 py-10 text-center text-muted-foreground">
                    Không tìm thấy mã ICD-10 phù hợp
                  </td>
                </tr>
              ) : (
                data.map((item) => <Icd10Row key={item.code} item={item} />)
              )}
            </tbody>
          </table>
        </div>
      )}

      {!query && (
        <div className="flex flex-col items-center gap-3 py-16 text-muted-foreground">
          <Search className="h-12 w-12 opacity-20" />
          <p className="text-sm">Nhập tên bệnh hoặc mã ICD-10 để tìm kiếm</p>
        </div>
      )}
    </div>
  );
}

function Icd10Row({ item }: { item: Icd10Response }) {
  return (
    <tr className="hover:bg-accent/40 transition-colors">
      <td className="px-4 py-2">
        <span className="font-mono font-semibold text-primary">{item.code}</span>
      </td>
      <td className="px-4 py-2">{item.name_vi}</td>
      <td className="px-4 py-2 text-muted-foreground text-xs">{item.category}</td>
      <td className="px-4 py-2">
        <Badge variant={item.is_billable ? "default" : "outline"} className="text-xs">
          {item.is_billable ? "Có" : "Không"}
        </Badge>
      </td>
    </tr>
  );
}
