"use client";

import { useEffect, useRef } from "react";

interface ReportBarcodeProps {
  code: string;
}

/**
 * Render barcode CODE128 từ bwip-js vào container div.
 * Height ~10mm (height: 10 trong đơn vị bwip-js), có text phía dưới.
 */
export function ReportBarcode({ code }: ReportBarcodeProps) {
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!containerRef.current || !code) return;

    let cancelled = false;

    import("bwip-js/browser").then((bwipjs) => {
      if (cancelled || !containerRef.current) return;
      try {
        const svgString = bwipjs.toSVG({
          bcid: "code128",
          text: code,
          scale: 2,
          height: 10,
          includetext: true,
          textxalign: "center",
        });
        // Fix #9: check lại sau khi tính SVG, tránh ghi vào DOM đã unmount
        if (cancelled || !containerRef.current) return;
        containerRef.current.innerHTML = svgString;
      } catch (err) {
        console.error("Lỗi render barcode:", err);
        if (cancelled || !containerRef.current) return;
        containerRef.current.innerHTML = `<span class="text-xs text-gray-500 font-mono">${code}</span>`;
      }
    });

    return () => {
      cancelled = true;
    };
  }, [code]);

  return (
    <div
      ref={containerRef}
      className="flex justify-center items-center py-2"
      aria-label={`Mã báo cáo: ${code}`}
      role="img"
    />
  );
}
