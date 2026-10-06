import axios from "axios";

export interface BackendErrorResponse {
  errorCode?: string;
  message?: string;
  correlationId?: string;
  details?: unknown;
  errors?: unknown;
  title?: string;
}

export function getApiErrorMessage(
  error: unknown,
  fallback = "Đã xảy ra lỗi khi kết nối với máy chủ."
): string {
  if (!axios.isAxiosError(error)) {
    if (error instanceof Error && error.message) {
      return error.message;
    }
    return fallback;
  }

  const payload = error.response?.data as BackendErrorResponse | string | undefined;

  if (typeof payload === "string" && payload.trim().length > 0) {
    return payload;
  }

  if (payload && typeof payload === "object") {
    // 1. Primary message from custom Backend ApiErrorResponse
    if (typeof payload.message === "string" && payload.message.trim().length > 0) {
      return payload.message;
    }

    // 2. Validation details dictionary from ApiErrorResponseFactory (Details: Record<string, string[]>)
    if (payload.details && typeof payload.details === "object") {
      const detailMessages = Object.values(payload.details as Record<string, unknown[]>)
        .flat()
        .filter((entry): entry is string => typeof entry === "string" && entry.trim().length > 0);
      if (detailMessages.length > 0) {
        return detailMessages.join(" ");
      }
    }

    // 3. ASP.NET standard ProblemDetails errors dictionary (Errors: Record<string, string[]>)
    if (payload.errors && typeof payload.errors === "object") {
      const messages = Object.values(payload.errors as Record<string, unknown[]>)
        .flat()
        .filter((entry): entry is string => typeof entry === "string" && entry.trim().length > 0);
      if (messages.length > 0) {
        return messages.join(" ");
      }
    }

    // 4. Fallback to ProblemDetails title
    if (typeof payload.title === "string" && payload.title.trim().length > 0) {
      return payload.title;
    }
  }

  // 5. HTTP status code specific fallbacks
  if (error.response?.status === 401) {
    return "Phiên đăng nhập đã hết hạn hoặc không hợp lệ. Vui lòng đăng nhập lại.";
  }
  if (error.response?.status === 403) {
    return "Bạn không có quyền thực hiện thao tác này.";
  }
  if (error.response?.status === 404) {
    return "Không tìm thấy dữ liệu yêu cầu.";
  }
  if (error.response?.status === 409) {
    return "Xung đột dữ liệu hoặc bản ghi đã bị thay đổi bởi người dùng khác.";
  }
  if (error.response?.status === 500) {
    return "Lỗi máy chủ nội bộ. Vui lòng thử lại sau.";
  }

  return fallback;
}

export function getApiErrorCode(error: unknown): string | null {
  if (!axios.isAxiosError(error)) return null;
  const payload = error.response?.data as BackendErrorResponse | undefined;
  if (payload && typeof payload === "object" && typeof payload.errorCode === "string") {
    return payload.errorCode;
  }
  return null;
}

export function getApiValidationDetails(error: unknown): Record<string, string[]> | null {
  if (!axios.isAxiosError(error)) return null;
  const payload = error.response?.data as BackendErrorResponse | undefined;
  if (!payload || typeof payload !== "object") return null;

  const candidate = payload.details || payload.errors;
  if (candidate && typeof candidate === "object" && !Array.isArray(candidate)) {
    return candidate as Record<string, string[]>;
  }

  return null;
}
