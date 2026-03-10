import { buildApiUrl } from "./url";

type AuthTokenResponse = {
  accessToken: string;
  username: string;
  displayName: string;
  roles: string[];
  tokenType: string;
  expiresIn: number;
};

async function fetchApi<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(buildApiUrl(path), {
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

function getAccessToken() {
  if (typeof window === "undefined") {
    return "";
  }

  return localStorage.getItem("session-token") ?? "";
}

export type LokOpenHours = {
  id: number;
  name: string;
  isActive: boolean;
  version: string;
  paragraph1: string;
  paragraph2: string;
  paragraph3: string;
  paragraph4: string;
  kitchenNotice: string;
};

export type LokOpenHoursInput = {
  name: string;
  paragraph1: string;
  paragraph2: string;
  paragraph3: string;
  paragraph4: string;
  kitchenNotice: string;
};

export type User = {
  username: string;
  added: string;
  lastUpdated: string;
  displayName: string;
  roles: string[];
};

export type CreateUserInput = {
  username: string;
  password: string;
  displayName: string;
  roles: string[];
};

export type UpdateUserInput = {
  password?: string;
  displayName: string;
  roles: string[];
};

export async function queryApiVersion(): Promise<string> {
  const data = await fetchApi<{ version: string }>("/");
  return data.version;
}

export async function queryLokOpenHours(): Promise<LokOpenHours[]> {
  return await fetchApi<LokOpenHours[]>("/lok/open-hours");
}

export async function createLokOpenHours(
  input: LokOpenHoursInput,
): Promise<LokOpenHours> {
  const name = input.name.trim();

  if (!name) {
    throw new Error("Open hours version name is required.");
  }

  const payload = {
    id: 0,
    name,
    isActive: false,
    version: new Date().toISOString(),
    paragraph1: input.paragraph1,
    paragraph2: input.paragraph2,
    paragraph3: input.paragraph3,
    paragraph4: input.paragraph4,
    kitchenNotice: input.kitchenNotice,
  } satisfies LokOpenHours;

  return await fetchApi<LokOpenHours>("/lok/open-hours", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
    body: JSON.stringify(payload),
  });
}

export async function updateLokOpenHours(
  id: number,
  input: LokOpenHoursInput,
): Promise<LokOpenHours> {
  if (!Number.isFinite(id) || id <= 0) {
    throw new Error("Open hours id is required for update.");
  }

  const name = input.name.trim();

  if (!name) {
    throw new Error("Open hours version name is required.");
  }

  const payload = {
    id,
    name,
    isActive: false,
    version: new Date().toISOString(),
    paragraph1: input.paragraph1,
    paragraph2: input.paragraph2,
    paragraph3: input.paragraph3,
    paragraph4: input.paragraph4,
    kitchenNotice: input.kitchenNotice,
  } satisfies LokOpenHours;

  return await fetchApi<LokOpenHours>(`/lok/open-hours/${id}`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
    body: JSON.stringify(payload),
  });
}

export async function deleteLokOpenHours(id: number): Promise<void> {
  if (!Number.isFinite(id) || id <= 0) {
    throw new Error("Open hours id is required for delete.");
  }

  await fetchApi<void>(`/lok/open-hours/${id}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
  });
}

export async function setActiveLokOpenHours(
  id: number,
): Promise<{ id: number; isActive: boolean }> {
  if (!Number.isFinite(id) || id <= 0) {
    throw new Error("Open hours id is required for setting active version.");
  }

  return await fetchApi<{ id: number; isActive: boolean }>(
    `/lok/open-hours/${id}/active`,
    {
      method: "PUT",
      headers: {
        Authorization: `Bearer ${getAccessToken()}`,
      },
    },
  );
}

export async function requestAuthToken(
  username: string,
  password: string,
): Promise<AuthTokenResponse> {
  return await fetchApi<AuthTokenResponse>("/auth/token", {
    method: "POST",
    body: JSON.stringify({
      username,
      password,
    }),
  });
}

export async function queryUsers(): Promise<User[]> {
  return await fetchApi<User[]>("/users", {
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
  });
}

export async function createUser(input: CreateUserInput): Promise<User> {
  return await fetchApi<User>("/users", {
    method: "POST",
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
    body: JSON.stringify(input),
  });
}

export async function updateUser(
  username: string,
  input: UpdateUserInput,
): Promise<User> {
  return await fetchApi<User>(`/users/${encodeURIComponent(username)}`, {
    method: "PUT",
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
    body: JSON.stringify(input),
  });
}

export async function deleteUser(username: string): Promise<void> {
  await fetchApi<void>(`/users/${encodeURIComponent(username)}`, {
    method: "DELETE",
    headers: {
      Authorization: `Bearer ${getAccessToken()}`,
    },
  });
}
