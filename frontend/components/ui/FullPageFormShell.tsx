"use client";

/**
 * FullPageFormShell — shell dùng chung cho các trang form Fullpage (create/edit).
 * Tránh copy-paste header/footer/sticky-bar giữa các route (F-09).
 * Tham khảo cấu trúc: app/(dashboard)/prescriptions/new/page.tsx,
 * app/(dashboard)/patients/_components/PatientEditorLayout.tsx.
 *
 * Shell KHÔNG biết gì về React Hook Form / mutation bên trong `children`.
 * Nó chỉ cung cấp: header (quay lại + tiêu đề + mô tả), vùng content
 * `max-w-5xl mx-auto`, và một `StickyActionBar` gọi `onSubmit` khi bấm
 * "Lưu" hoặc Ctrl+S. Trang cha (page.tsx) tự quyết định `onSubmit` thực sự
 * làm gì — thường là `formEl?.requestSubmit()` để kích hoạt submit gốc
 * của form con (đã có id riêng), giữ nguyên toàn bộ validation/logic của
 * form đó.
 */
import { useEffect, useRef } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { ArrowLeft, Save } from "lucide-react";
import { Button } from "@/components/ui/button";
import { StickyActionBar } from "@/components/ui/sticky-action-bar";

export interface FullPageFormShellProps {
  /** Tiêu đề hiển thị ở header (giữa) */
  title: string;
  /** Mô tả ngắn dưới tiêu đề, ẩn trên mobile để tiết kiệm chỗ */
  description?: string;
  /** Đường dẫn quay lại cố định — nếu có, nút quay lại render bằng <Link> (ưu tiên hơn onBack) */
  backHref?: string;
  /** Callback quay lại tuỳ biến — dùng khi không có backHref cố định (vd router.back(), confirm rời trang) */
  onBack?: () => void;
  /** Gọi khi bấm nút "Lưu" hoặc Ctrl+S. Trang cha tự quyết định cách trigger submit của form con. */
  onSubmit: () => void;
  /** Nhãn nút submit khi không đang submit, mặc định "Lưu" */
  submitLabel?: string;
  /** Đang submit — disable nút + đổi nhãn "Đang lưu..." */
  isSubmitting?: boolean;
  children: React.ReactNode;
}

export function FullPageFormShell({
  title,
  description,
  backHref,
  onBack,
  onSubmit,
  submitLabel = "Lưu",
  isSubmitting = false,
  children,
}: FullPageFormShellProps) {
  const router = useRouter();

  // Giữ tham chiếu mới nhất của props hay đổi để listener keydown không cần bind lại mỗi render
  const latest = useRef({ onSubmit, onBack, backHref, isSubmitting });
  useEffect(() => {
    latest.current = { onSubmit, onBack, backHref, isSubmitting };
  });

  const handleBack = () => {
    const { onBack: back, backHref: href } = latest.current;
    if (back) {
      back();
    } else if (href) {
      router.push(href);
    } else {
      router.back();
    }
  };

  // Ctrl+S submit / Esc quay lại — có cleanup khi unmount
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      const { onSubmit: submit, onBack: back, backHref: href, isSubmitting: submitting } = latest.current;
      if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "s") {
        e.preventDefault();
        if (!submitting) submit();
        return;
      }
      if (e.key === "Escape") {
        if (back) {
          back();
        } else if (href) {
          router.push(href);
        } else {
          router.back();
        }
      }
    };
    document.addEventListener("keydown", handler);
    return () => document.removeEventListener("keydown", handler);
  }, [router]);

  return (
    <div className="min-h-screen flex flex-col bg-background">
      {/* Header sticky */}
      <header className="sticky top-0 z-40 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="flex h-14 items-center gap-4 px-4 lg:px-6">
          {backHref ? (
            <Link
              href={backHref}
              className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
              aria-label="Quay lại"
            >
              <ArrowLeft className="h-4 w-4" />
              <span className="hidden sm:inline">Quay lại</span>
            </Link>
          ) : (
            <button
              type="button"
              onClick={handleBack}
              className="inline-flex items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground transition-colors"
              aria-label="Quay lại"
            >
              <ArrowLeft className="h-4 w-4" />
              <span className="hidden sm:inline">Quay lại</span>
            </button>
          )}

          <div className="flex-1 min-w-0 text-center">
            <h1 className="text-base font-semibold truncate">{title}</h1>
            {description && (
              <p className="hidden sm:block text-xs text-muted-foreground truncate">
                {description}
              </p>
            )}
          </div>

          <div className="flex items-center gap-2">
            <Button
              type="button"
              variant="outline"
              size="sm"
              onClick={handleBack}
              disabled={isSubmitting}
              className="hidden sm:inline-flex"
            >
              Huỷ
            </Button>
            <Button type="button" size="sm" onClick={onSubmit} disabled={isSubmitting}>
              <Save className="h-4 w-4 mr-1" />
              {isSubmitting ? "Đang lưu..." : submitLabel}
            </Button>
          </div>
        </div>
      </header>

      {/* Content */}
      <main className="flex-1 overflow-y-auto">
        <div className="max-w-5xl mx-auto px-6 py-8">
          {children}

          {/* Sticky action bar — bám cuối vùng content, full-bleed trong container px-6 */}
          <StickyActionBar left={<span>Ctrl+S lưu · Esc quay lại</span>}>
            <Button type="button" variant="outline" size="sm" onClick={handleBack} disabled={isSubmitting}>
              Huỷ
            </Button>
            <Button type="button" size="sm" onClick={onSubmit} disabled={isSubmitting}>
              <Save className="h-4 w-4 mr-1" />
              {isSubmitting ? "Đang lưu..." : submitLabel}
            </Button>
          </StickyActionBar>
        </div>
      </main>
    </div>
  );
}
