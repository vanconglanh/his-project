"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { standardSchemaResolver } from "@hookform/resolvers/standard-schema";
import { z } from "zod";
import { Loader2, CheckCircle2 } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { useForgotPassword } from "@/lib/hooks/use-users";

const schema = z.object({
  email: z.string().email("Email không hợp lệ"),
});

type FormData = z.infer<typeof schema>;

export default function ForgotPasswordPage() {
  const forgotPassword = useForgotPassword();
  const [sent, setSent] = useState(false);

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormData>({
    resolver: standardSchemaResolver(schema),
  });

  async function onSubmit(data: FormData) {
    try {
      await forgotPassword.mutateAsync(data.email);
    } catch {
      // BE luôn trả 204, FE không phân biệt thành công/thất bại để tránh enumeration
    }
    setSent(true);
  }

  if (sent) {
    return (
      <div className="space-y-4 text-center">
        <CheckCircle2 className="h-12 w-12 text-green-500 mx-auto" />
        <div className="space-y-1">
          <h2 className="text-xl font-semibold">Kiểm tra email của bạn</h2>
          <p className="text-sm text-muted-foreground">
            Nếu email tồn tại trong hệ thống, chúng tôi đã gửi liên kết đặt lại mật khẩu.
            Vui lòng kiểm tra hộp thư (kể cả thư rác).
          </p>
        </div>
        <Button variant="outline" className="w-full" render={<Link href="/login" />}>
          Quay lại đăng nhập
        </Button>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="space-y-1">
        <h2 className="text-xl font-semibold">Quên mật khẩu</h2>
        <p className="text-sm text-muted-foreground">
          Nhập email để nhận liên kết đặt lại mật khẩu
        </p>
      </div>

      <form onSubmit={handleSubmit(onSubmit)} noValidate className="space-y-4">
        <div className="space-y-1.5">
          <Label htmlFor="email">Email</Label>
          <Input
            id="email"
            type="email"
            placeholder="ten@phongkham.vn"
            autoComplete="email"
            {...register("email")}
          />
          {errors.email && (
            <p className="text-xs text-destructive">{errors.email.message}</p>
          )}
        </div>

        <Button
          type="submit"
          className="w-full h-11"
          disabled={isSubmitting || forgotPassword.isPending}
        >
          {(isSubmitting || forgotPassword.isPending) && (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          )}
          Gửi liên kết đặt lại
        </Button>
      </form>

      <Button variant="link" className="w-full h-auto p-0 text-sm" render={<Link href="/login" />}>
        Quay lại đăng nhập
      </Button>
    </div>
  );
}
