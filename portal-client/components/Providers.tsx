"use client";

import { MutationCache, QueryCache, QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { ApiRequestError } from "@/lib/api";
import { clearTokenCookie } from "@/lib/auth";

function makeQueryClient(onUnauthorized: () => void) {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: 1,
        staleTime: 15 * 1000,
        refetchOnWindowFocus: false,
      },
    },
    queryCache: new QueryCache({
      onError(error) {
        if (error instanceof ApiRequestError && error.status === 401) {
          onUnauthorized();
        }
      },
    }),
    mutationCache: new MutationCache({
      onError(error) {
        if (error instanceof ApiRequestError && error.status === 401) {
          onUnauthorized();
        }
      },
    }),
  });
}

export function Providers({ children }: { children: React.ReactNode }) {
  const router = useRouter();

  const [queryClient] = useState(() =>
    makeQueryClient(() => {
      clearTokenCookie();
      router.push("/login?reason=session_expired");
    }),
  );

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
}
