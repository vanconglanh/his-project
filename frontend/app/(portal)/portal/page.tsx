"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";

export default function PortalRootPage() {
  const router = useRouter();

  useEffect(() => {
    const token = localStorage.getItem("portal-token");
    if (token) {
      router.replace("/portal/me");
    } else {
      router.replace("/portal/login");
    }
  }, [router]);

  return null;
}
