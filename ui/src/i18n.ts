import { useMemo } from "react";
import { useRecoilState, useRecoilValue } from "recoil";
import { languageAtom, type Language } from "~/state/appState";

const STORAGE_KEY = "ui-language";

const translations = {
  en: {
    "nav.home": "Home",
    "nav.about": "About",
    "nav.login": "Login",
    "nav.signOut": "Sign Out",
    "nav.language.fi": "FI",
    "nav.language.en": "EN",
    "meta.description": "React + Recoil example",
    "home.title": "Home",
    "home.heading": "KlAPI",
    "home.signedInAs": "You are signed in as",
    "home.logoAlt": "logo",
    "home.openHours.heading": "Open hours versions",
    "home.openHours.latest": "Latest",
    "home.openHours.new": "New version",
    "home.openHours.edit": "Edit",
    "home.openHours.cancel": "Cancel",
    "home.openHours.editing": "Editing version",
    "home.openHours.active": "Active",
    "home.openHours.reuse": "Reuse selected",
    "home.openHours.delete": "Delete",
    "home.openHours.empty": "No open-hours versions found yet",
    "home.openHours.name": "Version name",
    "home.openHours.nameRequired": "Version name is required",
    "home.openHours.paragraph1": "Paragraph 1",
    "home.openHours.paragraph2": "Paragraph 2",
    "home.openHours.paragraph3": "Paragraph 3",
    "home.openHours.paragraph4": "Paragraph 4",
    "home.openHours.kitchenNotice": "Kitchen notice",
    "home.openHours.submit": "Add new version",
    "home.openHours.update": "Save changes",
    "home.openHours.saving": "Saving...",
    "home.openHours.updating": "Saving changes...",
    "home.openHours.saved": "New version saved",
    "home.openHours.updated": "Version updated",
    "home.openHours.deleted": "Version deleted",
    "about.title": "About",
    "about.apiVersion": "API version",
    "about.loading": "Loading...",
    "login.title": "Sign In",
    "login.heading": "Sign in",
    "login.email": "Email",
    "login.password": "Password",
    "login.submit": "Submit",
    "notFound.title": "Page Not Found",
    "notFound.heading": "Not Found",
    "notFound.message": "Sorry, the page you’re looking for doesn't exist",
    "notFound.goHome": "Go Home",
    "error.title": "Error",
    "errors.requiredEmailPassword": "Email and password are required",
    "errors.invalidEmailOrPassword": "Invalid email or password",
  },
  fi: {
    "nav.home": "Etusivu",
    "nav.about": "Tietoja",
    "nav.login": "Kirjaudu",
    "nav.signOut": "Kirjaudu ulos",
    "nav.language.fi": "FI",
    "nav.language.en": "EN",
    "meta.description": "React + Recoil -esimerkki",
    "home.title": "Etusivu",
    "home.heading": "KlAPI",
    "home.signedInAs": "Olet kirjautunut käyttäjänä",
    "home.logoAlt": "logo",
    "home.openHours.heading": "Aukioloaikaversiot",
    "home.openHours.latest": "Viimeisin",
    "home.openHours.new": "Uusi versio",
    "home.openHours.edit": "Muokkaa",
    "home.openHours.cancel": "Peruuta",
    "home.openHours.editing": "Muokataan versiota",
    "home.openHours.active": "Aktiivinen",
    "home.openHours.reuse": "Käytä valittua uudelleen",
    "home.openHours.delete": "Poista",
    "home.openHours.empty": "Aukioloaikaversioita ei vielä löytynyt",
    "home.openHours.name": "Version nimi",
    "home.openHours.nameRequired": "Version nimi on pakollinen",
    "home.openHours.paragraph1": "Kappale 1",
    "home.openHours.paragraph2": "Kappale 2",
    "home.openHours.paragraph3": "Kappale 3",
    "home.openHours.paragraph4": "Kappale 4",
    "home.openHours.kitchenNotice": "Keittiöhuomio",
    "home.openHours.submit": "Lisää uusi versio",
    "home.openHours.update": "Tallenna muutokset",
    "home.openHours.saving": "Tallennetaan...",
    "home.openHours.updating": "Tallennetaan muutoksia...",
    "home.openHours.saved": "Uusi versio tallennettu",
    "home.openHours.updated": "Versio päivitetty",
    "home.openHours.deleted": "Versio poistettu",
    "about.title": "Tietoja",
    "about.apiVersion": "API-versio",
    "about.loading": "Ladataan...",
    "login.title": "Kirjaudu",
    "login.heading": "Kirjaudu sisään",
    "login.email": "Sähköposti",
    "login.password": "Salasana",
    "login.submit": "Lähetä",
    "notFound.title": "Sivua ei löytynyt",
    "notFound.heading": "Ei löytynyt",
    "notFound.message": "Valitettavasti etsimääsi sivua ei ole olemassa",
    "notFound.goHome": "Takaisin etusivulle",
    "error.title": "Virhe",
    "errors.requiredEmailPassword": "Sähköposti ja salasana vaaditaan",
    "errors.invalidEmailOrPassword": "Virheellinen sähköposti tai salasana",
  },
} as const;

export type TranslationKey = keyof typeof translations.en;

export const normalizeLanguage = (value: unknown): Language =>
  value === "fi" ? "fi" : "en";

export const initializeLanguage = (setLanguage: (lang: Language) => void) => {
  const stored = normalizeLanguage(localStorage.getItem(STORAGE_KEY));
  setLanguage(stored);
};

export const useLanguage = () => {
  const [language, setLanguageState] = useRecoilState(languageAtom);

  const setLanguage = (lang: Language) => {
    setLanguageState(lang);
    localStorage.setItem(STORAGE_KEY, lang);
  };

  return { language, setLanguage };
};

export const useT = () => {
  const language = useRecoilValue(languageAtom);

  return useMemo(
    () => (key: TranslationKey) => translations[language][key],
    [language],
  );
};
