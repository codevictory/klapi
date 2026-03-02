import { atom } from "recoil";
import type { LokOpenHours } from "~/api";

export type Language = "fi" | "en";

export type Session = {
  email: string;
  token: string;
};

export type Toast = {
  id: number;
  message: string;
  kind: "success" | "error";
  leaving: boolean;
};

export const languageAtom = atom<Language>({
  key: "languageAtom",
  default: "en",
});

export const sessionAtom = atom<Session | null>({
  key: "sessionAtom",
  default: null,
});

export const openHoursAtom = atom<LokOpenHours[]>({
  key: "openHoursAtom",
  default: [],
});

export const toastsAtom = atom<Toast[]>({
  key: "toastsAtom",
  default: [],
});
