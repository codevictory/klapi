import { query } from "@solidjs/router";

const API_BASE_URL = process.env.API_BASE_URL ?? "http://localhost:5013";

const buildUrl = (path: string) =>
  `${API_BASE_URL.replace(/\/+$/, "")}/${path.replace(/^\/+/, "")}`;

async function fetchApi<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(buildUrl(path), {
    ...init,
    headers: {
      "content-type": "application/json",
      ...(init?.headers ?? {}),
    },
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(`API ${response.status}: ${text || response.statusText}`);
  }

  return (await response.json()) as T;
}

export const queryApiVersion = query(async () => {
  "use server";
  const data = await fetchApi<{ version: string }>("/");
  return data.version;
}, "api-version");
