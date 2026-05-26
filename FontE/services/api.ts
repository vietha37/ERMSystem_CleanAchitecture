import axios from "axios";
import {
  clearAuthSession,
  getAccessToken,
  getRefreshToken,
  setAuthSession,
} from "./authStorage";

const baseURL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5219/api";

const api = axios.create({
  baseURL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

const refreshClient = axios.create({
  baseURL,
  timeout: 10000,
  headers: {
    "Content-Type": "application/json",
  },
});

let refreshPromise: Promise<string | null> | null = null;

function isAuthEndpoint(url?: string) {
  return !!url && (
    url.includes("/auth/login") ||
    url.includes("/auth/verify-mfa-login") ||
    url.includes("/auth/refresh") ||
    url.includes("/auth/logout") ||
    url.includes("/auth/patient-register")
  );
}

async function refreshAccessToken(): Promise<string | null> {
  const accessToken = getAccessToken();
  const refreshToken = getRefreshToken();

  if (!accessToken || !refreshToken) {
    clearAuthSession();
    return null;
  }

  try {
    const response = await refreshClient.post("/auth/refresh", { accessToken, refreshToken });
    setAuthSession(response.data);
    return response.data.accessToken || response.data.token || null;
  } catch {
    clearAuthSession();
    return null;
  }
}

api.interceptors.request.use(
  (config) => {
    const token = getAccessToken();
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => Promise.reject(error)
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config as typeof error.config & { _retry?: boolean };

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !isAuthEndpoint(originalRequest.url)
    ) {
      originalRequest._retry = true;

      refreshPromise ??= refreshAccessToken().finally(() => {
        refreshPromise = null;
      });

      const nextAccessToken = await refreshPromise;
      if (nextAccessToken) {
        originalRequest.headers = originalRequest.headers ?? {};
        originalRequest.headers.Authorization = `Bearer ${nextAccessToken}`;
        return api(originalRequest);
      }
    }

    if (typeof window !== "undefined" && !isAuthEndpoint(originalRequest?.url)) {
      clearAuthSession();
      window.location.href = "/login";
    }

    return Promise.reject(error);
  }
);

export default api;
