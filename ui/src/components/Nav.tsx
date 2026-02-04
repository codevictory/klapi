import { useMatch } from "@solidjs/router";
import { Show } from "solid-js";
import { useAuth } from "~/components/Context";
import { language, setLanguage, t } from "~/i18n";

export default function Nav() {
  const { signedIn, logout } = useAuth();
  const isHome = useMatch(() => "/");
  const isAbout = useMatch(() => "/about");

  return (
    <nav class="fixed top-0 left-0 w-full bg-sky-800 shadow-sm z-50 flex items-center justify-between py-3 px-4 font-medium text-sm">
      <a
        href="/"
        class={`px-3 py-2 text-sky-100 uppercase transition-colors duration-200 border-b-2 ${isHome() ? "border-sky-300 text-white" : "border-transparent hover:text-white"
          }`}
      >
        {t("nav.home")}
      </a>
      <a
        href="/about"
        class={`px-3 py-2 text-sky-100 uppercase transition-colors duration-200 border-b-2 ${isAbout() ? "border-sky-300 text-white" : "border-transparent hover:text-white"
          }`}
      >
        {t("nav.about")}
      </a>
      <div class="ml-auto flex items-center gap-2">
        <div class="flex items-center gap-1 rounded-md border border-sky-700 bg-sky-700/40 p-1">
          <button
            type="button"
            onclick={() => setLanguage("fi")}
            class={`px-2 py-1 text-xs rounded ${language() === "fi" ? "bg-sky-200 text-sky-900" : "text-sky-100 hover:text-white"
              }`}
          >
            {t("nav.language.fi")}
          </button>
          <button
            type="button"
            onclick={() => setLanguage("en")}
            class={`px-2 py-1 text-xs rounded ${language() === "en" ? "bg-sky-200 text-sky-900" : "text-sky-100 hover:text-white"
              }`}
          >
            {t("nav.language.en")}
          </button>
        </div>
        <Show
          when={signedIn()}
          fallback={
            <a
              href="/login"
              class="px-4 py-2 text-sky-100 bg-sky-700 border border-sky-600 rounded-md hover:bg-sky-600 hover:text-white focus:outline-none transition-colors duration-200"
            >
              {t("nav.login")}
            </a>
          }
        >
          <form action={logout} method="post">
            <button
              type="submit"
              class="px-4 py-2 text-sky-100 bg-sky-700 border border-sky-600 rounded-md hover:bg-sky-600 hover:text-white focus:outline-none transition-colors duration-200"
            >
              {t("nav.signOut")}
            </button>
          </form>
        </Show>
      </div>
    </nav>
  );
}
