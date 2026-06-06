"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { authService } from "@/services/authService";
import { getApiErrorMessage } from "@/services/error";
import { PatientRegisterPayload, UserRole } from "@/services/types";
import toast from "react-hot-toast";

export function useAuth() {
  const router = useRouter();
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [role, setRole] = useState<UserRole | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [displayName, setDisplayName] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const checkAuth = async () => {
      const authStatus = await authService.ensureValidSession();
      setIsAuthenticated(authStatus);
      setRole(authStatus ? authService.getRole() : null);
      setUsername(authStatus ? authService.getUsername() : null);
      setDisplayName(authStatus ? authService.getDisplayName() : null);
      setIsLoading(false);
    };

    void checkAuth();
  }, []);

  const login = async (
    usernameInput: string,
    password: string,
    remember = false,
    allowedRoles?: UserRole[]
  ) => {
    const normalizedUsername = usernameInput.trim();

    if (normalizedUsername.length < 3) {
      const msg = "Tên đăng nhập phải có ít nhất 3 ký tự.";
      toast.error(msg);
      return { success: false, error: msg };
    }

    if (password.length < 6) {
      const msg = "Mật khẩu phải có ít nhất 6 ký tự.";
      toast.error(msg);
      return { success: false, error: msg };
    }

    try {
      const response = await authService.login(normalizedUsername, password);
      if (response.requiresTwoFactor && response.mfaChallengeToken) {
        toast.success("Nhập mã xác thực từ ứng dụng MFA để hoàn tất đăng nhập.");
        return {
          success: true,
          requiresTwoFactor: true,
          challengeToken: response.mfaChallengeToken,
          challengeExpiresAtUtc: response.mfaChallengeExpiresAtUtc ?? null,
        };
      }

      const nextRole = authService.getRole();
      if (allowedRoles?.length && (!nextRole || !allowedRoles.includes(nextRole))) {
        await authService.logout();
        setIsAuthenticated(false);
        setRole(null);
        setUsername(null);
        setDisplayName(null);
        localStorage.removeItem("emr_remember_me");
        const msg = "Tài khoản không thuộc cổng đăng nhập đã chọn.";
        toast.error(msg);
        return { success: false, error: msg };
      }

      const nextUsername = authService.getUsername();
      const nextDisplayName = authService.getDisplayName();
      setIsAuthenticated(true);
      setRole(nextRole);
      setUsername(nextUsername);
      setDisplayName(nextDisplayName);
      toast.success("Đăng nhập thành công.");

      if (remember) {
        localStorage.setItem("emr_remember_me", "true");
      } else {
        localStorage.removeItem("emr_remember_me");
      }

      router.push(nextRole === "Patient" ? "/portal" : "/dashboard");
      return { success: true };
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, "Tên đăng nhập hoặc mật khẩu không đúng.");
      toast.error(msg);
      return { success: false, error: msg };
    }
  };

  const verifyMfaLogin = async (
    challengeToken: string,
    code: string,
    remember = false,
    allowedRoles?: UserRole[]
  ) => {
    try {
      await authService.verifyMfaLogin({ mfaChallengeToken: challengeToken, code });
      const nextRole = authService.getRole();
      if (allowedRoles?.length && (!nextRole || !allowedRoles.includes(nextRole))) {
        await authService.logout();
        setIsAuthenticated(false);
        setRole(null);
        setUsername(null);
        setDisplayName(null);
        localStorage.removeItem("emr_remember_me");
        const msg = "Tài khoản không thuộc cổng đăng nhập đã chọn.";
        toast.error(msg);
        return { success: false, error: msg };
      }

      const nextUsername = authService.getUsername();
      const nextDisplayName = authService.getDisplayName();
      setIsAuthenticated(true);
      setRole(nextRole);
      setUsername(nextUsername);
      setDisplayName(nextDisplayName);
      toast.success("Đăng nhập xác thực hai bước thành công.");

      if (remember) {
        localStorage.setItem("emr_remember_me", "true");
      } else {
        localStorage.removeItem("emr_remember_me");
      }

      router.push(nextRole === "Patient" ? "/portal" : "/dashboard");
      return { success: true };
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, "Mã xác thực không hợp lệ.");
      toast.error(msg);
      return { success: false, error: msg };
    }
  };

  const registerPatient = async (payload: PatientRegisterPayload) => {
    try {
      await authService.registerPatient(payload);
      const nextRole = authService.getRole();
      const nextUsername = authService.getUsername();
      const nextDisplayName = authService.getDisplayName();
      setIsAuthenticated(true);
      setRole(nextRole);
      setUsername(nextUsername);
      setDisplayName(nextDisplayName);
      toast.success("Đã tạo tài khoản bệnh nhân.");
      router.push("/portal");
      return { success: true };
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, "Không thể tạo tài khoản bệnh nhân.");
      toast.error(msg);
      return { success: false, error: msg };
    }
  };

  const logout = async () => {
    await authService.logout();
    setIsAuthenticated(false);
    setRole(null);
    setUsername(null);
    setDisplayName(null);
    toast.success("Đã đăng xuất.");
    router.push("/login");
  };

  return {
    isAuthenticated,
    isLoading,
    role,
    username,
    displayName,
    login,
    verifyMfaLogin,
    registerPatient,
    logout,
  };
}
