const API_BASE_URL =
  (typeof process !== "undefined" ? process.env?.API_BASE_URL : undefined) ??
  import.meta.env.VITE_API_BASE_URL ??
  "/api";

export const buildApiUrl = (path: string) =>
  `${API_BASE_URL.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;
