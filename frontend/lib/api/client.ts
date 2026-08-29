import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import type { RefreshTokenResponse } from "./types";

// Base URL cho API.
// - Neu NEXT_PUBLIC_API_BASE_URL duoc set tuong minh (khi non-empty) -> dung gia tri do.
// - Nguoc lai o PRODUCTION -> "" (relative same-origin): browser goi thang /api/v1 tren cung
//   domain dang phuc vu app (nginx proxy /api -> backend). Nho vay KHONG con phu thuoc build-arg
//   NEXT_PUBLIC_API_BASE_URL / APP_PUBLIC_URL, va KHONG bao gio roi ve localhost:5000 khi build
//   thieu bien (day la nguyen nhan su co login truoc day).
// - O DEV (next dev) -> "http://localhost:5000" de goi backend .NET chay local.
const _explicitApiBase = process.env.NEXT_PUBLIC_API_BASE_URL;
export const API_BASE_URL =
  _explicitApiBase && _explicitApiBase.length > 0
    ? _explicitApiBase
    : process.env.NODE_ENV === "production"
      ? ""
      : "http://localhost:5000";

export const apiClient = axios.create({
  baseURL: `${API_BASE_URL}/api/v1`,
  headers: {
    "Content-Type": "application/json",
  },
  timeout: 30000,
});

// Track refresh state to avoid parallel refresh calls
let isRefreshing = false;
let refreshQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

function processQueue(error: unknown, token: string | null = null) {
  refreshQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error);
    } else {
      resolve(token!);
    }
  });
  refreshQueue = [];
}

// Request interceptor: inject access token + chi nhánh đang làm việc
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Read token from store at request time (avoid circular import)
    if (typeof window !== "undefined") {
      const raw = localStorage.getItem("auth-store");
      if (raw) {
        try {
          const parsed = JSON.parse(raw);
          const token = parsed?.state?.accessToken;
          if (token) {
            config.headers.Authorization = `Bearer ${token}`;
          }
        } catch {
          // ignore parse error
        }
      }

      // Gắn header X-Branch-Id theo chi nhánh đang chọn (Zustand store persist
      // key "prodiab.activeBranchId" — xem lib/stores/branch-store.ts).
      // Đổi chi nhánh KHÔNG cần đăng nhập lại (quyết định Q10): chỉ cần đổi
      // header này, backend BranchScopeMiddleware tự set lại context mỗi request.
      const branchRaw = localStorage.getItem("prodiab.activeBranchId");
      if (branchRaw) {
        try {
          const parsedBranch = JSON.parse(branchRaw);
          const branchId = parsedBranch?.state?.activeBranchId;
          if (branchId !== null && branchId !== undefined) {
            config.headers["X-Branch-Id"] = String(branchId);
          }
        } catch {
          // ignore parse error
        }
      }
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Response interceptor: 401 → refresh → retry
apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & {
      _retry?: boolean;
    };

    if (error.response?.status !== 401 || originalRequest._retry) {
      return Promise.reject(error);
    }

    // Get refresh token
    let refreshToken: string | null = null;
    if (typeof window !== "undefined") {
      const raw = localStorage.getItem("auth-store");
      if (raw) {
        try {
          const parsed = JSON.parse(raw);
          refreshToken = parsed?.state?.refreshToken ?? null;
        } catch {
          // ignore
        }
      }
    }

    if (!refreshToken) {
      redirectToLogin();
      return Promise.reject(error);
    }

    if (isRefreshing) {
      return new Promise<string>((resolve, reject) => {
        refreshQueue.push({ resolve, reject });
      }).then((token) => {
        originalRequest.headers.Authorization = `Bearer ${token}`;
        return apiClient(originalRequest);
      });
    }

    originalRequest._retry = true;
    isRefreshing = true;

    try {
      const { data } = await axios.post<RefreshTokenResponse>(
        `${API_BASE_URL}/api/v1/auth/refresh`,
        { refreshToken }
      );

      const newToken = data.accessToken;

      // Update store
      if (typeof window !== "undefined") {
        const raw = localStorage.getItem("auth-store");
        if (raw) {
          try {
            const parsed = JSON.parse(raw);
            parsed.state.accessToken = newToken;
            parsed.state.refreshToken = data.refreshToken;
            localStorage.setItem("auth-store", JSON.stringify(parsed));
          } catch {
            // ignore
          }
        }
      }

      processQueue(null, newToken);
      originalRequest.headers.Authorization = `Bearer ${newToken}`;
      return apiClient(originalRequest);
    } catch (refreshError) {
      processQueue(refreshError, null);
      redirectToLogin();
      return Promise.reject(refreshError);
    } finally {
      isRefreshing = false;
    }
  }
);

function redirectToLogin() {
  if (typeof window !== "undefined") {
    localStorage.removeItem("auth-store");
    window.location.href = "/login";
  }
}

export default apiClient;
