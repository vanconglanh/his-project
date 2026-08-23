"use client";

import { toast } from "sonner";

/**
 * Hien hop thoai nhap URL anh (dung cho toolbar chen anh cua Tiptap editor)
 * va chi chap nhan URL co scheme http:// hoac https:// de tranh chen
 * javascript: hoac cac scheme nguy hiem khac vao noi dung mau benh an.
 *
 * Tra ve URL hop le (da chuan hoa) hoac null neu nguoi dung huy / nhap sai.
 */
export function promptForSafeImageUrl(message = "Nhập URL ảnh:"): string | null {
  const input = typeof window !== "undefined" ? window.prompt(message) : null;
  if (input === null) {
    // Nguoi dung bam Huy - khong lam gi them
    return null;
  }

  const trimmed = input.trim();
  if (!trimmed) {
    toast.error("Vui lòng nhập URL ảnh");
    return null;
  }

  try {
    const parsed = new URL(trimmed);
    if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
      toast.error("URL ảnh không hợp lệ. Chỉ chấp nhận đường dẫn bắt đầu bằng http:// hoặc https://");
      return null;
    }
    return parsed.toString();
  } catch {
    toast.error("URL ảnh không hợp lệ. Chỉ chấp nhận đường dẫn bắt đầu bằng http:// hoặc https://");
    return null;
  }
}
