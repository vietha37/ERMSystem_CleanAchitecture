import React from "react";

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

export function Modal({ isOpen, onClose, title, children }: ModalProps) {
  if (!isOpen) return null;

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-slate-950/42 px-4 py-6 backdrop-blur-md"
      onClick={onClose}
    >
      <div
        className="max-h-[88vh] w-full max-w-2xl overflow-hidden rounded-[1.5rem] border border-white/75 bg-white shadow-[0_28px_90px_rgba(15,23,42,0.28)] md:rounded-[2rem]"
        onClick={(event) => event.stopPropagation()}
      >
        <div className="flex items-center justify-between border-b border-slate-100 bg-slate-50/80 px-4 py-4 md:px-6 md:py-5">
          <div>
            <p className="text-[11px] font-bold uppercase tracking-[0.28em] text-cyan-700">
              Workspace dialog
            </p>
            <h3 className="mt-2 text-xl font-semibold tracking-tight text-slate-950">
              {title}
            </h3>
          </div>
          <button
            onClick={onClose}
            className="flex h-10 w-10 items-center justify-center rounded-2xl border border-slate-200 bg-white text-slate-400 transition hover:text-slate-700"
          >
            ×
          </button>
        </div>
        <div className="max-h-[calc(88vh-104px)] overflow-y-auto px-4 py-4 md:px-6 md:py-5">{children}</div>
      </div>
    </div>
  );
}
