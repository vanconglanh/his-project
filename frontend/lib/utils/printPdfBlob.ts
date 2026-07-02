/**
 * Helper in PDF từ blob server-side (ADR-0001).
 * Nhận URL endpoint trả application/pdf,
 * fetch blob, nhúng vào <iframe> ẩn, trigger window.print().
 */
export async function printPdfBlob(url: string): Promise<void> {
  try {
    const resp = await fetch(url, { credentials: "include" });
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
