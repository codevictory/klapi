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
    "home.heading": "Hello World",
    "home.signedInAs": "You are signed in as",
    "home.logoAlt": "logo",
    "about.title": "About",
    "about.apiVersion": "API version",
    "about.loading": "Loading...",
    "login.title": "Sign In",
    "login.heading": "Sign in",
    "login.orContinueWith": "Or continue with",
    "login.signInWithDiscord": "Sign in with Discord",
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
    "home.heading": "Hei maailma",
    "home.signedInAs": "Olet kirjautunut käyttäjänä",
    "home.logoAlt": "logo",
    "about.title": "Tietoja",
    "about.apiVersion": "API-versio",
    "about.loading": "Ladataan...",
    "login.title": "Kirjaudu",
    "login.heading": "Kirjaudu sisään",
    "login.orContinueWith": "Tai jatka",
    "login.signInWithDiscord": "Kirjaudu Discordilla",
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
