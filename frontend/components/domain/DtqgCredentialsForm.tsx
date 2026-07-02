"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import { useDtqgCredentials, useUpsertDtqgCredentials, useTestDtqgConnection } from "@/lib/hooks/use-dtqg";
import { CheckCircle, Wifi } from "lucide-react";

const schema = z.object({
  cskcb_id: z.string().min(1, "Bắt buộc"),
  partner_code: z.string().min(1, "Bắt buộc"),
  token: z.string().min(1, "Bắt buộc"),
});

type FormData = z.infer<typeof schema>;

export function DtqgCredentialsForm() {
  const { data: credentials, isLoading } = useDtqgCredentials();
  const upsert = useUpsertDtqgCredentials();
  const testConnection = useTestDtqgConnection();

  const { register, handleSubmit, formState: { errors } } = useForm<FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      cskcb_id: credentials?.cskcb_id ?? "",
      partner_code: credentials?.partner_code ?? "",
      token: "",
    },
  });

  async function onSubmit(data: FormData) {
    await upsert.mutateAsync(data);
  }

  if (isLoading) {
    return <div className="h-32 flex items-center justify-center text-sm text-muted-foreground">Đang tải...</div>;
  }

  return (
    <div className="space-y-6">
      {/* Current status */}
      {credentials && (
        <div className="flex items-center justify-between rounded-md border p-4">
          <div className="space-y-1">
            <p className="text-sm font-medium">Trạng thái kết nối</p>
            <div className="flex items-center gap-2">
              {credentials.last_test_ok ? (
                <Badge className="bg-green-100 text-green-800 border-green-300" variant="outline">
                  <CheckCircle className="h-3 w-3 mr-1" />
                  Kết nối OK
                </Badge>
              ) : (
                <Badge variant="destructive">Chưa kiểm tra</Badge>
              )}
              {credentials.last_tested_at && (
                <span className="text-xs text-muted-foreground">
                  Lần cuối: {new Date(credentials.last_tested_at).toLocaleString("vi-VN")}
                </span>
              )}
            </div>
          </div>
          <Button
            variant="outline"
            size="sm"
            onClick={() => testConnection.mutate()}
            disabled={testConnection.isPending}
          >
            <Wifi className="h-4 w-4 mr-2" />
            {testConnection.isPending ? "Đang kiểm tra..." : "Test kết nối"}
          </Button>
        </div>
      )}

      {/* Credentials form */}
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="space-y-1">
          <Label htmlFor="cskcb_id">Mã CSKCB <span className="text-destructive">*</span></Label>
          <Input
            id="cskcb_id"
            placeholder="Mã cơ sở khám chữa bệnh..."
            defaultValue={credentials?.cskcb_id ?? ""}
            {...register("cskcb_id")}
            aria-invalid={!!errors.cskcb_id}
          />
          {errors.cskcb_id && <p className="text-xs text-destructive">{errors.cskcb_id.message}</p>}
        </div>

        <div className="space-y-1">
          <Label htmlFor="partner_code">Partner Code <span className="text-destructive">*</span></Label>
          <Input
            id="partner_code"
            placeholder="Mã đối tác ĐTQG..."
            defaultValue={credentials?.partner_code ?? ""}
            {...register("partner_code")}
            aria-invalid={!!errors.partner_code}
          />
          {errors.partner_code && <p className="text-xs text-destructive">{errors.partner_code.message}</p>}
        </div>

        <div className="space-y-1">
          <Label htmlFor="token">Token <span className="text-destructive">*</span></Label>
          <Input
            id="token"
            type="password"
            placeholder={credentials ? "Nhập token mới để thay đổi..." : "Token ĐTQG..."}
            {...register("token")}
            aria-invalid={!!errors.token}
          />
          {credentials && (
            <p className="text-xs text-muted-foreground">Token hiện tại: {credentials.token_masked}</p>
          )}
          {errors.token && <p className="text-xs text-destructive">{errors.token.message}</p>}
        </div>

        <div className="flex justify-end">
          <Button type="submit" disabled={upsert.isPending}>
            {upsert.isPending ? "Đang lưu..." : "Lưu thông tin kết nối"}
          </Button>
        </div>
      </form>
    </div>
  );
}
