import api from './api';
import {
  AuthResponse,
  MfaSetupResponse,
  MfaStatus,
  PatientRegisterPayload,
  UserRole,
  VerifyMfaLoginPayload,
  VerifyMfaPayload,
} from "./types";
import {
  clearAuthSession,
  getAccessToken as getStoredAccessToken,
  getRefreshToken as getStoredRefreshToken,
  setAuthSession,
} from "./authStorage";
const ROLE_CLAIM_URI = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
const NAME_CLAIM_URI = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";

type JwtPayload = {
  role?: unknown;
  unique_name?: string;
  name?: string;
  display_name?: string;
  sub?: string;
  [ROLE_CLAIM_URI]?: unknown;
  [NAME_CLAIM_URI]?: string;
  exp?: number;
};

const knownRoles: UserRole[] = ["Admin", "Doctor", "Cashier", "Patient"];

function parseJwtPayload(token: string): JwtPayload | null {
  const parts = token.split(".");
  if (parts.length !== 3) {
    return null;
  }

  try {
    const encoded = parts[1].replace(/-/g, "+").replace(/_/g, "/");
    const decoded = atob(encoded);
    return JSON.parse(decoded) as JwtPayload;
  } catch {
    return null;
  }
}

function normalizeRoleClaim(value: unknown): UserRole | null {
  const rawRole = Array.isArray(value) ? value[0] : value;
  if (typeof rawRole !== "string") {
    return null;
  }

  return knownRoles.find((role) => role === rawRole.trim()) ?? null;
}

export const authService = {
  login: async (username: string, password: string): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/login', { username, password });
    if (response.data?.requiresTwoFactor) {
      return response.data;
    }

    const token = response.data?.accessToken || response.data?.token;
    if (!token || !response.data?.refreshToken) {
      throw new Error('Authentication failed: no token received from server.');
    }
    setAuthSession(response.data);
    return response.data;
  },

  verifyMfaLogin: async (payload: VerifyMfaLoginPayload): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>('/auth/verify-mfa-login', payload);
    const token = response.data?.accessToken || response.data?.token;
    if (!token || !response.data?.refreshToken) {
      throw new Error('MFA verification failed: no token received from server.');
    }

    setAuthSession(response.data);
    return response.data;
  },

  registerPatient: async (payload: PatientRegisterPayload): Promise<AuthResponse> => {
    const response = await api.post<AuthResponse>("/auth/patient-register", payload);
    const token = response.data?.accessToken || response.data?.token;
    if (!token || !response.data?.refreshToken) {
      throw new Error("Patient registration failed: no token received from server.");
    }

    setAuthSession(response.data);
    return response.data;
  },

  logout: async (): Promise<void> => {
    const accessToken = getStoredAccessToken();
    const refreshToken = getStoredRefreshToken();

    try {
      if (accessToken && refreshToken) {
        await api.post('/auth/logout', { accessToken, refreshToken });
      }
    } finally {
      clearAuthSession();
    }
  },

  refreshTokens: async (): Promise<AuthResponse> => {
    const accessToken = getStoredAccessToken();
    const refreshToken = getStoredRefreshToken();

    if (!accessToken || !refreshToken) {
      throw new Error("No refresh session available.");
    }

    const response = await api.post<AuthResponse>("/auth/refresh", { accessToken, refreshToken });
    setAuthSession(response.data);
    return response.data;
  },

  getToken: (): string | null => {
    return getStoredAccessToken();
  },

  getRefreshToken: (): string | null => {
    return getStoredRefreshToken();
  },

  isAuthenticated: (): boolean => {
    return !!authService.getToken();
  },

  getRole: (): UserRole | null => {
    const token = authService.getToken();
    if (!token) {
      return null;
    }

    const payload = parseJwtPayload(token);
    if (!payload) {
      return null;
    }

    return normalizeRoleClaim(payload[ROLE_CLAIM_URI] ?? payload.role);
  },

  getUsername: (): string | null => {
    const token = authService.getToken();
    if (!token) {
      return null;
    }

    const payload = parseJwtPayload(token);
    if (!payload) {
      return null;
    }

    return payload.unique_name ?? payload[NAME_CLAIM_URI] ?? payload.name ?? null;
  },

  getDisplayName: (): string | null => {
    const token = authService.getToken();
    if (!token) {
      return null;
    }

    const payload = parseJwtPayload(token);
    if (!payload) {
      return null;
    }

    return payload.display_name ?? null;
  },

  isTokenExpired: (): boolean => {
    const token = authService.getToken();
    if (!token) {
      return true;
    }

    const payload = parseJwtPayload(token);
    if (!payload?.exp) {
      return false;
    }

    return Date.now() >= payload.exp * 1000;
  },

  ensureValidSession: async (): Promise<boolean> => {
    const token = authService.getToken();
    if (!token) {
      return false;
    }

    if (!authService.isTokenExpired()) {
      return true;
    }

    try {
      await authService.refreshTokens();
      return true;
    } catch {
      clearAuthSession();
      return false;
    }
  },

  getMfaStatus: async (): Promise<MfaStatus> => {
    const response = await api.get<MfaStatus>('/auth/mfa/status');
    return response.data;
  },

  setupMfa: async (): Promise<MfaSetupResponse> => {
    const response = await api.post<MfaSetupResponse>('/auth/mfa/setup');
    return response.data;
  },

  enableMfa: async (payload: VerifyMfaPayload): Promise<MfaStatus> => {
    const response = await api.post<MfaStatus>('/auth/mfa/enable', payload);
    return response.data;
  },

  disableMfa: async (payload: VerifyMfaPayload): Promise<MfaStatus> => {
    const response = await api.post<MfaStatus>('/auth/mfa/disable', payload);
    return response.data;
  },
};
