"use client";

import { useState, useRef } from "react";
import { Search, Star, Clock, Plus } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { useIcd10Search } from "@/lib/hooks/use-icd10";
import { Skeleton } from "@/components/ui/skeleton";
import type { Icd10Response, DiagnosisType } from "@/lib/api/types";
import { cn } from "@/lib/utils";

const RECENTLY_USED_KEY = "icd10_recently_used";
const FAVORITES_KEY = "icd10_favorites";

function getLocalList(key: string): Icd10Response[] {
  if (typeof window === "undefined") return [];
  try {
    return JSON.parse(localStorage.getItem(key) ?? "[]");
  } catch {
    return [];
  }
}

function addToLocalList(key: string, item: Icd10Response) {
  const list = getLocalList(key).filter((x) => x.code !== item.code);
  list.unshift(item);
  localStorage.setItem(key, JSON.stringify(list.slice(0, 20)));
}

interface Props {
  onSelect: (item: Icd10Response, type: DiagnosisType) => void;
  className?: string;
}

export function Icd10Picker({ onSelect, className }: Props) {
  const [query, setQuery] = useState("");
  const [selectedType, setSelectedType] = useState<DiagnosisType>("PRIMARY");
  const [favorites, setFavorites] = useState<Icd10Response[]>(() => getLocalList(FAVORITES_KEY));
  const [recentlyUsed] = useState<Icd10Response[]>(() => getLocalList(RECENTLY_USED_KEY));

  const { data: results, isLoading } = useIcd10Search({
    q: query,
    type: "all",
    limit: 15,
  });

  function handleSelect(item: Icd10Response) {
    addToLocalList(RECENTLY_USED_KEY, item);
    onSelect(item, selectedType);
    setQuery("");
  }

  function toggleFavorite(item: Icd10Response, e: React.MouseEvent) {
    e.stopPropagation();
    const list = getLocalList(FAVORITES_KEY);
    const exists = list.some((x) => x.code === item.code);
    const updated = exists
      ? list.filter((x) => x.code !== item.code)
      : [item, ...list].slice(0, 20);
    localStorage.setItem(FAVORITES_KEY, JSON.stringify(updated));
    setFavorites(updated);
  }

  const isFavorite = (code: string) => favorites.some((x) => x.code === code);

  const showResults = query.trim().length >= 1;
  const showList = showResults ? results ?? [] : [];

  return (
    <div className={cn("space-y-3", className)}>
      {/* Type selector */}
      <div className="flex gap-2">
        {(["PRIMARY", "SECONDARY"] as DiagnosisType[]).map((t) => (
          <Button
            key={t}
            variant={selectedType === t ? "default" : "outline"}
            size="sm"
            onClick={() => setSelectedType(t)}
            type="button"
          >
            {t === "PRIMARY" ? "Chính" : "Phụ"}
          </Button>
        ))}
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-muted-foreground" />
        <Input
          className="pl-9"
          placeholder="Tìm ICD-10 (VD: đái tháo đường, E11...)"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          aria-label="Tìm kiếm ICD-10"
        />
      </div>

      {/* Results or default lists */}
      <div className="rounded-md border bg-card max-h-72 overflow-y-auto">
        {showResults ? (
          isLoading ? (
            <div className="p-3 space-y-2">
              {[1, 2, 3].map((i) => <Skeleton key={i} className="h-10 w-full" />)}
            </div>
          ) : showList.length === 0 ? (
            <p className="p-4 text-sm text-muted-foreground text-center">Không tìm thấy mã ICD-10</p>
          ) : (
            <ul>
              {showList.map((item) => (
                <Icd10Item
                  key={item.code}
                  item={item}
                  isFav={isFavorite(item.code)}
                  onSelect={handleSelect}
                  onToggleFav={(e) => toggleFavorite(item, e)}
                />
              ))}
            </ul>
          )
        ) : (
          <div>
            {favorites.length > 0 && (
              <div>
                <p className="px-3 py-1.5 text-xs font-medium text-muted-foreground flex items-center gap-1">
                  <Star className="h-3 w-3" /> Yêu thích
                </p>
                {favorites.slice(0, 5).map((item) => (
                  <Icd10Item
                    key={item.code}
                    item={item}
                    isFav={true}
                    onSelect={handleSelect}
                    onToggleFav={(e) => toggleFavorite(item, e)}
                  />
                ))}
              </div>
            )}
            {recentlyUsed.length > 0 && (
              <div>
                <p className="px-3 py-1.5 text-xs font-medium text-muted-foreground flex items-center gap-1">
                  <Clock className="h-3 w-3" /> Gần đây
                </p>
                {recentlyUsed.slice(0, 5).map((item) => (
                  <Icd10Item
                    key={item.code}
                    item={item}
                    isFav={isFavorite(item.code)}
                    onSelect={handleSelect}
                    onToggleFav={(e) => toggleFavorite(item, e)}
                  />
                ))}
              </div>
            )}
            {favorites.length === 0 && recentlyUsed.length === 0 && (
              <p className="p-4 text-sm text-muted-foreground text-center">
                Nhập tên bệnh hoặc mã ICD-10 để tìm kiếm
              </p>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function Icd10Item({
  item,
  isFav,
  onSelect,
  onToggleFav,
}: {
  item: Icd10Response;
  isFav: boolean;
  onSelect: (item: Icd10Response) => void;
  onToggleFav: (e: React.MouseEvent) => void;
}) {
  return (
    <li className="flex items-center gap-2 px-3 py-2 hover:bg-accent cursor-pointer group">
      <button
        type="button"
        className="flex-1 text-left"
        onClick={() => onSelect(item)}
      >
        <span className="font-mono text-xs text-primary font-semibold mr-2">{item.code}</span>
        <span className="text-sm">{item.name_vi}</span>
        {!item.is_billable && (
          <Badge variant="outline" className="ml-2 text-xs">Không thanh toán</Badge>
        )}
      </button>
      <button
        type="button"
        onClick={onToggleFav}
        className={cn(
          "h-6 w-6 flex items-center justify-center rounded opacity-0 group-hover:opacity-100 transition-opacity",
          isFav ? "opacity-100 text-yellow-500" : "text-muted-foreground"
        )}
        aria-label={isFav ? "Bỏ yêu thích" : "Thêm yêu thích"}
      >
        <Star className={cn("h-3.5 w-3.5", isFav && "fill-current")} />
      </button>
      <button
        type="button"
        onClick={() => onSelect(item)}
        className="h-6 w-6 flex items-center justify-center rounded opacity-0 group-hover:opacity-100 transition-opacity text-primary"
        aria-label="Thêm chẩn đoán"
      >
        <Plus className="h-3.5 w-3.5" />
      </button>
    </li>
  );
}
