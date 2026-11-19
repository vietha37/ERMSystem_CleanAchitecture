import { describe, it, expect, vi, beforeEach, afterEach } from "vitest";
import { renderHook, act } from "@testing-library/react";
import { useDebounce } from "@/shared/hooks/useDebounce";

describe("useDebounce Hook", () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns initial value immediately", () => {
    const { result } = renderHook(() => useDebounce("hello", 300));
    expect(result.current).toBe("hello");
  });

  it("does not update value before delay elapses", () => {
    const { result, rerender } = renderHook(
      ({ val, delay }) => useDebounce(val, delay),
      { initialProps: { val: "initial", delay: 300 } }
    );

    rerender({ val: "changed", delay: 300 });

    act(() => {
      vi.advanceTimersByTime(200);
    });

    expect(result.current).toBe("initial");
  });

  it("updates value after delay elapses", () => {
    const { result, rerender } = renderHook(
      ({ val, delay }) => useDebounce(val, delay),
      { initialProps: { val: "initial", delay: 300 } }
    );

    rerender({ val: "updated", delay: 300 });

    act(() => {
      vi.advanceTimersByTime(350);
    });

    expect(result.current).toBe("updated");
  });
});
