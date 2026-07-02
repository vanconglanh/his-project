"use client";

import { useSearchParams, useRouter } from "next/navigation";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { useState } from "react";
import { Eye, EyeOff, Loader2 } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useAcceptInvite } from "@/lib/hooks/use-users";
import { useAuthStore } from "@/lib/stores/auth-store";

const schema = z
  .object({
    full_name: z.string().min(2, "Họ tên tối thiểu 2 ký tự"),
    password: z
      .string()
      .min(12, "Tối thiểu 12 ký tự")
      .regex(/[A-Z]/, "Cần chữ hoa")
      .regex(/[a-z]/, "Cần chữ thường")
      .regex(/\d/, "Cần số")
      .regex(/[^A-Za-z0-9]/, "Cần ký tự đặc biệt"),
    confirm_password: z.string(),
  })
  .refine((d) => d.password === d.confirm_password, {
    message: "Mật khẩu không khớp",
    path: ["confirm_password"],
  });

type FormData = z.infer<typeof schema>;

export default function AcceptInvitePage() {
  const searchParams = useSearchParams();
  const router = useRouter();
  const token = searchParams.get("token") ?? "";
  const acceptInvite = useAcceptInvite();
  const setAuth = useAuthStore((s) => s.setAuth);
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
        <p className="text-sm text-destructive font-medium">Liên kết mời không hợp lệ</p>
        <p className="text-xs text-muted-foreground">Vui lòng kiểm tra email và thử lại.</p>
      </div>
    );
  }

  async function onSubmit(data: FormData) {
    const result = await acceptInvite.mutateAsync({
      token,
      password: data.password,
      full_name: data.full_name,
    });
    // Auto login after accepting invite
    setAuth(
      {
        id: 0,
        email: result.user.email,
        fullName: result.user.full_name,
        role: "Admin",
        tenantId: 0,
        clinicId: 0,
        clinicName: "",
      },
      result.access_token,
      result.refresh_token
    );
    toast.success("Kích hoạt tài khoản thành công! Chào mừng bạn.");
    router.push("/");
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1">
        <h2 className="text-xl font-semibold">Kích hoạt tài khoản</h2>
        <p className="text-sm text-muted-foreground">
          Đặt mật khẩu để hoàn tất kích hoạt tài khoản của bạn
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="full-name">Họ và tên *</Label>
          <Input id="full-name" placeholder="Nguyễn Văn A" {...register("full_name")} />
          {errors.full_name && (
            <p className="text-xs text-destructive">{errors.full_name.message}</p>
          )}
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="password">Mật khẩu *</Label>
          <div className="relative">
            <Input
              id="password"
              type={showPass ? "text" : "password"}
              className="pr-10"
              {...register("password")}
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
          {errors.password && (
            <p className="text-xs text-destructive">{errors.password.message}</p>
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
          disabled={isSubmitting || acceptInvite.isPending}
        >
          {(isSubmitting || acceptInvite.isPending) && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Kích hoạt tài khoản
        </Button>
      </form>
    </div>
  );
}
