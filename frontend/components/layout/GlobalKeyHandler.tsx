"use client";

import { useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import { useUiStore } from "@/lib/stores/ui-store";

// Vim-style "g + <key>" navigation
const G_MAP: Record<string, string> = {
  p: "/patients",
  e: "/encounters",
  r: "/reception",
  c: "/cashier",
  x: "/prescriptions",
  h: "/",
};

export function GlobalKeyHandler() {
  const router = useRouter();
  const { setCommandPaletteOpen } = useUiStore();
  const pendingG = useRef(false);
  const gTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      const tag = (e.target as HTMLElement)?.tagName;
      const isEditable =
        tag === "INPUT" ||
        tag === "TEXTAREA" ||
        tag === "SELECT" ||
        (e.target as HTMLElement)?.isContentEditable;

      // Ctrl+K or Cmd+K → Command Palette
      if ((e.ctrlKey || e.metaKey) && e.key === "k") {
        e.preventDefault();
        setCommandPaletteOpen(true);
        return;
      }

      if (isEditable) return;

      // Vim g+key navigation
      if (e.key === "g" && !e.ctrlKey && !e.metaKey && !e.altKey) {
        pendingG.current = true;
        if (gTimer.current) clearTimeout(gTimer.current);
        gTimer.current = setTimeout(() => {
          pendingG.current = false;
        }, 1000);
        return;
      }

      if (pendingG.current) {
        const dest = G_MAP[e.key];
        if (dest) {
          e.preventDefault();
          pendingG.current = false;
          if (gTimer.current) clearTimeout(gTimer.current);
          router.push(dest);
        }
        return;
      }

      // F2 → new patient
      if (e.key === "F2") {
        e.preventDefault();
        router.push("/patients?new=1");
        return;
      }
    }

    window.addEventListener("keydown", onKey);
    return () => {
      window.removeEventListener("keydown", onKey);
      if (gTimer.current) clearTimeout(gTimer.current);
    };
  }, [router, setCommandPaletteOpen]);

  return null;
}
