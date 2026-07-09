"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { BigButton } from "@/components/BigButton";
import { ConfirmDialog } from "@/components/ConfirmDialog";
import { ErrorBlock, LoadingBlock } from "@/components/StateViews";
import { clearTokenCookie } from "@/lib/auth";
import { useLogoutMutation, useMe } from "@/lib/hooks";
import { formatDate } from "@/lib/utils";

export default function ProfilePage() {
  const router = useRouter();
  const { data: me, isLoading, isError, refetch } = useMe();
  const logoutMutation = useLogoutMutation();
  const [confirmLogout, setConfirmLogout] = useState(false);

  function handleLogout() {
    logoutMutation.mutate(undefined, {
      onSettled: () => {
        clearTokenCookie();
        router.push("/login");
      },
    });
  }

  return (
    <div className="p-4">
      <h1 className="mb-5 pt-4 text-slate-900">Hồ sơ của tôi</h1>

      {isLoading && <LoadingBlock label="Đang tải hồ sơ..." />}
      {isError && <ErrorBlock error={undefined} onRetry={() => refetch()} />}

      {me && (
        <div className="flex flex-col gap-4">
          <div className="rounded-2xl border border-[var(--border-soft)] bg-white p-4 shadow-[var(--shadow-card)]">
            <dl className="divide-y divide-slate-100">
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Họ và tên</dt>
                <dd className="mt-0.5 text-lg font-semibold text-slate-900">{me.fullName}</dd>
              </div>
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Mã bệnh nhân</dt>
                <dd className="mt-0.5 text-lg font-semibold text-slate-900">{me.patientCode}</dd>
              </div>
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Ngày sinh</dt>
                <dd className="mt-0.5 text-lg text-slate-900">{formatDate(me.dob)}</dd>
              </div>
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Giới tính</dt>
                <dd className="mt-0.5 text-lg text-slate-900">{me.gender}</dd>
              </div>
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Số điện thoại</dt>
                <dd className="mt-0.5 text-lg text-slate-900">{me.phone}</dd>
              </div>
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Địa chỉ</dt>
                <dd className="mt-0.5 text-lg text-slate-900">{me.address}</dd>
              </div>
              <div className="py-3 first:pt-0 last:pb-0">
                <dt className="text-base text-slate-500">Số thẻ BHYT</dt>
                <dd className="mt-0.5 text-lg text-slate-900">{me.bhytNumber ?? "Chưa cập nhật"}</dd>
              </div>
            </dl>
          </div>

          <Link href="/encounters" className="block">
            <BigButton variant="secondary">Lịch sử khám bệnh</BigButton>
          </Link>
          <Link href="/prescriptions" className="block">
            <BigButton variant="secondary">Đơn thuốc của tôi</BigButton>
          </Link>
          <Link href="/medications" className="block">
            <BigButton variant="secondary">Nhắc uống thuốc</BigButton>
          </Link>
          <Link href="/settings/notifications" className="block">
            <BigButton variant="secondary">Cài đặt thông báo</BigButton>
          </Link>

          <BigButton variant="danger" onClick={() => setConfirmLogout(true)}>
            Đăng xuất
          </BigButton>
        </div>
      )}

      <ConfirmDialog
        open={confirmLogout}
        title="Đăng xuất?"
        description="Bạn có chắc muốn đăng xuất khỏi tài khoản này không?"
        confirmLabel="Đăng xuất"
        cancelLabel="Ở lại"
        loading={logoutMutation.isPending}
        onConfirm={handleLogout}
        onCancel={() => setConfirmLogout(false)}
      />
    </div>
  );
}
