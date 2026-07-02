import axios, { AxiosError, InternalAxiosRequestConfig } from "axios";
import type { RefreshTokenResponse } from "./types";

const API_BASE_URL =
  process.env.NEXT_PUBLIC_API_BASE_URL || "http://localhost:5000";

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

// Request interceptor: inject access token
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
