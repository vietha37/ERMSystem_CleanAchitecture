"use client";

import React, { useRef, useEffect } from "react";
import { Modal } from "@/components/ui/Modal";
import { Button } from "@/components/ui/Button";

export interface ConfirmDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void | Promise<void>;
  title: string;
  message: string;
  confirmText?: string;
  cancelText?: string;
  variant?: "danger" | "warning" | "primary";
  isLoading?: boolean;
}

/**
 * Accessible confirmation dialog replacing browser-native window.confirm().
 */
export function ConfirmDialog({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = "Xác nhận",
  cancelText = "Hủy",
  variant = "danger",
  isLoading = false,
}: ConfirmDialogProps) {
  const cancelBtnRef = useRef<HTMLButtonElement>(null);

  // Auto focus cancel button when opened to prevent accidental Enter submit
  useEffect(() => {
    if (isOpen) {
      setTimeout(() => cancelBtnRef.current?.focus(), 50);
    }
  }, [isOpen]);

  const buttonVariant = variant === "danger" ? "danger" : "primary";

  return (
    <Modal
      isOpen={isOpen}
      onClose={isLoading ? () => {} : onClose}
      title={title}
      maxWidth="sm"
    >
      <div className="space-y-4">
        <p className="text-sm leading-relaxed text-slate-600">
          {message}
        </p>

        <div className="flex items-center justify-end gap-3 pt-3 border-t border-slate-100">
          <Button
            ref={cancelBtnRef}
            variant="secondary"
            size="sm"
            onClick={onClose}
            disabled={isLoading}
          >
            {cancelText}
          </Button>
          <Button
            variant={buttonVariant}
            size="sm"
            onClick={onConfirm}
            isLoading={isLoading}
          >
            {confirmText}
          </Button>
        </div>
      </div>
    </Modal>
  );
}
