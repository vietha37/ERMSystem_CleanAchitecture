import api from "./api";

export type AiSymptomKnowledgeMatch = {
  title: string;
  recommendedSpecialty: string;
  possibleConditions: string[];
  emergencySigns: string[];
  matchedSymptoms: string[];
  score: number;
};

export type AiSymptomChatResponse = {
  answer: string;
  recommendedSpecialties: string[];
  urgencyLevel: "emergency" | "routine" | "unknown" | string;
  aiRuntimeAvailable: boolean;
  matches: AiSymptomKnowledgeMatch[];
};

export type ChatTurn = {
  role: "user" | "assistant";
  content: string;
};

export async function sendSymptomMessage(
  message: string,
  history: ChatTurn[] = [],
): Promise<AiSymptomChatResponse> {
  const response = await api.post<AiSymptomChatResponse>("/ai-symptom-chat", {
    message,
    history,
  });
  return response.data;
}
