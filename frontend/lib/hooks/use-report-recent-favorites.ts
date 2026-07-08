"use client";

/**
 * Yêu thích / Gần dùng cho sidebar danh mục báo cáo — lưu localStorage theo report code,
 * tối đa 5 mục (báo cáo ⭐ đã ghim luôn ưu tiên đứng trước, phần còn lại là gần dùng gần nhất).
 */
import { useCallback, useEffect, useState } from "react";

const STORAGE_KEY = "reports-engine:recent-favorites";
const MAX_ITEMS = 5;

interface RecentFavoriteEntry {
  code: string;
  pinned: boolean;
  touchedAt: number;
}

function readStorage(): RecentFavoriteEntry[] {
  if (typeof window === "undefined") return [];
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return [];
    const parsed: unknown = JSON.parse(raw);
    if (!Array.isArray(parsed)) return [];
    return parsed.filter(
      (x): x is RecentFavoriteEntry =>
        !!x && typeof x === "object" && typeof (x as RecentFavoriteEntry).code === "string"
    );
  } catch {
    return [];
  }
}

function writeStorage(entries: RecentFavoriteEntry[]) {
  if (typeof window === "undefined") return;
  try {
    window.localStorage.setItem(STORAGE_KEY, JSON.stringify(entries));
  } catch {
    // bỏ qua lỗi quota localStorage
  }
}

function normalize(entries: RecentFavoriteEntry[]): RecentFavoriteEntry[] {
  const pinned = entries.filter((e) => e.pinned).sort((a, b) => b.touchedAt - a.touchedAt);
  const others = entries.filter((e) => !e.pinned).sort((a, b) => b.touchedAt - a.touchedAt);
  return [...pinned, ...others].slice(0, MAX_ITEMS);
}

export function useReportRecentFavorites() {
  const [entries, setEntries] = useState<RecentFavoriteEntry[]>([]);

  useEffect(() => {
    setEntries(normalize(readStorage()));
  }, []);

  /** Đánh dấu 1 báo cáo vừa được xem (gần dùng) — không ảnh hưởng trạng thái ghim. */
  const touchRecent = useCallback((code: string) => {
    setEntries((prev) => {
      const existing = prev.find((e) => e.code === code);
      const rest = prev.filter((e) => e.code !== code);
      const next = normalize([{ code, pinned: existing?.pinned ?? false, touchedAt: Date.now() }, ...rest]);
      writeStorage(next);
      return next;
    });
  }, []);

  /** Bật/tắt ghim yêu thích cho 1 báo cáo. */
  const toggleFavorite = useCallback((code: string) => {
    setEntries((prev) => {
      const existing = prev.find((e) => e.code === code);
      const rest = prev.filter((e) => e.code !== code);
      const next = normalize([
        { code, pinned: !(existing?.pinned ?? false), touchedAt: Date.now() },
        ...rest,
      ]);
      writeStorage(next);
      return next;
    });
  }, []);

  const isFavorite = useCallback(
    (code: string) => entries.some((e) => e.code === code && e.pinned),
    [entries]
  );

  return { items: entries, touchRecent, toggleFavorite, isFavorite };
}
