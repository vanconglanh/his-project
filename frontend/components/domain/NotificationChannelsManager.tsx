"use client";

import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { Switch } from "@/components/ui/switch";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  useNotificationChannels,
  useCreateNotificationChannel,
  useUpdateNotificationChannel,
  useDeleteNotificationChannel,
  useTestNotificationChannel,
} from "@/lib/hooks/use-notification-channels";
import type {
  NotificationChannelResponse,
  NotificationChannelType,
} from "@/lib/api/notification-channels";
import { CheckCircle2, MessageSquare, Send, Wifi, Pencil, Trash2, Plus } from "lucide-react";

// ─── Dinh nghia truong cau hinh theo tung kenh ─────────────────────────────────
interface FieldDef {
  key: string;
  label: string;
  secret?: boolean;
  optional?: boolean;
  placeholder?: string;
}

const CHANNEL_META: Record<
  NotificationChannelType,
  { title: string; provider: string; icon: typeof MessageSquare; fields: FieldDef[] }
> = {
  SMS: {
    title: "SMS (eSMS)",
    provider: "ESMS",
    icon: MessageSquare,
    fields: [
      { key: "api_key", label: "API Key", secret: true, placeholder: "ApiKey eSMS..." },
      { key: "secret_key", label: "Secret Key", secret: true, placeholder: "SecretKey eSMS..." },
      { key: "brand_name", label: "Brandname", placeholder: "Tên thương hiệu (Brandname)..." },
      { key: "sms_type", label: "Loại SMS", optional: true, placeholder: "Mặc định: 2 (CSKH)" },
    ],
  },
  ZALO_ZNS: {
    title: "Zalo ZNS (Official Account)",
    provider: "ZALO_OA",
    icon: Send,
    fields: [
      { key: "access_token", label: "Access Token", secret: true, placeholder: "Access token Zalo OA..." },
      { key: "template_id", label: "Template ID mặc định", placeholder: "ID mẫu ZNS nhắc lịch..." },
      { key: "oa_id", label: "OA ID", optional: true, placeholder: "ID Official Account..." },
    ],
  },
};

interface EditorState {
  open: boolean;
  channel: NotificationChannelType;
  editing: NotificationChannelResponse | null;
}

export function NotificationChannelsManager() {
  const { data: channels, isLoading } = useNotificationChannels();
  const createMut = useCreateNotificationChannel();
  const updateMut = useUpdateNotificationChannel();
  const deleteMut = useDeleteNotificationChannel();
  const testMut = useTestNotificationChannel();

  const [editor, setEditor] = useState<EditorState>({ open: false, channel: "SMS", editing: null });
  const [form, setForm] = useState<Record<string, string>>({});
  const [isActive, setIsActive] = useState(true);

  const existingChannels = useMemo(
    () => new Set((channels ?? []).map((c) => c.channel)),
    [channels]
  );

  function openCreate(channel: NotificationChannelType) {
    setEditor({ open: true, channel, editing: null });
    setForm({});
    setIsActive(true);
  }

  function openEdit(c: NotificationChannelResponse) {
    setEditor({ open: true, channel: c.channel, editing: c });
    // Prefill cac truong khong nhay cam tu config_masked; truong bi mat (secret) de trong.
    const meta = CHANNEL_META[c.channel];
    const prefill: Record<string, string> = {};
    for (const f of meta.fields) {
      if (!f.secret) prefill[f.key] = c.config_masked?.[f.key] ?? "";
    }
    setForm(prefill);
    setIsActive(c.is_active);
  }

  function closeEditor() {
    setEditor((e) => ({ ...e, open: false }));
  }

  async function handleSubmit() {
    const meta = CHANNEL_META[editor.channel];
    const config: Record<string, string> = {};
    for (const f of meta.fields) {
      const v = (form[f.key] ?? "").trim();
      if (v) config[f.key] = v;
    }
    const body = {
      channel: editor.channel,
      provider: meta.provider,
      config,
      is_active: isActive,
    };
    if (editor.editing) {
      await updateMut.mutateAsync({ id: editor.editing.id, body });
    } else {
      await createMut.mutateAsync(body);
    }
    closeEditor();
  }

  const meta = CHANNEL_META[editor.channel];
  const saving = createMut.isPending || updateMut.isPending;

  return (
    <div className="space-y-6">
      {/* Nut them kenh */}
      <div className="flex flex-wrap gap-2">
        {(Object.keys(CHANNEL_META) as NotificationChannelType[]).map((ch) => {
          const m = CHANNEL_META[ch];
          const Icon = m.icon;
          return (
            <Button
              key={ch}
              variant="outline"
              size="sm"
              onClick={() => openCreate(ch)}
              disabled={existingChannels.has(ch)}
              title={existingChannels.has(ch) ? "Đã cấu hình kênh này" : undefined}
            >
              <Plus className="h-4 w-4 mr-1" />
              <Icon className="h-4 w-4 mr-1" />
              Thêm {m.title}
            </Button>
          );
        })}
      </div>

      {/* Danh sach kenh */}
      {isLoading ? (
        <div className="h-32 flex items-center justify-center text-sm text-muted-foreground">Đang tải...</div>
      ) : (channels ?? []).length === 0 ? (
        <div className="rounded-md border border-dashed p-8 text-center text-sm text-muted-foreground">
          Chưa có kênh thông báo nào. Nhấn nút phía trên để thêm cấu hình SMS hoặc Zalo ZNS.
        </div>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2">
          {(channels ?? []).map((c) => {
            const m = CHANNEL_META[c.channel];
            const Icon = m.icon;
            return (
              <Card key={c.id}>
                <CardHeader className="pb-3">
                  <CardTitle className="text-base flex items-center gap-2">
                    <Icon className="h-4 w-4" />
                    {m.title}
                    {c.is_active ? (
                      <Badge variant="outline" className="ml-auto border-status-done/30 text-status-done bg-status-done/10">
                        Đang bật
                      </Badge>
                    ) : (
                      <Badge variant="outline" className="ml-auto text-muted-foreground">
                        Đang tắt
                      </Badge>
                    )}
                  </CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                  <div className="flex items-center gap-2 text-sm">
                    {c.last_test_ok ? (
                      <Badge className="bg-status-done/10 text-status-done border-status-done/30" variant="outline">
                        <CheckCircle2 className="h-3 w-3 mr-1" />
                        Kết nối OK
                      </Badge>
                    ) : (
                      <Badge variant="destructive">Chưa kiểm tra</Badge>
                    )}
                    {c.last_tested_at && (
                      <span className="text-xs text-muted-foreground">
                        {new Date(c.last_tested_at).toLocaleString("vi-VN")}
                      </span>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => testMut.mutate(c.id)}
                      disabled={testMut.isPending}
                    >
                      <Wifi className="h-4 w-4 mr-1" />
                      Test kết nối
                    </Button>
                    <Button variant="outline" size="sm" onClick={() => openEdit(c)}>
                      <Pencil className="h-4 w-4 mr-1" />
                      Sửa
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => {
                        if (confirm("Xóa (reset) cấu hình kênh này?")) deleteMut.mutate(c.id);
                      }}
                      disabled={deleteMut.isPending}
                    >
                      <Trash2 className="h-4 w-4 mr-1" />
                      Xóa
                    </Button>
                  </div>
                </CardContent>
              </Card>
            );
          })}
        </div>
      )}

      {/* Dialog form tao/sua */}
      <Dialog open={editor.open} onOpenChange={(v) => (v ? null : closeEditor())}>
        <DialogContent className="sm:max-w-md">
          <DialogHeader>
            <DialogTitle>
              {editor.editing ? "Sửa cấu hình" : "Thêm cấu hình"} — {meta.title}
            </DialogTitle>
          </DialogHeader>

          <div className="space-y-4">
            {meta.fields.map((f) => (
              <div key={f.key} className="space-y-1">
                <Label htmlFor={`nc-${f.key}`}>
                  {f.label}
                  {!f.optional && <span className="text-destructive"> *</span>}
                </Label>
                <Input
                  id={`nc-${f.key}`}
                  type={f.secret ? "password" : "text"}
                  placeholder={
                    editor.editing && f.secret ? "Nhập mới để thay đổi..." : f.placeholder
                  }
                  value={form[f.key] ?? ""}
                  onChange={(e) => setForm((s) => ({ ...s, [f.key]: e.target.value }))}
                />
                {editor.editing && f.secret && (
                  <p className="text-xs text-muted-foreground">
                    Giá trị hiện tại: {editor.editing.config_masked?.[f.key] || "(chưa đặt)"} — để trống nếu giữ nguyên.
                  </p>
                )}
              </div>
            ))}

            <div className="flex items-center justify-between rounded-md border p-3">
              <div>
                <p className="text-sm font-medium">Kích hoạt kênh</p>
                <p className="text-xs text-muted-foreground">Tắt để tạm ngừng gửi qua kênh này.</p>
              </div>
              <Switch checked={isActive} onCheckedChange={setIsActive} />
            </div>
          </div>

          <DialogFooter>
            <Button variant="outline" onClick={closeEditor} disabled={saving}>
              Huỷ
            </Button>
            <Button onClick={handleSubmit} disabled={saving}>
              {saving ? "Đang lưu..." : "Lưu cấu hình"}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
