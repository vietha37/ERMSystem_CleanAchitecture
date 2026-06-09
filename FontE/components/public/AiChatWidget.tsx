"use client";

import Link from "next/link";
import { FormEvent, KeyboardEvent, useEffect, useMemo, useRef, useState } from "react";
import {
  AiSymptomChatResponse,
  sendSymptomMessage,
} from "@/services/aiSymptomChatService";

type ChatMessage = {
  id: number;
  role: "assistant" | "user";
  content: string;
  analysis?: AiSymptomChatResponse;
};

const initialMessages: ChatMessage[] = [
  {
    id: 1,
    role: "assistant",
    content: "Bạn đang có triệu chứng gì?",
  },
];

const quickSymptoms = [
  "Sốt, ho, đau họng",
  "Đau bụng, buồn nôn",
  "Đau ngực, khó thở",
  "Đau đầu, chóng mặt",
];

function getUrgencyLabel(urgencyLevel?: string) {
  if (urgencyLevel === "emergency") {
    return "Cần chú ý khẩn cấp";
  }

  if (urgencyLevel === "routine") {
    return "Nên đặt lịch khám";
  }

  return "Cần thêm thông tin";
}

function getUrgencyClassName(urgencyLevel?: string) {
  if (urgencyLevel === "emergency") {
    return "border-red-200 bg-red-50 text-red-700";
  }

  if (urgencyLevel === "routine") {
    return "border-cyan-200 bg-cyan-50 text-cyan-700";
  }

  return "border-slate-200 bg-slate-50 text-slate-600";
}

export function AiChatWidget() {
  const [isOpen, setIsOpen] = useState(false);
  const [input, setInput] = useState("");
  const [messages, setMessages] = useState<ChatMessage[]>(initialMessages);
  const [isLoading, setIsLoading] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  const canSubmit = useMemo(() => input.trim().length > 0 && !isLoading, [input, isLoading]);

  useEffect(() => {
    if (isOpen) {
      messagesEndRef.current?.scrollIntoView({ behavior: "smooth", block: "end" });
    }
  }, [isOpen, messages, isLoading]);

  async function submitMessage(rawMessage: string) {
    const nextMessage = rawMessage.trim();
    if (!nextMessage || isLoading) {
      return;
    }

    const userMessage: ChatMessage = {
      id: Date.now(),
      role: "user",
      content: nextMessage,
    };

    setInput("");
    setMessages((current) => [...current, userMessage]);
    setIsLoading(true);

    try {
      const analysis = await sendSymptomMessage(nextMessage);
      setMessages((current) => [
        ...current,
        {
          id: Date.now() + 1,
          role: "assistant",
          content: analysis.answer,
          analysis,
        },
      ]);
    } catch {
      setMessages((current) => [
        ...current,
        {
          id: Date.now() + 1,
          role: "assistant",
          content:
            "Hệ thống AI đang tạm thời chưa sẵn sàng. Bạn vui lòng thử lại sau hoặc đặt lịch để được nhân viên y tế tư vấn trực tiếp.",
        },
      ]);
    } finally {
      setIsLoading(false);
    }
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    await submitMessage(input);
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void submitMessage(input);
    }
  }

  function resetConversation() {
    setMessages(initialMessages);
    setInput("");
  }

  return (
    <div className="fixed bottom-5 right-5 z-50 flex max-w-[calc(100vw-2.5rem)] flex-col items-end">
      {isOpen ? (
        <section
          className="mb-3 flex h-[min(600px,calc(100vh-7rem))] w-[400px] max-w-full flex-col overflow-hidden rounded-lg border border-slate-200 bg-white shadow-2xl"
          aria-label="Tư vấn triệu chứng AI"
        >
          <div className="flex items-center justify-between border-b border-slate-200 bg-slate-950 px-4 py-3 text-white">
            <div>
              <p className="text-sm font-semibold">Tư vấn triệu chứng AI</p>
              <p className="mt-0.5 text-xs text-slate-300">Sàng lọc tham khảo, không thay thế bác sĩ</p>
            </div>
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={resetConversation}
                className="flex h-9 items-center justify-center rounded-full border border-white/15 px-3 text-xs font-semibold text-white transition hover:bg-white/10"
              >
                Xóa
              </button>
              <button
                type="button"
                onClick={() => setIsOpen(false)}
                className="flex h-9 w-9 items-center justify-center rounded-full border border-white/15 text-lg leading-none text-white transition hover:bg-white/10"
                aria-label="Đóng chat AI"
              >
                ×
              </button>
            </div>
          </div>

          <div className="flex-1 space-y-3 overflow-y-auto bg-slate-50 p-4">
            {messages.map((message) => (
              <div
                key={message.id}
                className={`flex ${message.role === "user" ? "justify-end" : "justify-start"}`}
              >
                <div
                  className={`max-w-[88%] rounded-lg px-3 py-2 text-sm leading-6 ${
                    message.role === "user"
                      ? "bg-cyan-700 text-white"
                      : "border border-slate-200 bg-white text-slate-700"
                  }`}
                >
                  <div className="whitespace-pre-line">{message.content}</div>

                  {message.analysis ? (
                    <div className="mt-3 space-y-2 border-t border-slate-100 pt-3">
                      <div className="flex flex-wrap gap-1.5">
                        <span
                          className={`rounded-full border px-2.5 py-1 text-xs font-semibold ${getUrgencyClassName(
                            message.analysis.urgencyLevel
                          )}`}
                        >
                          {getUrgencyLabel(message.analysis.urgencyLevel)}
                        </span>
                        {!message.analysis.aiRuntimeAvailable ? (
                          <span className="rounded-full border border-amber-200 bg-amber-50 px-2.5 py-1 text-xs font-semibold text-amber-700">
                            Dùng tri thức nội bộ
                          </span>
                        ) : null}
                      </div>

                      {message.analysis.recommendedSpecialties.length > 0 ? (
                        <div className="flex flex-wrap gap-1.5">
                          {message.analysis.recommendedSpecialties.map((specialty) => (
                            <span
                              key={specialty}
                              className="rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-xs font-medium text-slate-700"
                            >
                              {specialty}
                            </span>
                          ))}
                        </div>
                      ) : null}
                    </div>
                  ) : null}
                </div>
              </div>
            ))}

            {isLoading ? (
              <div className="flex justify-start">
                <div className="rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm text-slate-500">
                  AI đang phân tích...
                </div>
              </div>
            ) : null}
            <div ref={messagesEndRef} />
          </div>

          <div className="border-t border-slate-200 bg-white p-3">
            <div className="mb-2 flex gap-2 overflow-x-auto pb-1">
              {quickSymptoms.map((symptom) => (
                <button
                  key={symptom}
                  type="button"
                  onClick={() => submitMessage(symptom)}
                  disabled={isLoading}
                  className="shrink-0 rounded-full border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-medium text-slate-700 transition hover:border-cyan-300 hover:text-cyan-700 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {symptom}
                </button>
              ))}
            </div>

            <form onSubmit={handleSubmit}>
              <textarea
                value={input}
                onChange={(event) => setInput(event.target.value)}
                onKeyDown={handleKeyDown}
                className="h-24 w-full resize-none rounded-md border border-slate-300 px-3 py-2 text-sm leading-6 text-slate-900 outline-none transition placeholder:text-slate-400 focus:border-cyan-600 focus:ring-2 focus:ring-cyan-100"
                maxLength={1200}
                placeholder="Ví dụ: Tôi bị sốt, ho, đau họng 2 ngày..."
              />
              <div className="mt-2 grid grid-cols-[1fr_auto] gap-2">
                <Link
                  href="/booking"
                  className="inline-flex h-10 items-center justify-center rounded-md border border-slate-300 px-3 text-sm font-semibold text-slate-700 transition hover:border-cyan-600 hover:text-cyan-700"
                >
                  Đặt lịch
                </Link>
                <button
                  type="submit"
                  disabled={!canSubmit}
                  className="inline-flex h-10 items-center justify-center rounded-md bg-cyan-700 px-4 text-sm font-semibold text-white transition hover:bg-cyan-800 disabled:cursor-not-allowed disabled:bg-slate-300"
                >
                  Gửi
                </button>
              </div>
            </form>
          </div>
        </section>
      ) : null}

      <button
        type="button"
        onClick={() => setIsOpen((current) => !current)}
        className="flex h-14 w-14 items-center justify-center rounded-full bg-cyan-700 text-sm font-bold text-white shadow-xl ring-1 ring-cyan-900/10 transition hover:-translate-y-0.5 hover:bg-cyan-800 focus:outline-none focus:ring-4 focus:ring-cyan-200"
        aria-label={isOpen ? "Đóng chat AI" : "Mở chat AI"}
      >
        AI
      </button>
    </div>
  );
}
