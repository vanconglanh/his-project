import { getTokenFromClientCookie } from "@/lib/auth";
import { getDevTenantHeaders } from "@/lib/tenant";
import type { ApiErrorEnvelope } from "@/lib/types";

export const API_BASE =
  process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5000";

export const PORTAL_API_PREFIX = "/api/portal/v1";

/** Lỗi API có code + message tiếng Việt để hiển thị trực tiếp cho người dùng */
export class ApiRequestError extends Error {
  code: string;
  status: number;

  constructor(code: string, message: string, status: number) {
    super(message);
    this.name = "ApiRequestError";
    this.code = code;
    this.status = status;
  }
}

interface RequestOptions extends Omit<RequestInit, "body"> {
  body?: unknown;
  auth?: boolean;
}

async function request<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const { body, auth = true, headers, ...rest } = options;

  const finalHeaders: Record<string, string> = {
    Accept: "application/json",
    ...getDevTenantHeaders(),
    ...(headers as Record<string, string> | undefined),
  };

  if (body !== undefined && !(body instanceof FormData)) {
    finalHeaders["Content-Type"] = "application/json";
  }

  if (auth) {
    const token = getTokenFromClientCookie();
    if (token) {
      finalHeaders.Authorization = `Bearer ${token}`;
    }
  }

  const res = await fetch(`${API_BASE}${PORTAL_API_PREFIX}${path}`, {
    ...rest,
    headers: finalHeaders,
    body:
      body === undefined
        ? undefined
        : body instanceof FormData
          ? body
          : JSON.stringify(body),
  });

  if (res.status === 204) {
    return undefined as T;
  }

  const isJson = res.headers.get("content-type")?.includes("application/json");

  if (!res.ok) {
    if (isJson) {
      const envelope = (await res.json()) as ApiErrorEnvelope;
      throw new ApiRequestError(
        envelope.error?.code ?? "UNKNOWN_ERROR",
        envelope.error?.message ?? "Đã có lỗi xảy ra, vui lòng thử lại",
        res.status,
      );
    }
    throw new ApiRequestError("UNKNOWN_ERROR", "Đã có lỗi xảy ra, vui lòng thử lại", res.status);
  }

  if (!isJson) {
    return undefined as T;
  }

  const json = (await res.json()) as { data: T };
  return json.data;
}

export const api = {
  get: <T>(path: string, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "GET" }),
  post: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "POST", body }),
  put: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "PUT", body }),
  del: <T>(path: string, body?: unknown, options?: RequestOptions) =>
    request<T>(path, { ...options, method: "DELETE", body }),
};

/** Lấy file nhị phân (PDF) kèm token, trả về blob URL để mở/tải */
export async function fetchFileBlob(path: string): Promise<Blob> {
  const token = getTokenFromClientCookie();
  const res = await fetch(`${API_BASE}${PORTAL_API_PREFIX}${path}`, {
    headers: {
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...getDevTenantHeaders(),
    },
  });
  if (!res.ok) {
    throw new ApiRequestError("FILE_DOWNLOAD_FAILED", "Không tải được tệp, vui lòng thử lại", res.status);
  }
  return res.blob();
}
