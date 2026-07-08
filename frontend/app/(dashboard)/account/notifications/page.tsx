"use client";

import { useEffect } from "react";
import { useForm, Controller } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { RadioGroup, RadioGroupItem } from "@/components/ui/radio-group";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { WebPushSubscribeButton } from "@/components/domain/WebPushSubscribeButton";
import {
  useNotificationPreferences,
  useUpdateNotificationPreferences,
} from "@/lib/hooks/use-notifications";

const schema = z.object({
  position: z.enum(["TOP_RIGHT", "BOTTOM_RIGHT", "CENTER"]),
  sound_enabled: z.boolean(),
  sound_name: z.string(),
  browser_push_enabled: z.boolean(),
});
type FormValues = z.infer<typeof schema>;

const SOUND_OPTIONS = [
  { value: "DEFAULT", label: "Mặc định" },
  { value: "CHIME", label: "Chuông (Chime)" },
  { value: "BELL", label: "Chuông nhỏ (Bell)" },
];

export default function AccountNotificationsPage() {
  const { data: prefs, isLoading } = useNotificationPreferences();
  const updateMutation = useUpdateNotificationPreferences();

  const { control, register, handleSubmit, watch, reset, setValue } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: {
      position: "TOP_RIGHT",
      sound_enabled: true,
      sound_name: "DEFAULT",
      browser_push_enabled: false,
    },
  });

  useEffect(() => {
    if (prefs) {
      reset({
        position: prefs.position ?? "TOP_RIGHT",
        sound_enabled: prefs.sound_enabled ?? true,
        sound_name: prefs.sound_name ?? "DEFAULT",
        browser_push_enabled: prefs.browser_push_enabled ?? false,
      });
    }
  }, [prefs, reset]);

  const soundEnabled = watch("sound_enabled");
  const browserPushEnabled = watch("browser_push_enabled");

  function previewSound(sound: string) {
    const sounds: Record<string, number[]> = {
      DEFAULT: [440, 100],
      CHIME: [523, 200],
      BELL: [659, 300],
    };
    const [freq, dur] = sounds[sound] ?? [440, 100];
    try {
      const ctx = new (window.AudioContext || (window as unknown as { webkitAudioContext: typeof AudioContext }).webkitAudioContext)();
      const osc = ctx.createOscillator();
      osc.connect(ctx.destination);
      osc.frequency.value = freq;
      osc.start();
      osc.stop(ctx.currentTime + dur / 1000);
    } catch {
      // ignore
    }
  }

  function onSubmit(values: FormValues) {
    updateMutation.mutate(values);
  }

  if (isLoading) {
    return (
      <div className="space-y-4 max-w-xl">
        {[...Array(3)].map((_, i) => (
          <div key={i} className="h-32 animate-pulse rounded-lg bg-muted" />
        ))}
      </div>
    );
  }

  return (
    <div className="max-w-xl space-y-6">
      <div>
        <h1 className="text-xl font-bold tracking-tight">Tuỳ chọn thông báo</h1>
        <p className="text-sm text-muted-foreground">Cá nhân hoá cách nhận thông báo</p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
        {/* Position */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Vị trí hiển thị</CardTitle>
          </CardHeader>
          <CardContent>
            <Controller
              name="position"
              control={control}
              render={({ field }) => (
                <RadioGroup
                  value={field.value}
                  onValueChange={field.onChange}
                  className="space-y-2"
                >
                  {[
                    { value: "TOP_RIGHT", label: "Góc trên phải" },
                    { value: "BOTTOM_RIGHT", label: "Góc dưới phải" },
                    { value: "CENTER", label: "Giữa màn hình" },
                  ].map((opt) => (
                    <div key={opt.value} className="flex items-center gap-3 min-h-[44px]">
                      <RadioGroupItem value={opt.value} id={`pos-${opt.value}`} />
                      <Label htmlFor={`pos-${opt.value}`} className="cursor-pointer">
                        {opt.label}
                      </Label>
                    </div>
                  ))}
                </RadioGroup>
              )}
            />
          </CardContent>
        </Card>

        {/* Sound */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Âm thanh</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between min-h-[44px]">
              <Label htmlFor="sound_enabled" className="cursor-pointer">
                Bật âm thanh thông báo
              </Label>
              <Controller
                name="sound_enabled"
                control={control}
                render={({ field }) => (
                  <Switch
                    id="sound_enabled"
                    checked={field.value}
                    onCheckedChange={field.onChange}
                  />
                )}
              />
            </div>

            {soundEnabled && (
              <div className="space-y-2">
                <Label>Chọn âm thanh</Label>
                <div className="flex items-center gap-2">
                  <Controller
                    name="sound_name"
                    control={control}
                    render={({ field }) => (
                      <Select
                        items={Object.fromEntries(SOUND_OPTIONS.map((o) => [o.value, o.label]))}
                        value={field.value}
                        onValueChange={field.onChange}
                      >
                        <SelectTrigger className="flex-1">
                          <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                          {SOUND_OPTIONS.map((opt) => (
                            <SelectItem key={opt.value} value={opt.value}>
                              {opt.label}
                            </SelectItem>
                          ))}
                        </SelectContent>
                      </Select>
                    )}
                  />
                  <Button
                    type="button"
                    variant="outline"
                    size="sm"
                    onClick={() => previewSound(watch("sound_name"))}
                  >
                    Nghe thử
                  </Button>
                </div>
              </div>
            )}
          </CardContent>
        </Card>

        {/* Browser Push */}
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Thông báo trình duyệt (Web Push)</CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <p className="text-sm text-muted-foreground">
              Nhận thông báo ngay cả khi không mở trang web. Cần cấp quyền thông báo cho trình
              duyệt.
            </p>
            <WebPushSubscribeButton
              enabled={browserPushEnabled}
              onToggle={(val) => setValue("browser_push_enabled", val, { shouldDirty: true })}
            />
          </CardContent>
        </Card>

        <Button type="submit" disabled={updateMutation.isPending} className="w-full">
          {updateMutation.isPending ? "Đang lưu..." : "Lưu tuỳ chọn"}
        </Button>
      </form>
    </div>
  );
}
