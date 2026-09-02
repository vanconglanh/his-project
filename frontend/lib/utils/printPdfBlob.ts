/**
 * BUG FIX (QC print-button audit 2026-09-02): API dùng JWT lưu trong
 * localStorage ("auth-store"), gắn vào request qua Axios interceptor
 * (xem lib/api/client.ts) — KHÔNG dùng cookie session. fetch() trần với
 * chỉ `credentials: "include"` không gửi header Authorization -> mọi
 * endpoint PDF trả 401. Đọc token trực tiếp từ localStorage tại đây,
 * tương tự cách apiClient interceptor làm, để gắn Bearer header.
 */
function getAuthHeader(): Record<string, string> {
  if (typeof window === "undefined") return {};
  try {
    const raw = localStorage.getItem("auth-store");
    if (!raw) return {};
    const parsed = JSON.parse(raw);
    const token = parsed?.state?.accessToken;
    return token ? { Authorization: `Bearer ${token}` } : {};
  } catch {
    return {};
  }
}

/**
 * Helper in PDF từ blob server-side (ADR-0001).
 * Nhận URL endpoint trả application/pdf,
 * fetch blob, nhúng vào <iframe> ẩn, trigger window.print().
 *
 * `method`: mặc định GET; truyền "POST" cho các endpoint backend khai báo
 * [HttpPost] (vd receipts/print) — trước đây luôn gọi GET nên trả 405.
 */
export async function printPdfBlob(url: string, method: "GET" | "POST" = "GET"): Promise<void> {
  try {
    const resp = await fetch(url, {
      method,
      credentials: "include",
      headers: getAuthHeader(),
    });
    if (!resp.ok) {
      throw new Error(`Lỗi tải PDF: ${resp.status}`);
    }
    const blob = await resp.blob();
    const objectUrl = URL.createObjectURL(blob);

    const iframe = document.createElement("iframe");
    iframe.style.position = "fixed";
    iframe.style.top = "-9999px";
    iframe.style.left = "-9999px";
    iframe.style.width = "1px";
    iframe.style.height = "1px";
    iframe.src = objectUrl;

    document.body.appendChild(iframe);

    iframe.onload = () => {
      try {
        iframe.contentWindow?.print();
      } finally {
        setTimeout(() => {
          document.body.removeChild(iframe);
          URL.revokeObjectURL(objectUrl);
        }, 3000);
      }
    };
  } catch (err) {
    // Fallback: mở tab mới để user in thủ công
    window.open(url, "_blank");
    console.warn("[printPdfBlob] Fallback to window.open:", err);
  }
}
