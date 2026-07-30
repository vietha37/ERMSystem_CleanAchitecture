import type { NextConfig } from "next";

const BACKEND_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5219/api";

const nextConfig: NextConfig = {
  reactCompiler: true,
  devIndicators: false,

  // Proxy all /api/* requests to the ASP.NET Core backend.
  // This eliminates CORS issues in production and keeps the URL configurable.
  async rewrites() {
    return [
      {
        source: "/api/:path*",
        destination: `${BACKEND_URL}/:path*`,
      },
    ];
  },
};

export default nextConfig;
