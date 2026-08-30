"use client";

import { useEffect, useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Input } from "@/components/ui/input";
import { Loader2, Search, CheckIcon } from "lucide-react";
import { cn } from "@/lib/utils";

export interface PickerOption {
  id: string;
  name: string;
  subtitle?: string;
}

interface ItemPickerProps {
  queryKeyPrefix: string;
  fetchOptions: (q: string) => Promise<PickerOption[]>;
  value: PickerOption | null;
  onChange: (option: PickerOption) => void;
  placeholder?: string;
  disabled?: boolean;
}

/**
 * Ô tìm kiếm/chọn item (dịch vụ hoặc thuốc) dạng autocomplete gọn nhẹ,
 * không phụ thuộc lib mới — chỉ dùng Input + danh sách dropdown thủ công.
 */
export function ItemPicker({
  queryKeyPrefix,
  fetchOptions,
  value,
  onChange,
  placeholder = "Tìm kiếm...",
  disabled,
}: ItemPickerProps) {
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState("");
  const containerRef = useRef<HTMLDivElement>(null);

  const { data: options = [], isFetching } = useQuery({
    queryKey: [queryKeyPrefix, "search", q],
    queryFn: () => fetchOptions(q),
    enabled: open,
  });

  useEffect(() => {
    function handleClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  return (
    <div className="relative" ref={containerRef}>
      <div className="relative">
        <Search className="pointer-events-none absolute left-2.5 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-8"
          placeholder={placeholder}
          disabled={disabled}
          value={open ? q : value?.name ?? ""}
          onFocus={() => {
            setOpen(true);
            setQ("");
          }}
          onChange={(e) => setQ(e.target.value)}
          autoComplete="off"
        />
        {isFetching && (
          <Loader2 className="absolute right-2.5 top-1/2 h-4 w-4 -translate-y-1/2 animate-spin text-muted-foreground" />
        )}
      </div>
      {open && (
        <div className="absolute z-50 mt-1 max-h-64 w-full overflow-y-auto rounded-md border bg-popover text-popover-foreground shadow-md">
          {options.length === 0 && !isFetching && (
            <div className="px-3 py-2 text-sm text-muted-foreground">
              {q ? "Không tìm thấy kết quả" : "Nhập từ khoá để tìm kiếm"}
            </div>
          )}
          {options.map((opt) => (
            <button
              type="button"
              key={opt.id}
              className={cn(
                "flex w-full items-center justify-between gap-2 px-3 py-2 text-left text-sm hover:bg-accent hover:text-accent-foreground",
                value?.id === opt.id && "bg-accent/60"
              )}
              onClick={() => {
                onChange(opt);
                setOpen(false);
              }}
            >
              <span>
                {opt.name}
                {opt.subtitle && (
                  <span className="ml-1 text-xs text-muted-foreground">{opt.subtitle}</span>
                )}
              </span>
              {value?.id === opt.id && <CheckIcon className="h-4 w-4 shrink-0" />}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
