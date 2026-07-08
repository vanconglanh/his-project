"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";
import { ChevronLeftIcon } from "@/components/icons";
import { ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { ApiRequestError } from "@/lib/api";
import {
  useNotificationPreferences,
  useSavePushSubscription,
  useTenantInfo,
  useUpdateNotificationPreferences,
} from "@/lib/hooks";
import { isIosDevice, isIosStandalone, isPushSupported, subscribeToPush } from "@/lib/push";

export default function NotificationSettingsPage() {
  const router = useRouter();
  const { data: prefs, isLoading, isError, refetch } = useNotificationPreferences();
  const { data: tenant } = useTenantInfo();
  const updatePrefs = useUpdateNotificationPreferences();
  const saveSubscription = useSavePushSubscription();

  const [error, setError] = useState<string | null>(null);
  const [enablingPush, setEnablingPush] = useState(false);
  const [pushEnabledLocal, setPushEnabledLocal] = useState(false);

  const iosDevice = isIosDevice();
  const iosStandalone = isIosStandalone();
  const needsA2hs = iosDevice && !iosStandalone;

  function handleTogglePush(checked: boolean) {
    setError(null);
    updatePrefs.mutate(
      { push: checked, email: prefs?.email ?? false },
      {
        onError: (err) =>
          setError(err instanceof ApiRequestError ? err.message : "Không cập nhật được cài đặt"),
      },
    );
  }

  function handleToggleEmail(checked: boolean) {
    setError(null);
    updatePrefs.mutate(
      { push: prefs?.push ?? false, email: checked },
      {
        onError: (err) =>
          setError(err instanceof ApiRequestError ? err.message : "Không cập nhật được cài đặt"),
      },
    );
  }

  async function handleEnableBrowserPush() {
    if (!tenant?.vapidPublicKey) return;
    setError(null);
    setEnablingPush(true);
    try {
      const payload = await subscribeToPush(tenant.vapidPublicKey);
      if (!payload) {
        setError("Trình duyệt từ chối quyền thông báo hoặc không hỗ trợ");
        return;
      }
      saveSubscription.mutate(payload, {
        onSuccess: () => setPushEnabledLocal(true),
        onError: () => setError("Không lưu được đăng ký thông báo, vui lòng thử lại"),
      });
    } finally {
      setEnablingPush(false);
    }
  }

  return (
    <div className="p-4">
      <div className="mb-5 flex items-center gap-2 pt-4">
        <button
          type="button"
          onClick={() => router.push("/me")}
          aria-label="Quay lại"
          className="flex h-11 w-11 items-center justify-center rounded-full hover:bg-slate-100"
        >
          <ChevronLeftIcon className="h-6 w-6" />
        </button>
        <h1 className="text-slate-900">Cài đặt thông báo</h1>
      </div>

      {error && (
        <div className="mb-4 rounded-xl border-2 border-red-200 bg-red-50 p-3 text-center text-base font-medium text-red-700">
          {error}
        </div>
      )}

      {isLoading && <LoadingBlock label="Đang tải cài đặt..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {prefs && (
        <div className="flex flex-col gap-4">
          <div className="flex items-center justify-between rounded-2xl border-2 border-slate-200 bg-white p-4">
            <div>
              <p className="text-lg font-semibold text-slate-900">Thông báo đẩy (push)</p>
              <p className="text-base text-slate-500">Nhắc lịch hẹn, số thứ tự, uống thuốc</p>
            </div>
            <label className="relative inline-flex h-8 w-14 cursor-pointer items-center">
              <input
                type="checkbox"
                className="peer sr-only"
                checked={prefs.push}
                onChange={(e) => handleTogglePush(e.target.checked)}
                aria-label="Bật/tắt thông báo đẩy"
              />
              <span className="absolute inset-0 rounded-full bg-slate-300 transition-colors peer-checked:bg-blue-600" />
              <span className="absolute left-1 h-6 w-6 rounded-full bg-white transition-transform peer-checked:translate-x-6" />
            </label>
          </div>

          <div className="flex items-center justify-between rounded-2xl border-2 border-slate-200 bg-white p-4">
            <div>
              <p className="text-lg font-semibold text-slate-900">Thông báo email</p>
              <p className="text-base text-slate-500">Gửi thông báo qua email</p>
            </div>
            <label className="relative inline-flex h-8 w-14 cursor-pointer items-center">
              <input
                type="checkbox"
                className="peer sr-only"
                checked={prefs.email}
                onChange={(e) => handleToggleEmail(e.target.checked)}
                aria-label="Bật/tắt thông báo email"
              />
              <span className="absolute inset-0 rounded-full bg-slate-300 transition-colors peer-checked:bg-blue-600" />
              <span className="absolute left-1 h-6 w-6 rounded-full bg-white transition-transform peer-checked:translate-x-6" />
            </label>
          </div>

          {!isPushSupported() && !iosDevice && (
            <p className="rounded-xl bg-slate-100 p-3 text-base text-slate-600">
              Trình duyệt của bạn chưa hỗ trợ thông báo đẩy.
            </p>
          )}

          {needsA2hs && (
            <div className="rounded-2xl border-2 border-amber-300 bg-amber-50 p-4">
              <p className="mb-1 text-lg font-semibold text-amber-900">
                Thêm vào màn hình chính để nhận thông báo
              </p>
              <p className="text-base text-amber-800">
                Trên iPhone/iPad: bấm nút Chia sẻ trên Safari → chọn &quot;Thêm vào Màn hình chính&quot;.
                Sau đó mở lại ứng dụng từ màn hình chính để bật thông báo.
              </p>
            </div>
          )}

          {isPushSupported() && !needsA2hs && (
            <button
              type="button"
              onClick={handleEnableBrowserPush}
              disabled={enablingPush || pushEnabledLocal}
              className="min-h-14 rounded-2xl bg-blue-600 px-4 text-lg font-semibold text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {pushEnabledLocal
                ? "Đã bật thông báo trên thiết bị này"
                : enablingPush
                  ? "Đang bật thông báo..."
                  : "Bật thông báo trên thiết bị này"}
            </button>
          )}
        </div>
      )}
    </div>
  );
}
