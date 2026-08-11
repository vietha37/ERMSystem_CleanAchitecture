import { AuthResponse } from "./types";

const ACCESS_TOKEN_KEY = "emr_auth_token";
const REFRESH_TOKEN_KEY = "emr_refresh_token";

function getStorage(): Storage | null {
  if (typeof window === "undefined") {
    return null;
  }
  // Sử dụng sessionStorage thay cho localStorage
  // Giúp mỗi TAB trình duyệt có bộ nhớ phiên làm việc độc lập (Multi-role tab isolation)
  return window.sessionStorage;
}

export function getAccessToken(): string | null {
  return getStorage()?.getItem(ACCESS_TOKEN_KEY) ?? null;
}

export function getRefreshToken(): string | null {
  return getStorage()?.getItem(REFRESH_TOKEN_KEY) ?? null;
}

export function setAuthSession(auth: AuthResponse) {
  const storage = getStorage();
  if (!storage) {
    return;
  }

  const accessToken = auth.accessToken || auth.token;
  if (!accessToken || !auth.refreshToken) {
    throw new Error("Authentication response is missing token data.");
  }

  storage.setItem(ACCESS_TOKEN_KEY, accessToken);
  storage.setItem(REFRESH_TOKEN_KEY, auth.refreshToken);
}

export function clearAuthSession() {
  const storage = getStorage();
  if (!storage) {
    return;
  }

  storage.removeItem(ACCESS_TOKEN_KEY);
  storage.removeItem(REFRESH_TOKEN_KEY);
}
