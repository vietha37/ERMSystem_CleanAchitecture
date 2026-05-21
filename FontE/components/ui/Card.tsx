import React from "react";

export function Card({
  children,
  className = "",
}: {
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div
      className={`rounded-[1.8rem] border border-white/80 bg-white/88 p-4 shadow-[0_18px_40px_rgba(15,23,42,0.06)] backdrop-blur-sm transition-all duration-300 hover:shadow-[0_20px_46px_rgba(15,23,42,0.08)] ${className}`}
    >
      {children}
    </div>
  );
}
