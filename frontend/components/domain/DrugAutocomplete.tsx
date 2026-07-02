"use client";

import { useState, useRef, useEffect } from "react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";
import { Skeleton } from "@/components/ui/skeleton";
import { cn } from "@/lib/utils";
import { useDrugSearch } from "@/lib/hooks/use-drugs";
import type { DrugMasterResponse } from "@/lib/api/drugs";
import { Search } from "lucide-react";

interface Props {
  onSelect: (drug: DrugMasterResponse) => void;
  placeholder?: string;
  className?: string;
}

export function DrugAutocomplete({ onSelect, placeholder = "Tìm thuốc theo tên hoặc mã...", className }: Props) {
  const [query, setQuery] = useState("");
  const [open, setOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const { data: drugs, isLoading } = useDrugSearch(query);

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  function handleSelect(drug: DrugMasterResponse) {
    onSelect(drug);
    setQuery("");
    setOpen(false);
  }

  return (
    <div ref={ref} className={cn("relative", className)}>
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
        <Input
          value={query}
          onChange={(e) => {
            setQuery(e.target.value);
            setOpen(true);
          }}
          onFocus={() => query.length >= 1 && setOpen(true)}
          placeholder={placeholder}
          className="pl-9"
          aria-label="Tìm thuốc"
          aria-autocomplete="list"
          aria-expanded={open}
        />
      </div>

      {open && query.length >= 1 && (
        <div
          className="absolute z-50 mt-1 w-full rounded-md border bg-popover shadow-md max-h-72 overflow-y-auto"
          role="listbox"
        >
          {isLoading ? (
            <div className="p-2 space-y-2">
              {[1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : !drugs || drugs.length === 0 ? (
            <p className="p-4 text-sm text-muted-foreground text-center">Không tìm thấy thuốc</p>
          ) : (
            <ul>
              {drugs.map((drug) => (
                <li key={drug.id}>
                  <button
                    type="button"
                    role="option"
                    className="w-full text-left px-3 py-2 hover:bg-accent transition-colors flex items-start gap-2"
                    onClick={() => handleSelect(drug)}
                  >
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className="font-medium text-sm truncate">{drug.name_vi}</span>
                        {drug.strength && (
                          <span className="text-xs text-muted-foreground">{drug.strength}</span>
                        )}
                        {drug.is_psychotropic && (
                          <Badge variant="destructive" className="text-[10px] px-1 py-0">Hướng thần</Badge>
                        )}
                        {drug.is_narcotic && (
                          <Badge variant="destructive" className="text-[10px] px-1 py-0">Gây nghiện</Badge>
                        )}
                        {!drug.requires_prescription && (
                          <Badge variant="secondary" className="text-[10px] px-1 py-0">OTC</Badge>
                        )}
                      </div>
                      <div className="text-xs text-muted-foreground flex gap-2 mt-0.5">
                        <span>{drug.form}</span>
                        {drug.manufacturer && <span>· {drug.manufacturer}</span>}
                        {drug.price && (
                          <span>· {drug.price.toLocaleString("vi-VN")}đ/{drug.unit}</span>
                        )}
                      </div>
                    </div>
                    <span className="text-xs font-mono text-muted-foreground shrink-0">{drug.code}</span>
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
