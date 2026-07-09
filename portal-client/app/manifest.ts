import type { MetadataRoute } from "next";

export default function manifest(): MetadataRoute.Manifest {
  return {
    name: "diaB — Cổng bệnh nhân",
    short_name: "diaB",
    description: "Cổng thông tin bệnh nhân phòng khám diaB",
    start_url: "/",
    display: "standalone",
    background_color: "#01645A",
    theme_color: "#01645A",
    lang: "vi",
    icons: [
      { src: "/icons/icon-192.png", sizes: "192x192", type: "image/png", purpose: "any" },
      { src: "/icons/icon-512.png", sizes: "512x512", type: "image/png", purpose: "any" },
      { src: "/icons/icon-512.png", sizes: "512x512", type: "image/png", purpose: "maskable" },
    ],
  };
}
