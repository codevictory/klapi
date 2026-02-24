import { createSignal } from "solid-js";
import { isServer } from "solid-js/web";

export type Language = "fi" | "en";

const STORAGE_KEY = "ui-language";

const translations = {
  en: {
    "nav.home": "Home",
    "nav.about": "About",
    "nav.login": "Login",
    "nav.signOut": "Sign Out",
    "nav.language.fi": "FI",
    "nav.language.en": "EN",
    "meta.description": "SolidStart with-auth example",
    "home.title": "Home",
    "home.heading": "KlAPI",
    "home.signedInAs": "You are signed in as",
    "home.logoAlt": "logo",
    "home.openHours.heading": "Open hours versions",
    "home.openHours.latest": "Latest",
    "home.openHours.new": "New version",
    "home.openHours.reuse": "Reuse selected",
    "home.openHours.delete": "Delete selected",
    "home.openHours.empty": "No open-hours versions found yet",
    "home.openHours.name": "Version name",
    "home.openHours.nameRequired": "Version name is required",
    "home.openHours.paragraph1": "Paragraph 1",
    "home.openHours.paragraph2": "Paragraph 2",
    "home.openHours.paragraph3": "Paragraph 3",
    "home.openHours.paragraph4": "Paragraph 4",
    "home.openHours.kitchenNotice": "Kitchen notice",
    "home.openHours.submit": "Add new version",
    "home.openHours.saving": "Saving...",
    "home.openHours.saved": "New version saved",
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
    "counter.clicks": "Clicks",
    "errors.requiredEmailPassword": "Email and password are required",
    "errors.invalidStoredPasswordFormat": "Invalid stored password format",
    "errors.invalidEmailOrPassword": "Invalid email or password",
    "errors.oauthOnly":
      "Account exists via OAuth. Sign in with your OAuth provider",
  },
  fi: {
    "nav.home": "Etusivu",
    "nav.about": "Tietoja",
    "nav.login": "Kirjaudu",
    "nav.signOut": "Kirjaudu ulos",
    "nav.language.fi": "FI",
    "nav.language.en": "EN",
    "meta.description": "SolidStart with-auth -esimerkki",
    "home.title": "Etusivu",
    "home.heading": "KlAPI",
    "home.signedInAs": "Olet kirjautunut käyttäjänä",
    "home.logoAlt": "logo",
    "home.openHours.heading": "Aukioloaikaversiot",
    "home.openHours.latest": "Viimeisin",
    "home.openHours.new": "Uusi versio",
    "home.openHours.reuse": "Käytä valittua uudelleen",
    "home.openHours.delete": "Poista valittu",
    "home.openHours.empty": "Aukioloaikaversioita ei vielä löytynyt",
    "home.openHours.name": "Version nimi",
    "home.openHours.nameRequired": "Version nimi on pakollinen",
    "home.openHours.paragraph1": "Kappale 1",
    "home.openHours.paragraph2": "Kappale 2",
    "home.openHours.paragraph3": "Kappale 3",
    "home.openHours.paragraph4": "Kappale 4",
    "home.openHours.kitchenNotice": "Keittiöhuomio",
    "home.openHours.submit": "Lisää uusi versio",
    "home.openHours.saving": "Tallennetaan...",
    "home.openHours.saved": "Uusi versio tallennettu",
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
    "counter.clicks": "Klikkauksia",
    "errors.requiredEmailPassword": "Sähköposti ja salasana vaaditaan",
    "errors.invalidStoredPasswordFormat":
      "Tallennetun salasanan muoto on virheellinen",
    "errors.invalidEmailOrPassword": "Virheellinen sähköposti tai salasana",
    "errors.oauthOnly": "Tili löytyy OAuthin kautta. Kirjaudu OAuth-palvelulla",
  },
} as const;

export type TranslationKey = keyof typeof translations.en;

export const normalizeLanguage = (value: unknown): Language =>
  value === "fi" ? "fi" : "en";

const [language, setLanguageSignal] = createSignal<Language>("en");

if (!isServer) {
  const stored = normalizeLanguage(localStorage.getItem(STORAGE_KEY));
  setLanguageSignal(stored);
}

export const setLanguage = (lang: Language) => {
  setLanguageSignal(lang);
  if (!isServer) {
    localStorage.setItem(STORAGE_KEY, lang);
  }
};

export { language };

export const getTranslations = (lang: Language) => translations[lang];

export const t = (key: TranslationKey) => translations[language()][key];

export const getLanguageFromFormData = (formData: FormData): Language =>
  normalizeLanguage(formData.get("lang"));
