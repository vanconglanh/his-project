"use client";

import { useState } from "react";
import { Send } from "lucide-react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Dialog, DialogContent, DialogHeader, DialogTitle } from "@/components/ui/dialog";
import { Badge } from "@/components/ui/badge";
import { format, parseISO } from "date-fns";
import { VapidKeyGenerator } from "@/components/domain/VapidKeyGenerator";
import {
  useSendTestNotification,
  useNotificationLogs,
} from "@/lib/hooks/use-notifications";

const testSchema = z.object({
  title: z.string().min(1, "Nhập tiêu đề"),
  body: z.string().min(1, "Nhập nội dung"),
  user_id: z.string().optional(),
});
type TestForm = z.infer<typeof testSchema>;

export default function NotificationsConfigPage() {
  const [showTestDialog, setShowTestDialog] = useState(false);
  const { data: logs, isLoading: logsLoading } = useNotificationLogs({ page_size: 20 });
  const sendTestMutation = useSendTestNotification();

  const { register, handleSubmit, formState: { errors }, reset } = useForm<TestForm>({
    resolver: zodResolver(testSchema),
  });

  function onSubmitTest(values: TestForm) {
    sendTestMutation.mutate(values, {
      onSuccess: () => {
        setShowTestDialog(false);
        reset();
      },
    });
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-xl font-bold tracking-tight">Cấu hình Thông báo</h1>
        <p className="text-sm text-muted-foreground">Quản lý VAPID key và gửi thông báo test</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* VAPID Key */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">VAPID Key (Web Push)</CardTitle>
          </CardHeader>
          <CardContent>
            <VapidKeyGenerator />
          </CardContent>
        </Card>

        {/* Test Notification */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Gửi thông báo test</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-sm text-muted-foreground mb-4">
              Gửi một thông báo thử nghiệm tới một user hoặc toàn bộ người dùng đang online.
            </p>
            <Button onClick={() => setShowTestDialog(true)}>
              <Send className="mr-2 h-4 w-4" />
              Gửi thông báo test
            </Button>
          </CardContent>
        </Card>
      </div>

      {/* Recent Notification Logs */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Log thông báo gần đây</CardTitle>
        </CardHeader>
        <CardContent>
          {logsLoading ? (
            <div className="space-y-2">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="h-10 animate-pulse rounded bg-muted" />
              ))}
            </div>
          ) : !logs?.data?.length ? (
            <p className="text-center py-6 text-sm text-muted-foreground">Chưa có log thông báo</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="border-b">
                  <tr className="text-left text-muted-foreground">
                    <th className="pb-2 pr-4 font-medium">Thời gian</th>
                    <th className="pb-2 pr-4 font-medium">Loại</th>
                    <th className="pb-2 pr-4 font-medium">Tiêu đề</th>
                    <th className="pb-2 font-medium">Trạng thái</th>
                  </tr>
                </thead>
                <tbody>
                  {logs.data.map((log) => (
                    <tr key={log.id} className="border-b last:border-0 hover:bg-muted/20">
                      <td className="py-2 pr-4 text-muted-foreground whitespace-nowrap text-xs">
                        {log.created_at ? format(parseISO(log.created_at), "HH:mm dd/MM") : "—"}
                      </td>
                      <td className="py-2 pr-4">
                        <Badge variant="outline" className="text-xs font-mono">
                          {log.type}
                        </Badge>
                      </td>
                      <td className="py-2 pr-4 max-w-[200px] truncate">{log.title}</td>
                      <td className="py-2">
                        <Badge variant={log.read_at ? "secondary" : "default"} className="text-xs">
                          {log.read_at ? "Đã đọc" : "Chưa đọc"}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Test Dialog */}
      <Dialog open={showTestDialog} onOpenChange={setShowTestDialog}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Gửi thông báo test</DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmitTest)} className="space-y-4">
            <div className="space-y-1.5">
              <Label htmlFor="user_id">User ID (để trống = gửi tất cả)</Label>
              <Input id="user_id" {...register("user_id")} placeholder="uuid hoặc để trống" />
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="title">Tiêu đề *</Label>
              <Input id="title" {...register("title")} placeholder="Thông báo test" />
              {errors.title && (
                <p className="text-xs text-destructive">{errors.title.message}</p>
              )}
            </div>
            <div className="space-y-1.5">
              <Label htmlFor="body">Nội dung *</Label>
              <Textarea id="body" {...register("body")} placeholder="Nội dung thông báo..." rows={3} />
              {errors.body && (
                <p className="text-xs text-destructive">{errors.body.message}</p>
              )}
            </div>
            <div className="flex gap-2 justify-end">
              <Button type="button" variant="outline" onClick={() => setShowTestDialog(false)}>
                Huỷ
              </Button>
              <Button type="submit" disabled={sendTestMutation.isPending}>
                {sendTestMutation.isPending ? "Đang gửi..." : "Gửi"}
              </Button>
            </div>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
