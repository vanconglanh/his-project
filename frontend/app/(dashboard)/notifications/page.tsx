"use client";

import { useState } from "react";
import { Check, CheckCheck, Trash2, BellOff } from "lucide-react";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { cn } from "@/lib/utils";
import {
  useNotificationInbox,
  useMarkRead,
  useMarkAllRead,
  useDeleteNotification,
} from "@/lib/hooks/use-notifications";

export default function NotificationsPage() {
  const [page, setPage] = useState(1);
  const [unreadOnly, setUnreadOnly] = useState(false);

  const { data, isLoading } = useNotificationInbox({ page, page_size: 20, unread_only: unreadOnly });
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();
  const deleteNotif = useDeleteNotification();

  const notifications = data?.data ?? [];
  const total = data?.meta?.total ?? 0;
  const totalPages = Math.ceil(total / 20);

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-bold tracking-tight">Hộp thư thông báo</h1>
          <p className="text-sm text-muted-foreground">Tất cả thông báo của bạn</p>
        </div>
        <Button variant="outline" size="sm" onClick={() => markAllRead.mutate()}>
          <CheckCheck className="mr-2 h-3.5 w-3.5" />
          Đánh dấu tất cả đã đọc
        </Button>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3">
        <Select
          items={{ all: "Tất cả", unread: "Chưa đọc" }}
          value={unreadOnly ? "unread" : "all"}
          onValueChange={(v) => {
            setUnreadOnly(v === "unread");
            setPage(1);
          }}
        >
          <SelectTrigger className="w-36">
            <SelectValue />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="all">Tất cả</SelectItem>
            <SelectItem value="unread">Chưa đọc</SelectItem>
          </SelectContent>
        </Select>
        <span className="text-sm text-muted-foreground">
          {total} thông báo
        </span>
      </div>

      {/* List */}
      {isLoading ? (
        <div className="space-y-3">
          {[...Array(6)].map((_, i) => (
            <div key={i} className="h-20 animate-pulse rounded-lg bg-muted" />
          ))}
        </div>
      ) : notifications.length === 0 ? (
        <div className="flex flex-col items-center py-20 text-center">
          <BellOff className="h-12 w-12 text-muted-foreground mb-4" />
          <p className="font-medium">Không có thông báo</p>
          <p className="text-sm text-muted-foreground">
            {unreadOnly ? "Bạn đã đọc tất cả thông báo" : "Chưa có thông báo nào"}
          </p>
        </div>
      ) : (
        <div className="rounded-lg border divide-y">
          {notifications.map((notif) => (
            <div
              key={notif.id}
              className={cn(
                "flex items-start gap-4 p-4 hover:bg-muted/30 transition-colors",
                !notif.read_at && "bg-primary/5"
              )}
            >
              <div className="mt-1 shrink-0">
                {!notif.read_at ? (
                  <div className="h-2.5 w-2.5 rounded-full bg-primary" />
                ) : (
                  <div className="h-2.5 w-2.5 rounded-full bg-muted-foreground/20" />
                )}
              </div>

              <div className="flex-1 min-w-0">
                <div className="flex items-start justify-between gap-2">
                  <p className="font-medium text-sm">{notif.title}</p>
                  <Badge variant="outline" className="shrink-0 text-xs font-mono">
                    {notif.type}
                  </Badge>
                </div>
                <p className="text-sm text-muted-foreground mt-0.5">{notif.body}</p>
                <p className="text-xs text-muted-foreground mt-1.5">
                  {format(parseISO(notif.created_at), "HH:mm - EEEE, dd/MM/yyyy", { locale: vi })}
                </p>
              </div>

              <div className="flex gap-1 shrink-0">
                {!notif.read_at && (
                  <Button
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => markRead.mutate(notif.id)}
                    title="Đánh dấu đã đọc"
                    aria-label="Đánh dấu đã đọc"
                  >
                    <Check className="h-3.5 w-3.5" />
                  </Button>
                )}
                <Button
                  variant="ghost"
                  size="icon"
                  className="h-8 w-8 text-destructive hover:text-destructive"
                  onClick={() => deleteNotif.mutate(notif.id)}
                  title="Xoá thông báo"
                  aria-label="Xoá thông báo"
                >
                  <Trash2 className="h-3.5 w-3.5" />
                </Button>
              </div>
            </div>
          ))}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-center gap-2">
          <Button
            variant="outline"
            size="sm"
            disabled={page <= 1}
            onClick={() => setPage((p) => p - 1)}
          >
            Trước
          </Button>
          <span className="text-sm text-muted-foreground">
            Trang {page}/{totalPages}
          </span>
          <Button
            variant="outline"
            size="sm"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
          >
            Tiếp
          </Button>
        </div>
      )}
    </div>
  );
}
