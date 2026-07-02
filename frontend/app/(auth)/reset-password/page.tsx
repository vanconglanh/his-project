"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { useState } from "react";
import { Eye, EyeOff, Loader2 } from "lucide-react";
import { toast } from "sonner";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useResetPassword } from "@/lib/hooks/use-users";

const schema = z
  .object({
    new_password: z
      .string()
      .min(12, "Tối thiểu 12 ký tự")
      .regex(/[A-Z]/, "Cần chữ hoa")
      .regex(/[a-z]/, "Cần chữ thường")
      .regex(/\d/, "Cần số")
      .regex(/[^A-Za-z0-9]/, "Cần ký tự đặc biệt"),
    confirm_password: z.string(),
  })
  .refine((d) => d.new_password === d.confirm_password, {
    message: "Mật khẩu không khớp",
    path: ["confirm_password"],
  });

type FormData = z.infer<typeof schema>;

export default function ResetPasswordPage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token") ?? "";
  const resetPassword = useResetPassword();
  const [showPass, setShowPass] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: standardSchemaResolver(schema),
  });

  if (!token) {
    return (
      <div className="text-center space-y-2">
        <p className="text-sm text-destructive font-medium">Liên kết không hợp lệ hoặc đã hết hạn</p>
        <Button variant="link" render={<Link href="/forgot-password" />}>
          Yêu cầu liên kết mới
        </Button>
      </div>
    );
  }

  async function onSubmit(data: FormData) {
    await resetPassword.mutateAsync({ token, new_password: data.new_password });
    toast.success("Đặt lại mật khẩu thành công! Vui lòng đăng nhập.");
    router.push("/login");
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1">
        <h2 className="text-xl font-semibold">Đặt lại mật khẩu</h2>
        <p className="text-sm text-muted-foreground">
          Nhập mật khẩu mới cho tài khoản của bạn
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="new-password">Mật khẩu mới *</Label>
          <div className="relative">
            <Input
              id="new-password"
              type={showPass ? "text" : "password"}
              className="pr-10"
              {...register("new_password")}
            />
            <Button
              type="button"
              variant="ghost"
              size="icon"
              className="absolute right-1 top-1/2 -translate-y-1/2 h-7 w-7"
              onClick={() => setShowPass((p) => !p)}
              aria-label={showPass ? "Ẩn mật khẩu" : "Hiện mật khẩu"}
            >
              {showPass ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </Button>
          </div>
          {errors.new_password && (
            <p className="text-xs text-destructive">{errors.new_password.message}</p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="confirm-password">Xác nhận mật khẩu *</Label>
          <Input
            id="confirm-password"
            type="password"
            {...register("confirm_password")}
          />
          {errors.confirm_password && (
            <p className="text-xs text-destructive">{errors.confirm_password.message}</p>
          )}
        </div>

        <Button
          type="submit"
          className="w-full h-11"
          disabled={isSubmitting || resetPassword.isPending}
        >
          {(isSubmitting || resetPassword.isPending) && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Đặt lại mật khẩu
        </Button>
      </form>
    </div>
  );
}
