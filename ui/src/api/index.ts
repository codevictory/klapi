import { action, query } from "@solidjs/router";

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

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const queryApiVersion = query(async () => {
  "use server";
  const data = await fetchApi<{ version: string }>("/");
  return data.version;
}, "api-version");

export type LokOpenHours = {
  id: number;
  name: string;
  version: string;
  paragraph1: string;
  paragraph2: string;
  paragraph3: string;
  paragraph4: string;
  kitchenNotice: string;
};

export const queryLokOpenHours = query(async (_refreshKey = 0) => {
  "use server";
  return await fetchApi<LokOpenHours[]>("/lok/open-hours");
}, "lok-open-hours");

export const createLokOpenHours = action(async (formData: FormData) => {
  "use server";
  const name = String(formData.get("name") ?? "").trim();

  if (!name) {
    throw new Error("Open hours version name is required.");
  }

  const payload = {
    id: 0,
    name,
    version: new Date().toISOString(),
    paragraph1: String(formData.get("paragraph1") ?? ""),
    paragraph2: String(formData.get("paragraph2") ?? ""),
    paragraph3: String(formData.get("paragraph3") ?? ""),
    paragraph4: String(formData.get("paragraph4") ?? ""),
    kitchenNotice: String(formData.get("kitchenNotice") ?? ""),
  } satisfies LokOpenHours;

  return await fetchApi<LokOpenHours>("/lok/open-hours", {
    method: "POST",
    body: JSON.stringify(payload),
  });
});

export const deleteLokOpenHours = action(async (formData: FormData) => {
  "use server";
  const idValue = String(formData.get("id") ?? "").trim();
  const id = Number(idValue);

  if (!Number.isFinite(id) || id <= 0) {
    throw new Error("Open hours id is required for delete.");
  }

  await fetchApi<void>(`/lok/open-hours/${id}`, {
    method: "DELETE",
  });

  return { deleted: true };
});
