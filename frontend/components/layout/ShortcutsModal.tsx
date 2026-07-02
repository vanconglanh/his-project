"use client";

import { useEffect, useState } from "react";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Keyboard } from "lucide-react";

interface ShortcutRow {
  keys: string[];
  description: string;
}

const SHORTCUTS: { group: string; items: ShortcutRow[] }[] = [
  {
    group: "Điều hướng toàn cục",
    items: [
      { keys: ["Ctrl", "K"], description: "Mở Command Palette" },
      { keys: ["?"], description: "Hiện bảng phím tắt này" },
      { keys: ["g", "p"], description: "Đến trang Bệnh nhân" },
      { keys: ["g", "e"], description: "Đến trang Khám bệnh" },
      { keys: ["g", "r"], description: "Đến trang Tiếp đón" },
      { keys: ["g", "c"], description: "Đến trang Thu ngân" },
      { keys: ["g", "x"], description: "Đến trang Kê đơn" },
      { keys: ["g", "h"], description: "Về Tổng quan" },
    ],
  },
  {
    group: "Lễ tân",
    items: [
      { keys: ["F2"], description: "Thêm bệnh nhân mới" },
      { keys: ["F4"], description: "Lưu form hiện tại" },
      { keys: ["Esc"], description: "Đóng dialog / huỷ thao tác" },
    ],
  },
  {
    group: "Bảng dữ liệu",
    items: [
      { keys: ["↑", "↓"], description: "Di chuyển giữa các dòng" },
      { keys: ["Enter"], description: "Mở chi tiết dòng đang chọn" },
    ],
  },
];

export function ShortcutsModal() {
  const [open, setOpen] = useState(false);

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      const tag = (e.target as HTMLElement)?.tagName;
      if (tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT") return;
      if (e.key === "?") {
        e.preventDefault();
        setOpen((o) => !o);
      }
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, []);

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogContent className="max-w-xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle className="flex items-center gap-2">
            <Keyboard className="h-5 w-5" />
            Phím tắt bàn phím
          </DialogTitle>
        </DialogHeader>

        <div className="space-y-5 mt-2">
          {SHORTCUTS.map((group) => (
            <div key={group.group}>
              <p className="text-xs font-semibold text-muted-foreground uppercase tracking-wider mb-2">
                {group.group}
              </p>
              <div className="space-y-1">
                {group.items.map((item, i) => (
                  <div
                    key={i}
                    className="flex items-center justify-between py-1.5 px-3 rounded-md hover:bg-muted/50"
                  >
                    <span className="text-sm text-foreground">{item.description}</span>
                    <div className="flex items-center gap-1">
                      {item.keys.map((key, ki) => (
                        <kbd
                          key={ki}
                          className="inline-flex items-center px-1.5 py-0.5 text-xs font-medium bg-muted border border-border rounded shadow-sm"
                        >
                          {key}
                        </kbd>
                      ))}
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ))}
        </div>
      </DialogContent>
    </Dialog>
  );
}
