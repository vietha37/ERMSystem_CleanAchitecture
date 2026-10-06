import type { Metadata } from "next";
import { Toaster } from "react-hot-toast";
import { AuthProvider } from "@/contexts/AuthContext";
import { I18nProvider } from "@/contexts/I18nContext";
import "./globals.css";

export const metadata: Metadata = {
  title: "ERM Private Hospital",
  description: "Nền tảng bệnh viện tư với khám đa khoa, xét nghiệm, chẩn đoán và chăm sóc gia đình.",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html lang="vi">
      <body className="bg-[var(--background)] text-[var(--foreground)] antialiased" suppressHydrationWarning>
        <I18nProvider>
          <AuthProvider>
            {children}
          </AuthProvider>
        </I18nProvider>
        <Toaster position="top-right" />
      </body>
    </html>
  );
}
