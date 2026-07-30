"use client";

import React, { createContext, useContext, useEffect, useRef, useState } from "react";
import { authService } from "@/services/authService";
import { UserRole } from "@/services/types";

type AuthContextValue = {
  /** true khi session đã được verify xong (dù valid hay không) */
  isReady: boolean;
  isAuthenticated: boolean;
  role: UserRole | null;
  username: string | null;
  displayName: string | null;
  /** Refresh lại auth state từ storage (dùng sau login/logout) */
  refreshAuthState: () => void;
};

const AuthContext = createContext<AuthContextValue>({
  isReady: false,
  isAuthenticated: false,
  role: null,
  username: null,
  displayName: null,
  refreshAuthState: () => {},
});

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [isReady, setIsReady] = useState(false);
  const [isAuthenticated, setIsAuthenticated] = useState(false);
  const [role, setRole] = useState<UserRole | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState<string | null>(null);
  const initRef = useRef(false);

  const syncFromStorage = () => {
    const token = authService.getToken();
    if (!token) {
      setIsAuthenticated(false);
      setRole(null);
      setUsername(null);
      setDisplayName(null);
      return;
    }
    setIsAuthenticated(true);
    setRole(authService.getRole());
    setUsername(authService.getUsername());
    setDisplayName(authService.getDisplayName());
  };

  useEffect(() => {
    if (initRef.current) return;
    initRef.current = true;

    const init = async () => {
      // Đảm bảo token hợp lệ (refresh nếu cần) trước khi cho phép render
      await authService.ensureValidSession();
      syncFromStorage();
      setIsReady(true);
    };

    void init();
  }, []);

  const refreshAuthState = () => {
    syncFromStorage();
  };

  return (
    <AuthContext.Provider
      value={{ isReady, isAuthenticated, role, username, displayName, refreshAuthState }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuthContext() {
  return useContext(AuthContext);
}
