"use client";

import React from "react";
import { AlertTriangle, RefreshCw } from "lucide-react";
import { Button } from "@/components/ui/button";

interface ErrorBoundaryState {
  hasError: boolean;
  error?: Error;
}

interface ErrorBoundaryProps {
  children: React.ReactNode;
  fallback?: React.ReactNode;
}

export class ErrorBoundary extends React.Component<
  ErrorBoundaryProps,
  ErrorBoundaryState
> {
  constructor(props: ErrorBoundaryProps) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(error: Error): ErrorBoundaryState {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: React.ErrorInfo) {
    console.error("[ErrorBoundary]", error, info.componentStack);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: undefined });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <div className="flex flex-col items-center justify-center min-h-[400px] px-6 text-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 mb-4">
            <AlertTriangle className="h-8 w-8 text-destructive" aria-hidden="true" />
          </div>
          <h2 className="text-lg font-semibold text-foreground mb-2">
            Đã có lỗi xảy ra
          </h2>
          <p className="text-sm text-muted-foreground mb-1 max-w-sm">
            Trang này gặp sự cố không mong muốn. Vui lòng thử tải lại.
          </p>
          {process.env.NODE_ENV === "development" && this.state.error && (
            <pre className="mt-3 mb-4 text-xs text-left bg-muted p-3 rounded-md overflow-auto max-w-md max-h-32 w-full">
              {this.state.error.message}
            </pre>
          )}
          <div className="flex gap-3 mt-4">
            <Button
              variant="outline"
              size="sm"
              onClick={this.handleReset}
              className="gap-2"
            >
              <RefreshCw className="h-4 w-4" />
              Thử lại
            </Button>
            <Button
              size="sm"
              onClick={() => window.location.reload()}
              className="gap-2"
            >
              Tải lại trang
            </Button>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}

/** Route-level error fallback for Next.js error.tsx */
export function RouteErrorFallback({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] px-6 text-center">
      <div className="flex h-16 w-16 items-center justify-center rounded-full bg-destructive/10 mb-4">
        <AlertTriangle className="h-8 w-8 text-destructive" aria-hidden="true" />
      </div>
      <h2 className="text-lg font-semibold text-foreground mb-2">
        Đã có lỗi xảy ra
      </h2>
      <p className="text-sm text-muted-foreground mb-4 max-w-sm">
        {error.message ?? "Trang này gặp sự cố không mong muốn."}
      </p>
      <Button onClick={reset} size="sm" className="gap-2">
        <RefreshCw className="h-4 w-4" />
        Thử lại
      </Button>
    </div>
  );
}
