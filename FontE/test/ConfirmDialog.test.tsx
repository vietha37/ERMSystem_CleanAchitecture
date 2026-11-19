import React from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { ConfirmDialog } from "@/shared/ui/Modal/ConfirmDialog";

describe("ConfirmDialog Component", () => {
  it("does not render dialog content when isOpen is false", () => {
    render(
      <ConfirmDialog
        isOpen={false}
        onClose={vi.fn()}
        onConfirm={vi.fn()}
        title="Xác nhận xóa"
        message="Bạn có chắc chắn muốn xóa không?"
      />
    );
    expect(screen.queryByText("Xác nhận xóa")).not.toBeInTheDocument();
  });

  it("renders title, message, confirm and cancel buttons when isOpen is true", () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={vi.fn()}
        onConfirm={vi.fn()}
        title="Xác nhận xóa bệnh nhân"
        message="Hành động này không thể hoàn tác."
        confirmText="Đồng ý xóa"
        cancelText="Bỏ qua"
      />
    );

    expect(screen.getByText("Xác nhận xóa bệnh nhân")).toBeInTheDocument();
    expect(screen.getByText("Hành động này không thể hoàn tác.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Đồng ý xóa" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Bỏ qua" })).toBeInTheDocument();
  });

  it("calls onClose when cancel button is clicked", () => {
    const handleClose = vi.fn();
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={handleClose}
        onConfirm={vi.fn()}
        title="Xác nhận"
        message="Nội dung"
        cancelText="Hủy bỏ"
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Hủy bỏ" }));
    expect(handleClose).toHaveBeenCalledTimes(1);
  });

  it("calls onConfirm when confirm button is clicked", () => {
    const handleConfirm = vi.fn();
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={vi.fn()}
        onConfirm={handleConfirm}
        title="Xác nhận"
        message="Nội dung"
        confirmText="Xác nhận"
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Xác nhận" }));
    expect(handleConfirm).toHaveBeenCalledTimes(1);
  });

  it("disables buttons when isLoading is true", () => {
    render(
      <ConfirmDialog
        isOpen={true}
        onClose={vi.fn()}
        onConfirm={vi.fn()}
        title="Xác nhận"
        message="Nội dung"
        isLoading={true}
        confirmText="Đang thực hiện"
        cancelText="Hủy"
      />
    );

    expect(screen.getByRole("button", { name: "Hủy" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Đang thực hiện" })).toBeDisabled();
  });
});
