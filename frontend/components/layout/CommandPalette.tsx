"use client";

import { useEffect, useState, useCallback, useRef } from "react";
import { useRouter } from "next/navigation";
import {
  Search,
  Users,
  Stethoscope,
  ClipboardList,
  Receipt,
  UserPlus,
  BarChart3,
  Settings,
  ArrowRight,
  Pill,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { useUiStore } from "@/lib/stores/ui-store";
import { useDebounce } from "@/lib/hooks/use-debounce";
import { searchPatients } from "@/lib/api/patients";

interface CommandItem {
  id: string;
  label: string;
  description?: string;
  href?: string;
  action?: () => void;
  icon: React.ElementType;
  category: "navigate" | "action" | "patient";
  keywords?: string[];
}

const STATIC_ITEMS: CommandItem[] = [
  {
    id: "nav-dashboard",
    label: "Tổng quan",
    href: "/",
    icon: BarChart3,
    category: "navigate",
    keywords: ["dashboard", "tổng quan", "home"],
  },
  {
    id: "nav-reception",
    label: "Tiếp đón",
    href: "/reception",
    icon: UserPlus,
    category: "navigate",
    keywords: ["reception", "tiếp đón", "lễ tân"],
  },
  {
    id: "nav-patients",
    label: "Bệnh nhân",
    href: "/patients",
    icon: Users,
    category: "navigate",
    keywords: ["patients", "bệnh nhân", "BN"],
  },
  {
    id: "nav-encounters",
    label: "Khám bệnh",
    href: "/encounters",
    icon: Stethoscope,
    category: "navigate",
    keywords: ["encounters", "khám bệnh"],
  },
  {
    id: "nav-prescriptions",
    label: "Kê đơn",
    href: "/prescriptions",
    icon: ClipboardList,
    category: "navigate",
    keywords: ["prescriptions", "kê đơn", "đơn thuốc"],
  },
  {
    id: "nav-pharmacy",
    label: "Kho dược",
    href: "/pharmacy",
    icon: Pill,
    category: "navigate",
    keywords: ["pharmacy", "kho dược", "thuốc"],
  },
  {
    id: "nav-cashier",
    label: "Thu ngân",
    href: "/cashier",
    icon: Receipt,
    category: "navigate",
    keywords: ["cashier", "thu ngân", "thanh toán"],
  },
  {
    id: "nav-reports",
    label: "Báo cáo",
    href: "/reports",
    icon: BarChart3,
    category: "navigate",
    keywords: ["reports", "báo cáo"],
  },
  {
    id: "nav-admin",
    label: "Quản trị",
    href: "/admin",
    icon: Settings,
    category: "navigate",
    keywords: ["admin", "quản trị"],
  },
  {
    id: "action-new-patient",
    label: "Tạo bệnh nhân mới",
    description: "Mở form tạo bệnh nhân",
    href: "/patients?new=1",
    icon: Users,
    category: "action",
    keywords: ["tạo BN", "new patient", "thêm bệnh nhân"],
  },
];

const RECENT_KEY = "cmd_palette_recent";
const MAX_RECENT = 5;

function getRecent(): string[] {
  try {
    return JSON.parse(localStorage.getItem(RECENT_KEY) ?? "[]");
  } catch {
    return [];
  }
}

function saveRecent(id: string) {
  try {
    const recent = getRecent().filter((r) => r !== id);
    recent.unshift(id);
    localStorage.setItem(RECENT_KEY, JSON.stringify(recent.slice(0, MAX_RECENT)));
  } catch {
    // ignore
  }
}

export function CommandPalette() {
  const router = useRouter();
  const { commandPaletteOpen, setCommandPaletteOpen } = useUiStore();
  const [query, setQuery] = useState("");
  const [activeIndex, setActiveIndex] = useState(0);
  const [patientResults, setPatientResults] = useState<CommandItem[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const [recentIds, setRecentIds] = useState<string[]>([]);
  const inputRef = useRef<HTMLInputElement>(null);
  const listRef = useRef<HTMLDivElement>(null);
  const debouncedQuery = useDebounce(query, 300);

  useEffect(() => {
    if (commandPaletteOpen) {
      setRecentIds(getRecent());
      setQuery("");
      setActiveIndex(0);
      setTimeout(() => inputRef.current?.focus(), 50);
    }
  }, [commandPaletteOpen]);

  useEffect(() => {
    if (!debouncedQuery || debouncedQuery.length < 2) {
      setPatientResults([]);
      return;
    }
    setIsSearching(true);
    searchPatients({ q: debouncedQuery, page_size: 5 })
      .then((res) => {
        const items: CommandItem[] = (res.data ?? []).map(
          (p: { id: string; full_name: string; code: string; phone?: string }) => ({
            id: `patient-${p.id}`,
            label: p.full_name,
            description: `Mã BN: ${p.code}${p.phone ? " · " + p.phone : ""}`,
            href: `/patients/${p.id}`,
            icon: Users,
            category: "patient" as const,
          })
        );
        setPatientResults(items);
      })
      .catch(() => setPatientResults([]))
      .finally(() => setIsSearching(false));
  }, [debouncedQuery]);

  const filteredStatic = query
    ? STATIC_ITEMS.filter((item) => {
        const q = query.toLowerCase();
        return (
          item.label.toLowerCase().includes(q) ||
          item.description?.toLowerCase().includes(q) ||
          item.keywords?.some((k) => k.toLowerCase().includes(q))
        );
      })
    : STATIC_ITEMS;

  const recentItems = !query
    ? STATIC_ITEMS.filter((item) => recentIds.includes(item.id))
    : [];

  const allItems: CommandItem[] = query
    ? [...patientResults, ...filteredStatic]
    : [...recentItems, ...STATIC_ITEMS.filter((i) => !recentIds.includes(i.id))];

  const execute = useCallback(
    (item: CommandItem) => {
      saveRecent(item.id);
      setCommandPaletteOpen(false);
      if (item.action) {
        item.action();
      } else if (item.href) {
        router.push(item.href);
      }
    },
    [router, setCommandPaletteOpen]
  );

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (!commandPaletteOpen) return;
      if (e.key === "Escape") {
        setCommandPaletteOpen(false);
      } else if (e.key === "ArrowDown") {
        e.preventDefault();
        setActiveIndex((i) => Math.min(i + 1, allItems.length - 1));
      } else if (e.key === "ArrowUp") {
        e.preventDefault();
        setActiveIndex((i) => Math.max(i - 1, 0));
      } else if (e.key === "Enter") {
        const item = allItems[activeIndex];
        if (item) execute(item);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [commandPaletteOpen, allItems, activeIndex, execute, setCommandPaletteOpen]);

  useEffect(() => {
    const el = listRef.current?.children[activeIndex] as HTMLElement | undefined;
    el?.scrollIntoView({ block: "nearest" });
  }, [activeIndex]);

  if (!commandPaletteOpen) return null;

  const categoryLabel = (cat: CommandItem["category"]) => {
    if (cat === "patient") return "Bệnh nhân";
    if (cat === "action") return "Thao tác nhanh";
    return "Điều hướng";
  };

  // Group items for display
  const grouped: { label: string; items: CommandItem[] }[] = [];
  if (!query && recentItems.length > 0) {
    grouped.push({ label: "Gần đây", items: recentItems });
    const rest = STATIC_ITEMS.filter((i) => !recentIds.includes(i.id));
    if (rest.length > 0) grouped.push({ label: "Tất cả", items: rest });
  } else if (query) {
    if (patientResults.length > 0)
      grouped.push({ label: "Bệnh nhân", items: patientResults });
    if (filteredStatic.length > 0)
      grouped.push({ label: "Điều hướng & Thao tác", items: filteredStatic });
  } else {
    grouped.push({ label: "Điều hướng", items: STATIC_ITEMS });
  }

  let globalIndex = 0;

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-[15vh] px-4"
      onClick={() => setCommandPaletteOpen(false)}
    >
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/40 backdrop-blur-sm" />

      <div
        className="relative w-full max-w-lg bg-popover border border-border rounded-xl shadow-2xl overflow-hidden"
        onClick={(e) => e.stopPropagation()}
        role="dialog"
        aria-modal="true"
        aria-label="Command palette"
      >
        {/* Search input */}
        <div className="flex items-center gap-3 px-4 border-b border-border">
          <Search className="h-4 w-4 text-muted-foreground shrink-0" />
          <input
            ref={inputRef}
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Tìm kiếm bệnh nhân, điều hướng, thao tác..."
            className="flex-1 py-4 bg-transparent outline-none text-sm placeholder:text-muted-foreground"
            aria-label="Tìm kiếm"
            autoComplete="off"
          />
          {isSearching && (
            <div className="h-4 w-4 border-2 border-primary border-t-transparent rounded-full animate-spin" />
          )}
          <kbd className="hidden sm:inline-flex items-center gap-1 px-1.5 py-0.5 text-xs text-muted-foreground border border-border rounded">
            Esc
          </kbd>
        </div>

        {/* Results */}
        <div
          ref={listRef}
          className="max-h-80 overflow-y-auto py-2"
          role="listbox"
        >
          {allItems.length === 0 && query && !isSearching && (
            <div className="py-8 text-center text-sm text-muted-foreground">
              Không tìm thấy kết quả cho &ldquo;{query}&rdquo;
            </div>
          )}

          {grouped.map((group) => (
            <div key={group.label}>
              <p className="px-4 py-1.5 text-xs font-medium text-muted-foreground uppercase tracking-wider">
                {group.label}
              </p>
              {group.items.map((item) => {
                const idx = globalIndex++;
                const isActive = idx === activeIndex;
                const Icon = item.icon;
                return (
                  <button
                    key={item.id}
                    role="option"
                    aria-selected={isActive}
                    onClick={() => execute(item)}
                    onMouseEnter={() => setActiveIndex(idx)}
                    className={cn(
                      "w-full flex items-center gap-3 px-4 py-2.5 text-left transition-colors",
                      isActive ? "bg-accent text-accent-foreground" : "hover:bg-accent/50"
                    )}
                  >
                    <div
                      className={cn(
                        "flex h-8 w-8 items-center justify-center rounded-md border shrink-0",
                        item.category === "patient"
                          ? "bg-blue-50 border-blue-100 dark:bg-blue-900/20 dark:border-blue-800"
                          : item.category === "action"
                            ? "bg-amber-50 border-amber-100 dark:bg-amber-900/20 dark:border-amber-800"
                            : "bg-muted border-border"
                      )}
                    >
                      <Icon
                        className={cn(
                          "h-4 w-4",
                          item.category === "patient"
                            ? "text-blue-600 dark:text-blue-400"
                            : item.category === "action"
                              ? "text-amber-600 dark:text-amber-400"
                              : "text-muted-foreground"
                        )}
                      />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium truncate">{item.label}</p>
                      {item.description && (
                        <p className="text-xs text-muted-foreground truncate">
                          {item.description}
                        </p>
                      )}
                    </div>
                    {isActive && <ArrowRight className="h-3.5 w-3.5 text-muted-foreground shrink-0" />}
                  </button>
                );
              })}
            </div>
          ))}
        </div>

        {/* Footer */}
        <div className="border-t border-border px-4 py-2 flex items-center gap-4 text-xs text-muted-foreground">
          <span className="flex items-center gap-1">
            <kbd className="px-1.5 py-0.5 border border-border rounded">↑↓</kbd> chọn
          </span>
          <span className="flex items-center gap-1">
            <kbd className="px-1.5 py-0.5 border border-border rounded">↵</kbd> mở
          </span>
          <span className="flex items-center gap-1">
            <kbd className="px-1.5 py-0.5 border border-border rounded">Esc</kbd> đóng
          </span>
        </div>
      </div>
    </div>
  );
}
