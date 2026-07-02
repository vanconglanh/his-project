"use client";

import { useState } from "react";
import Link from "next/link";
import { Bell, Check, CheckCheck, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuTrigger,
  DropdownMenuSeparator,
} from "@/components/ui/dropdown-menu";
import { ScrollArea } from "@/components/ui/scroll-area";
import { format, parseISO } from "date-fns";
import { vi } from "date-fns/locale";
import {
  useUnreadCount,
  useNotificationInbox,
  useMarkRead,
  useMarkAllRead,
  useDeleteNotification,
} from "@/lib/hooks/use-notifications";
import { cn } from "@/lib/utils";

export function NotificationDropdown() {
  const [open, setOpen] = useState(false);
  const { data: countData, isError: countError } = useUnreadCount();
  const { data, isError: inboxError } = useNotificationInbox({ page_size: 10 });
  const markRead = useMarkRead();
  const markAllRead = useMarkAllRead();
  const deleteNotif = useDeleteNotification();

  // Ẩn badge nếu lỗi hoặc chưa có dữ liệu
  const unreadCount = countError || countData === undefined ? null : countData.count;
  const notifications = data?.data ?? [];

  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger
        render={
          <Button variant="ghost" size="icon" className="relative" aria-label="Thông báo">
            <Bell className="h-4 w-4" />
            {unreadCount !== null && unreadCount > 0 && (
              <Badge
                className="absolute -top-0.5 -right-0.5 h-4 min-w-[16px] px-1 text-[10px] leading-none flex items-center justify-center"
                aria-label={`${unreadCount} thông báo chưa đọc`}
              >
                {unreadCount > 99 ? "99+" : unreadCount}
              </Badge>
            )}
          </Button>
        }
      />

      <DropdownMenuContent align="end" className="w-[360px] p-0" sideOffset={8}>
        {/* Header */}
        <div className="flex items-center justify-between px-4 py-3 border-b">
          <span className="font-semibold text-sm">Thông báo</span>
          {unreadCount !== null && unreadCount > 0 && (
            <Button
              variant="ghost"
              size="sm"
              className="h-7 text-xs"
              onClick={() => markAllRead.mutate()}
            >
              <CheckCheck className="mr-1.5 h-3 w-3" />
              Đánh dấu tất cả đã đọc
            </Button>
          )}
        </div>

        {/* List */}
        <ScrollArea className="max-h-[400px]">
          {inboxError ? (
            <div className="py-10 text-center text-sm text-muted-foreground">
              Không tải được thông báo
            </div>
          ) : notifications.length === 0 ? (
            <div className="py-10 text-center text-sm text-muted-foreground">
              Không có thông báo mới
            </div>
          ) : (
            <ul>
              {notifications.map((notif) => (
                <li
                  key={notif.id}
                  className={cn(
                    "flex items-start gap-3 px-4 py-3 hover:bg-muted/50 transition-colors border-b last:border-0",
                    !notif.read_at && "bg-primary/5"
                  )}
                >
                  {/* Unread indicator */}
                  <div className="mt-1.5 shrink-0">
                    {!notif.read_at ? (
                      <div className="h-2 w-2 rounded-full bg-primary" />
                    ) : (
                      <div className="h-2 w-2 rounded-full bg-transparent" />
                    )}
                  </div>

                  <div className="flex-1 min-w-0">
                    <p className="text-sm font-medium leading-tight truncate">{notif.title}</p>
                    <p className="text-xs text-muted-foreground mt-0.5 line-clamp-2">{notif.body}</p>
                    <p className="text-[10px] text-muted-foreground mt-1">
                      {notif.created_at
                        ? format(parseISO(notif.created_at), "HH:mm dd/MM/yyyy", { locale: vi })
                        : "—"}
                    </p>
                  </div>

                  <div className="flex flex-col gap-1 shrink-0">
                    {!notif.read_at && (
                      <Button
                        variant="ghost"
                        size="icon"
                        className="h-6 w-6"
                        onClick={() => markRead.mutate(notif.id)}
                        title="Đánh dấu đã đọc"
                      >
                        <Check className="h-3 w-3" />
                      </Button>
                    )}
                    <Button
                      variant="ghost"
                      size="icon"
                      className="h-6 w-6 text-destructive hover:text-destructive"
                      onClick={() => deleteNotif.mutate(notif.id)}
                      title="Xoá"
                    >
                      <Trash2 className="h-3 w-3" />
                    </Button>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </ScrollArea>

        <DropdownMenuSeparator />
        {/* Footer */}
        <div className="p-2">
          <Link href="/notifications" onClick={() => setOpen(false)}>
            <Button variant="ghost" size="sm" className="w-full text-xs">
              Xem tất cả thông báo
            </Button>
          </Link>
        </div>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
