import React from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { Button } from "@/components/ui/Button";

describe("Button Component", () => {
  it("renders children text correctly", () => {
    render(<Button>Lưu dữ liệu</Button>);
    expect(screen.getByRole("button", { name: "Lưu dữ liệu" })).toBeInTheDocument();
  });

  it("handles click events", () => {
    const handleClick = vi.fn();
    render(<Button onClick={handleClick}>Bấm vào đây</Button>);
    fireEvent.click(screen.getByRole("button", { name: "Bấm vào đây" }));
    expect(handleClick).toHaveBeenCalledTimes(1);
  });

  it("is disabled when disabled prop is true", () => {
    const handleClick = vi.fn();
    render(<Button disabled onClick={handleClick}>Vô hiệu hóa</Button>);
    const button = screen.getByRole("button", { name: "Vô hiệu hóa" });
    expect(button).toBeDisabled();
    fireEvent.click(button);
    expect(handleClick).not.toHaveBeenCalled();
  });

  it("shows loading state and is disabled when isLoading is true", () => {
    render(<Button isLoading>Đang lưu...</Button>);
    const button = screen.getByRole("button", { name: "Đang lưu..." });
    expect(button).toBeDisabled();
    expect(button).toHaveAttribute("aria-busy", "true");
  });

  it("applies danger variant classes", () => {
    render(<Button variant="danger">Xóa bản ghi</Button>);
    const button = screen.getByRole("button", { name: "Xóa bản ghi" });
    expect(button.className).toContain("bg-rose-600");
  });
});
