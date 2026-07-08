"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { Button } from "@/components/ui/button";
import { FullPageFormShell } from "@/components/ui/FullPageFormShell";
import { ApiPartnerForm } from "@/components/domain/ApiPartnerForm";
import { ApiPartnerKeyDisplay } from "@/components/domain/ApiPartnerKeyDisplay";
import { useCreateApiPartner } from "@/lib/hooks/use-api-partners";

const FORM_ID = "api-partner-form";

export default function NewApiPartnerPage() {
  const router = useRouter();
  const createMutation = useCreateApiPartner();
  const [createdKey, setCreatedKey] = useState<string | null>(null);

  // Sau khi tạo xong: hiển thị API key ngay trong trang (không đưa key vào URL)
  if (createdKey) {
    return (
      <div className="min-h-screen flex flex-col bg-background">
        <header className="sticky top-0 z-40 border-b bg-background/95 px-4 lg:px-6">
          <div className="flex h-14 items-center gap-4">
            <Link
              href="/admin/api-partners"
              className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
            >
              <ArrowLeft className="h-4 w-4" />
              <span className="hidden sm:inline">Về danh sách</span>
            </Link>
            <h1 className="text-base font-semibold">API Key mới</h1>
          </div>
        </header>
        <main className="flex-1 overflow-y-auto">
          <div className="mx-auto max-w-lg px-6 py-8 space-y-4">
            <ApiPartnerKeyDisplay apiKey={createdKey} />
            <Button className="w-full" onClick={() => router.push("/admin/api-partners")}>
              Đã copy, về danh sách
            </Button>
          </div>
        </main>
      </div>
    );
  }

  return (
    <FullPageFormShell
      title="Tạo đối tác API"
      description="Cấp quyền truy cập Public API cho đối tác tích hợp"
      backHref="/admin/api-partners"
      onSubmit={() => (document.getElementById(FORM_ID) as HTMLFormElement | null)?.requestSubmit()}
      submitLabel="Tạo đối tác"
      isSubmitting={createMutation.isPending}
    >
      <div className="max-w-2xl rounded-lg border bg-card p-6">
        <ApiPartnerForm
          formId={FORM_ID}
          hideSubmit
          isLoading={createMutation.isPending}
          onSubmit={(data) =>
            createMutation.mutate(data, {
              onSuccess: (res) => setCreatedKey(res.api_key_plain),
            })
          }
        />
      </div>
    </FullPageFormShell>
  );
}
