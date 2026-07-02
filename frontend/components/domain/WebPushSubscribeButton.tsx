"use client";

import { useState, useCallback } from "react";
import { Bell, BellOff, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { toast } from "sonner";
import { useVapidPublicKey, useSubscribeWebPush, useUnsubscribeWebPush } from "@/lib/hooks/use-notifications";

function urlBase64ToUint8Array(base64String: string): ArrayBuffer {
  const padding = "=".repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);
  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray.buffer as ArrayBuffer;
}

interface WebPushSubscribeButtonProps {
  enabled?: boolean;
  onToggle?: (enabled: boolean) => void;
}

export function WebPushSubscribeButton({ enabled, onToggle }: WebPushSubscribeButtonProps) {
  const [isProcessing, setIsProcessing] = useState(false);
  const { data: vapidData } = useVapidPublicKey();
  const subscribeMutation = useSubscribeWebPush();
  const unsubscribeMutation = useUnsubscribeWebPush();

  const handleToggle = useCallback(async () => {
    if (!("Notification" in window) || !("serviceWorker" in navigator)) {
      toast.error("Trình duyệt không hỗ trợ thông báo đẩy");
      return;
    }

    setIsProcessing(true);
    try {
      if (enabled) {
        // Unsubscribe
        const registration = await navigator.serviceWorker.getRegistration();
        if (registration) {
          const sub = await registration.pushManager.getSubscription();
          if (sub) {
            await sub.unsubscribe();
            unsubscribeMutation.mutate(sub.endpoint);
          }
        }
        onToggle?.(false);
      } else {
        // Request permission
        const permission = await Notification.requestPermission();
        if (permission !== "granted") {
          toast.error("Bạn đã từ chối quyền thông báo. Hãy bật trong cài đặt trình duyệt.");
          return;
        }

        if (!vapidData?.public_key) {
          toast.error("VAPID key chưa được cấu hình. Liên hệ admin.");
          return;
        }

        // Register service worker & subscribe
        const registration = await navigator.serviceWorker.register("/sw.js");
        await navigator.serviceWorker.ready;

        const subscription = await registration.pushManager.subscribe({
          userVisibleOnly: true,
          applicationServerKey: urlBase64ToUint8Array(vapidData.public_key),
        });

        const { endpoint, keys } = subscription.toJSON() as {
          endpoint: string;
          keys?: { p256dh?: string; auth?: string };
        };

        await subscribeMutation.mutateAsync({
          endpoint,
          p256dh_key: keys?.p256dh ?? "",
          auth_key: keys?.auth ?? "",
          user_agent: navigator.userAgent,
        });

        onToggle?.(true);
      }
    } catch (err) {
      console.error("Web push error:", err);
      toast.error("Thao tác thất bại, vui lòng thử lại");
    } finally {
      setIsProcessing(false);
    }
  }, [enabled, vapidData, subscribeMutation, unsubscribeMutation, onToggle]);

  return (
    <Button
      variant={enabled ? "default" : "outline"}
      onClick={handleToggle}
      disabled={isProcessing}
      className="min-h-[44px]"
    >
      {isProcessing ? (
        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
      ) : enabled ? (
        <Bell className="mr-2 h-4 w-4" />
      ) : (
        <BellOff className="mr-2 h-4 w-4" />
      )}
      {enabled ? "Tắt thông báo trình duyệt" : "Bật thông báo trình duyệt"}
    </Button>
  );
}
