import React from "react";
import { describe, it, expect, vi } from "vitest";
import { render, screen, fireEvent } from "@testing-library/react";
import { TablePagination } from "@/shared/ui/Table/TablePagination";

describe("TablePagination Component", () => {
  it("renders nothing when totalItems is 0 and totalPages is 1", () => {
    const { container } = render(
      <TablePagination
        currentPage={1}
        totalPages={1}
        totalItems={0}
        pageSize={10}
        onPageChange={vi.fn()}
      />
    );
    expect(container.firstChild).toBeNull();
  });

  it("renders item range summary and total count correctly", () => {
    render(
      <TablePagination
        currentPage={2}
        totalPages={5}
        totalItems={45}
        pageSize={10}
        onPageChange={vi.fn()}
        itemName="bệnh nhân"
      />
    );

    expect(screen.getByText("11")).toBeInTheDocument();
    expect(screen.getByText("20")).toBeInTheDocument();
    expect(screen.getByText("45")).toBeInTheDocument();
    expect(screen.getByText(/bệnh nhân/)).toBeInTheDocument();
    expect(screen.getByText("2 / 5")).toBeInTheDocument();
  });

  it("handles previous and next page clicks", () => {
    const handlePageChange = vi.fn();
    render(
      <TablePagination
        currentPage={2}
        totalPages={5}
        totalItems={45}
        pageSize={10}
        onPageChange={handlePageChange}
      />
    );

    fireEvent.click(screen.getByRole("button", { name: "Trang trước" }));
    expect(handlePageChange).toHaveBeenCalledWith(1);

    fireEvent.click(screen.getByRole("button", { name: "Trang tiếp theo" }));
    expect(handlePageChange).toHaveBeenCalledWith(3);
  });

  it("disables previous button on the first page", () => {
    render(
      <TablePagination
        currentPage={1}
        totalPages={5}
        totalItems={50}
        pageSize={10}
        onPageChange={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: "Trang trước" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Đến trang đầu tiên" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Trang tiếp theo" })).not.toBeDisabled();
  });

  it("disables next button on the last page", () => {
    render(
      <TablePagination
        currentPage={5}
        totalPages={5}
        totalItems={50}
        pageSize={10}
        onPageChange={vi.fn()}
      />
    );

    expect(screen.getByRole("button", { name: "Trang tiếp theo" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Đến trang cuối cùng" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Trang trước" })).not.toBeDisabled();
  });

  it("triggers onPageSizeChange when page size selector changes", () => {
    const handlePageSizeChange = vi.fn();
    render(
      <TablePagination
        currentPage={1}
        totalPages={5}
        totalItems={50}
        pageSize={10}
        onPageChange={vi.fn()}
        onPageSizeChange={handlePageSizeChange}
      />
    );

    const select = screen.getByLabelText("Số dòng trên mỗi trang");
    fireEvent.change(select, { target: { value: "20" } });
    expect(handlePageSizeChange).toHaveBeenCalledWith(20);
  });
});
