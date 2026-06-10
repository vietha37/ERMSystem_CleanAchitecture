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

export async function sendSymptomMessage(message: string): Promise<AiSymptomChatResponse> {
  const response = await api.post<AiSymptomChatResponse>("/ai-symptom-chat", { message });
  return response.data;
}
