"use client";

import { useState, useEffect } from 'react';
import { useRouter } from 'next/navigation';
import { authService } from '@/services/authService';
import { getApiErrorMessage } from '@/services/error';
import { PatientRegisterPayload, UserRole } from '@/services/types';
import toast from 'react-hot-toast';

export function useAuth() {
  const router = useRouter();
  const [isAuthenticated, setIsAuthenticated] = useState<boolean>(false);
  const [role, setRole] = useState<UserRole | null>(null);
  const [username, setUsername] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  useEffect(() => {
    const checkAuth = async () => {
      const authStatus = await authService.ensureValidSession();
      setIsAuthenticated(authStatus);
      setRole(authStatus ? authService.getRole() : null);
      setUsername(authStatus ? authService.getUsername() : null);
      setIsLoading(false);
    };

    void checkAuth();
  }, []);

  const login = async (username: string, password: string, remember: boolean = false) => {
    try {
      const response = await authService.login(username, password);
      if (response.requiresTwoFactor && response.mfaChallengeToken) {
        toast.success('Nhập mã xác thực từ ứng dụng MFA để hoàn tất đăng nhập.');
        return {
          success: true,
          requiresTwoFactor: true,
          challengeToken: response.mfaChallengeToken,
          challengeExpiresAtUtc: response.mfaChallengeExpiresAtUtc ?? null,
        };
      }

      const nextRole = authService.getRole();
      const nextUsername = authService.getUsername();
      setIsAuthenticated(true);
      setRole(nextRole);
      setUsername(nextUsername);
      toast.success('Login successful!');
      
      // Additional remember me logic if needed
      if (remember) {
         localStorage.setItem('emr_remember_me', 'true');
      } else {
         localStorage.removeItem('emr_remember_me');
      }

      router.push(nextRole === 'Patient' ? '/portal' : '/dashboard');
      return { success: true };
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, 'Invalid username or password');
      toast.error(msg);
      return { success: false, error: msg };
    }
  };

  const verifyMfaLogin = async (challengeToken: string, code: string, remember: boolean = false) => {
    try {
      await authService.verifyMfaLogin({ mfaChallengeToken: challengeToken, code });
      const nextRole = authService.getRole();
      const nextUsername = authService.getUsername();
      setIsAuthenticated(true);
      setRole(nextRole);
      setUsername(nextUsername);
      toast.success('Đăng nhập xác thực hai bước thành công.');

      if (remember) {
         localStorage.setItem('emr_remember_me', 'true');
      } else {
         localStorage.removeItem('emr_remember_me');
      }

      router.push(nextRole === 'Patient' ? '/portal' : '/dashboard');
      return { success: true };
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, 'Mã xác thực không hợp lệ');
      toast.error(msg);
      return { success: false, error: msg };
    }
  };

  const registerPatient = async (payload: PatientRegisterPayload) => {
    try {
      await authService.registerPatient(payload);
      const nextRole = authService.getRole();
      const nextUsername = authService.getUsername();
      setIsAuthenticated(true);
      setRole(nextRole);
      setUsername(nextUsername);
      toast.success('Patient account created successfully');
      router.push('/portal');
      return { success: true };
    } catch (error: unknown) {
      const msg = getApiErrorMessage(error, 'Unable to create patient account');
      toast.error(msg);
      return { success: false, error: msg };
    }
  };

  const logout = async () => {
    await authService.logout();
    setIsAuthenticated(false);
    setRole(null);
    setUsername(null);
    toast.success('Logged out successfully');
    router.push('/login');
  };

  return {
    isAuthenticated,
    isLoading,
    role,
    username,
    login,
    verifyMfaLogin,
    registerPatient,
    logout,
  };
}
