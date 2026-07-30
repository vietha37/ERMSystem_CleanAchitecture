"use client";

import Link from "next/link";
import {
  FormEvent,
  KeyboardEvent,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import {
  AiSymptomChatResponse,
  ChatTurn,
  sendSymptomMessage,
} from "@/services/aiSymptomChatService";

// ── Types ─────────────────────────────────────────────────────────────────────

type ChatMessage = {
  id: number;
  role: "assistant" | "user";
  content: string;
  analysis?: AiSymptomChatResponse;
  isError?: boolean;
};

// ── Constants ─────────────────────────────────────────────────────────────────

const INITIAL_MESSAGES: ChatMessage[] = [
  {
    id: 1,
    role: "assistant",
    content:
      "Xin chào! Tôi là trợ lý AI của **ERM Hospital**.\n\nBạn đang có triệu chứng gì? Hãy mô tả chi tiết để tôi có thể tư vấn phù hợp nhất.",
  },
];

const QUICK_SYMPTOMS = [
  "Sốt, ho, đau họng",
  "Đau bụng, buồn nôn",
  "Đau ngực, khó thở",
  "Đau đầu, chóng mặt",
  "Mất ngủ, lo âu",
];

// ── Urgency helpers ───────────────────────────────────────────────────────────

function getUrgencyConfig(urgencyLevel?: string) {
  if (urgencyLevel === "emergency") {
    return {
      label: "⚠️ Cần xử lý khẩn cấp",
      cls: "bg-red-50 border-red-200 text-red-700",
      dot: "bg-red-500",
    };
  }
  if (urgencyLevel === "routine") {
    return {
      label: "📅 Nên đặt lịch khám",
      cls: "bg-emerald-50 border-emerald-200 text-emerald-700",
      dot: "bg-emerald-500",
    };
  }
  return {
    label: "ℹ️ Cần thêm thông tin",
    cls: "bg-slate-50 border-slate-200 text-slate-600",
    dot: "bg-slate-400",
  };
}

// ── Simple markdown renderer (bold, newlines) ─────────────────────────────────

function RenderMarkdown({ text }: { text: string }) {
  const lines = text.split("\n");
  return (
    <div className="space-y-1">
      {lines.map((line, i) => {
        if (line.trim() === "") return <div key={i} className="h-1" />;

        // Bold **text**
        const parts = line.split(/\*\*(.*?)\*\*/g);
        return (
          <p key={i} className="leading-relaxed">
            {parts.map((part, j) =>
              j % 2 === 1 ? (
                <strong key={j} className="font-semibold text-slate-900">
                  {part}
                </strong>
              ) : (
                <span key={j}>{part}</span>
              ),
            )}
          </p>
        );
      })}
    </div>
  );
}

// ── Typing dots animation ─────────────────────────────────────────────────────

function TypingDots() {
  return (
    <div className="flex items-center gap-1 px-1 py-0.5">
      {[0, 1, 2].map((i) => (
        <span
          key={i}
          className="block h-2 w-2 animate-bounce rounded-full bg-cyan-400"
          style={{ animationDelay: `${i * 0.15}s` }}
        />
      ))}
    </div>
  );
}

// ── Main widget ───────────────────────────────────────────────────────────────

export function AiChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>(INITIAL_MESSAGES);
  const [isLoading, setIsLoading] = useState(false);
  const [hasUnread, setHasUnread] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);
  const textareaRef = useRef<HTMLTextAreaElement | null>(null);

  const canSubmit = useMemo(
    () => input.trim().length > 0 && !isLoading,
    [input, isLoading],
  );

  // Scroll to bottom on new messages
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
  }, [messages, isLoading]);

  // Auto-focus textarea when chat opens
  useEffect(() => {
    if (isOpen) {
      setHasUnread(false);
      setTimeout(() => textareaRef.current?.focus(), 100);
    }
  }, [isOpen]);

  // Auto-resize textarea
  useEffect(() => {
    const el = textareaRef.current;
    if (!el) return;
    el.style.height = "auto";
    el.style.height = `${Math.min(el.scrollHeight, 120)}px`;
  }, [input]);

  async function submitMessage(rawMessage: string) {
    const nextMessage = rawMessage.trim();
    if (!nextMessage || isLoading) return;

    const userMsg: ChatMessage = {
      id: Date.now(),
      role: "user",
      content: nextMessage,
    };

    setInput("");
    setMessages((prev) => [...prev, userMsg]);
    setIsLoading(true);

    // Build history from existing messages (exclude greeting, max 10 turns)
    const history: ChatTurn[] = messages
      .filter((m) => m.id !== 1) // exclude initial greeting
      .slice(-10)
      .map((m) => ({ role: m.role, content: m.content }));

    try {
      const analysis = await sendSymptomMessage(nextMessage, history);
      const assistantMsg: ChatMessage = {
        id: Date.now() + 1,
        role: "assistant",
        content: analysis.answer,
        analysis,
      };
      setMessages((prev) => [...prev, assistantMsg]);
      if (!isOpen) setHasUnread(true);
    } catch {
      setMessages((prev) => [
        ...prev,
        {
          id: Date.now() + 1,
          role: "assistant",
          content:
            "Rất tiếc, hệ thống AI đang tạm thời gián đoạn. Bạn có thể thử lại sau hoặc đặt lịch để được nhân viên y tế tư vấn trực tiếp.",
          isError: true,
        },
      ]);
    } finally {
      setIsLoading(false);
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    void submitMessage(input);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void submitMessage(input);
    }
  }

  function resetConversation() {
    setMessages(INITIAL_MESSAGES);
    setInput("");
    setTimeout(() => textareaRef.current?.focus(), 50);
  }

  const msgCount = messages.length - 1; // exclude greeting

  return (
    <div className="fixed bottom-5 right-5 z-50 flex flex-col items-end gap-3">
      {/* Chat panel */}
      {isOpen && (
        <section
          className="flex h-[min(640px,calc(100svh-7rem))] w-[420px] max-w-[calc(100vw-2.5rem)] flex-col overflow-hidden rounded-2xl border border-slate-200/80 bg-white shadow-[0_32px_80px_rgba(15,23,42,0.18)] ring-1 ring-black/5"
          aria-label="Tư vấn triệu chứng AI"
          style={{ animation: "slideUp 0.22s cubic-bezier(.22,1,.36,1)" }}
        >
          {/* Header */}
          <div className="flex items-center justify-between bg-gradient-to-r from-cyan-700 to-cyan-600 px-4 py-3 text-white">
            <div className="flex items-center gap-3">
              <div className="flex h-9 w-9 items-center justify-center rounded-full bg-white/15 text-base font-bold ring-2 ring-white/20">
                🩺
              </div>
              <div>
                <p className="text-sm font-semibold leading-tight">Trợ lý AI ERM Hospital</p>
                <div className="flex items-center gap-1.5 mt-0.5">
                  <span className="block h-1.5 w-1.5 animate-pulse rounded-full bg-emerald-400" />
                  <p className="text-xs text-cyan-100">Gemini AI · Sàng lọc tham khảo</p>
                </div>
              </div>
            </div>
            <div className="flex items-center gap-1.5">
              {msgCount > 0 && (
                <button
                  type="button"
                  onClick={resetConversation}
                  className="flex h-8 items-center gap-1.5 rounded-lg border border-white/20 px-2.5 text-xs font-medium text-white/80 transition hover:bg-white/15 hover:text-white"
                  title="Cuộc trò chuyện mới"
                >
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                    <path strokeLinecap="round" strokeLinejoin="round" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                  </svg>
                  Mới
                </button>
              )}
              <button
                type="button"
                onClick={() => setIsOpen(false)}
                className="flex h-8 w-8 items-center justify-center rounded-lg border border-white/20 text-white/80 transition hover:bg-white/15 hover:text-white"
                aria-label="Đóng"
              >
                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>
          </div>

          {/* Messages */}
          <div className="flex-1 space-y-4 overflow-y-auto overscroll-contain bg-slate-50 p-4 scroll-smooth">
            {messages.map((msg) => (
              <div
                key={msg.id}
                className={`flex gap-2 ${msg.role === "user" ? "flex-row-reverse" : "flex-row"}`}
                style={{ animation: "fadeIn 0.18s ease" }}
              >
                {/* Avatar */}
                {msg.role === "assistant" && (
                  <div className="mt-0.5 flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-cyan-100 text-sm">
                    🩺
                  </div>
                )}

                <div className={`flex max-w-[85%] flex-col gap-2 ${msg.role === "user" ? "items-end" : "items-start"}`}>
                  {/* Bubble */}
                  <div
                    className={`rounded-2xl px-3.5 py-2.5 text-sm ${
                      msg.role === "user"
                        ? "rounded-tr-sm bg-cyan-700 text-white shadow-sm"
                        : msg.isError
                          ? "rounded-tl-sm border border-red-100 bg-red-50 text-red-700 shadow-sm"
                          : "rounded-tl-sm border border-slate-200 bg-white text-slate-700 shadow-sm"
                    }`}
                  >
                    {msg.role === "user" ? (
                      <p className="leading-relaxed whitespace-pre-wrap">{msg.content}</p>
                    ) : (
                      <RenderMarkdown text={msg.content} />
                    )}
                  </div>

                  {/* Analysis card */}
                  {msg.analysis && (
                    <div className="w-full space-y-2">
                      {/* Urgency badge */}
                      {(() => {
                        const cfg = getUrgencyConfig(msg.analysis.urgencyLevel);
                        return (
                          <div className={`flex items-center gap-2 rounded-xl border px-3 py-1.5 text-xs font-semibold ${cfg.cls}`}>
                            <span className={`h-2 w-2 rounded-full ${cfg.dot}`} />
                            {cfg.label}
                            {!msg.analysis.aiRuntimeAvailable && (
                              <span className="ml-auto rounded-full bg-amber-100 px-2 py-0.5 text-amber-700 font-medium">
                                Tri thức nội bộ
                              </span>
                            )}
                          </div>
                        );
                      })()}

                      {/* Specialties */}
                      {msg.analysis.recommendedSpecialties.length > 0 && (
                        <div className="rounded-xl border border-slate-100 bg-white px-3 py-2">
                          <p className="mb-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400">
                            Chuyên khoa gợi ý
                          </p>
                          <div className="flex flex-wrap gap-1.5">
                            {msg.analysis.recommendedSpecialties.map((s) => (
                              <span
                                key={s}
                                className="rounded-full bg-cyan-50 px-2.5 py-0.5 text-xs font-medium text-cyan-700 ring-1 ring-cyan-100"
                              >
                                {s}
                              </span>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Matched conditions */}
                      {msg.analysis.matches.length > 0 && (
                        <div className="rounded-xl border border-slate-100 bg-white px-3 py-2">
                          <p className="mb-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400">
                            Khả năng liên quan
                          </p>
                          <div className="space-y-1.5">
                            {msg.analysis.matches.slice(0, 3).map((match) => (
                              <div key={match.title} className="rounded-lg bg-slate-50 px-2.5 py-1.5 text-xs">
                                <p className="font-semibold text-slate-800">{match.title}</p>
                                {match.matchedSymptoms.length > 0 && (
                                  <p className="mt-0.5 text-slate-500">
                                    Triệu chứng khớp: {match.matchedSymptoms.slice(0, 4).join(", ")}
                                  </p>
                                )}
                              </div>
                            ))}
                          </div>
                        </div>
                      )}

                      {/* Book CTA if emergency or routine */}
                      {msg.analysis.urgencyLevel !== "unknown" && (
                        <Link
                          href="/booking"
                          className="flex items-center justify-center gap-1.5 rounded-xl bg-cyan-700 py-2 text-xs font-semibold text-white transition hover:bg-cyan-800"
                        >
                          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
                          </svg>
                          Đặt lịch khám ngay
                        </Link>
                      )}
                    </div>
                  )}
                </div>
              </div>
            ))}

            {/* Typing indicator */}
            {isLoading && (
              <div className="flex items-end gap-2" style={{ animation: "fadeIn 0.18s ease" }}>
                <div className="flex h-7 w-7 shrink-0 items-center justify-center rounded-full bg-cyan-100 text-sm">
                  🩺
                </div>
                <div className="rounded-2xl rounded-tl-sm border border-slate-200 bg-white px-3.5 py-3 shadow-sm">
                  <TypingDots />
                </div>
              </div>
            )}

            <div ref={messagesEndRef} />
          </div>

          {/* Quick chips */}
          <div className="border-t border-slate-100 bg-white px-3 pt-2.5">
            <div className="flex gap-1.5 overflow-x-auto pb-2 scrollbar-none">
              {QUICK_SYMPTOMS.map((symptom) => (
                <button
                  key={symptom}
                  type="button"
                  onClick={() => void submitMessage(symptom)}
                  disabled={isLoading}
                  className="shrink-0 rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs font-medium text-slate-600 transition hover:border-cyan-300 hover:bg-cyan-50 hover:text-cyan-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {symptom}
                </button>
              ))}
            </div>
          </div>

          {/* Input area */}
          <div className="border-t border-slate-100 bg-white p-3">
            <form onSubmit={handleSubmit} className="flex items-end gap-2">
              <div className="relative flex-1">
                <textarea
                  ref={textareaRef}
                  value={input}
                  onChange={(e) => setInput(e.target.value)}
                  onKeyDown={handleKeyDown}
                  rows={1}
                  maxLength={1200}
                  placeholder="Mô tả triệu chứng... (Enter để gửi)"
                  className="w-full resize-none rounded-xl border border-slate-200 bg-slate-50 px-3.5 py-2.5 text-sm leading-relaxed text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-cyan-400 focus:bg-white focus:ring-4 focus:ring-cyan-50"
                  style={{ minHeight: 44, maxHeight: 120 }}
                  disabled={isLoading}
                />
                {input.length > 900 && (
                  <span className="absolute bottom-2 right-2.5 text-[10px] text-slate-400">
                    {input.length}/1200
                  </span>
                )}
              </div>
              <button
                type="submit"
                disabled={!canSubmit}
                className="flex h-11 w-11 shrink-0 items-center justify-center rounded-xl bg-cyan-700 text-white shadow-sm transition hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-200 disabled:text-slate-400"
                aria-label="Gửi"
              >
                <svg className="h-4.5 w-4.5 h-[18px] w-[18px]" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
                  <path strokeLinecap="round" strokeLinejoin="round" d="M6 12L3.269 3.126A59.768 59.768 0 0121.485 12 59.77 59.77 0 013.27 20.876L5.999 12zm0 0h7.5" />
                </svg>
              </button>
            </form>
            <p className="mt-1.5 text-center text-[10px] text-slate-400">
              Chỉ mang tính tham khảo · Không thay thế chẩn đoán bác sĩ
            </p>
          </div>
        </section>
      )}

      {/* FAB toggle button */}
      <button
        type="button"
        onClick={() => setIsOpen((prev) => !prev)}
        className={`group relative flex h-14 w-14 items-center justify-center rounded-full shadow-xl ring-1 ring-black/10 transition-all duration-200 ${
          isOpen
            ? "bg-slate-700 hover:bg-slate-800"
            : "bg-cyan-700 hover:bg-cyan-800 hover:-translate-y-0.5 hover:shadow-2xl"
        }`}
        aria-label={isOpen ? "Đóng trợ lý AI" : "Mở trợ lý AI"}
      >
        {isOpen ? (
          <svg className="h-5 w-5 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
          </svg>
        ) : (
          <span className="text-xl">🩺</span>
        )}

        {/* Unread badge */}
        {hasUnread && !isOpen && (
          <span className="absolute -right-0.5 -top-0.5 flex h-4 w-4 items-center justify-center rounded-full bg-red-500 text-[9px] font-bold text-white ring-2 ring-white">
            !
          </span>
        )}

        {/* Pulse ring when closed */}
        {!isOpen && (
          <span className="absolute inset-0 animate-ping rounded-full bg-cyan-400 opacity-20" />
        )}
      </button>

      {/* Keyframe styles */}
      <style>{`
        @keyframes slideUp {
          from { opacity: 0; transform: translateY(16px) scale(0.97); }
          to   { opacity: 1; transform: translateY(0)    scale(1);    }
        }
        @keyframes fadeIn {
          from { opacity: 0; transform: translateY(6px); }
          to   { opacity: 1; transform: translateY(0);   }
        }
        .scrollbar-none::-webkit-scrollbar { display: none; }
        .scrollbar-none { -ms-overflow-style: none; scrollbar-width: none; }
      `}</style>
    </div>
  );
}
