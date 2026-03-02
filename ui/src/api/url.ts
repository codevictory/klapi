const API_BASE_URL = process.env.API_BASE_URL ?? "http://localhost:5013";

export const buildApiUrl = (path: string) =>
  `${API_BASE_URL.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;
