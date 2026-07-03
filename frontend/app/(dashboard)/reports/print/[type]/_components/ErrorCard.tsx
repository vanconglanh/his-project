"use client";

import { AlertTriangle, RefreshCw } from "lucide-react";

interface ErrorCardProps {
  title: string;
  description: string;
  /** Hành động khi bấm "Thử lại". Mặc định reload cả trang. */
  onRetry?: () => void;
}

export function ErrorCard({ title, description, onRetry }: ErrorCardProps) {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-100 px-4 print:hidden">
      <div className="max-w-md w-full bg-white rounded-lg shadow-md border border-gray-200 p-8 text-center">
        <div className="mx-auto mb-4 w-12 h-12 rounded-full bg-amber-100 flex items-center justify-center">
          <AlertTriangle className="w-6 h-6 text-amber-600" />
        </div>
        <h2 className="text-lg font-semibold text-gray-900 mb-2">{title}</h2>
        <p className="text-sm text-gray-600 mb-6">{description}</p>
        <button
          type="button"
          onClick={onRetry ?? (() => location.reload())}
          className="inline-flex items-center gap-2 px-4 py-2 rounded-md bg-teal-700 text-white text-sm font-medium hover:bg-teal-800 focus:outline-none focus:ring-2 focus:ring-teal-500 focus:ring-offset-2"
        >
          <RefreshCw className="w-4 h-4" />
          Thử lại
        </button>
      </div>
    </div>
  );
}
