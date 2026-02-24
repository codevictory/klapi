import { useMatch } from "@solidjs/router";
import { Show } from "solid-js";
import { useAuth } from "~/components/Context";
import { language, setLanguage, t } from "~/i18n";

export default function Nav() {
  const { session, signedIn, logout } = useAuth();
  const isHome = useMatch(() => "/");
  const isAbout = useMatch(() => "/about");

  return (
    <nav class="fixed top-0 left-0 w-full bg-[#70421E] shadow-sm z-50 flex items-center justify-between py-3 px-4 font-medium text-sm">
      <a
        href="/"
        class={`px-3 py-2 text-[#F5D1A9] uppercase transition-colors duration-200 border-b-2 ${isHome() ? "border-[#E3A977] text-[#FFF7EE]" : "border-transparent hover:text-[#FFF7EE]"
          }`}
      >
        {t("nav.home")}
      </a>
      <a
        href="/about"
        class={`px-3 py-2 text-[#F5D1A9] uppercase transition-colors duration-200 border-b-2 ${isAbout() ? "border-[#E3A977] text-[#FFF7EE]" : "border-transparent hover:text-[#FFF7EE]"
          }`}
      >
        {t("nav.about")}
      </a>
      <div class="ml-auto flex items-center gap-2">
        <b class="font-large text-[#F5D1A9]">{session()?.email}</b>
        <div class="flex items-center gap-1 rounded-md border border-[#8E4F24] bg-[#8E4F24]/45 p-1">
          <button
            type="button"
            onclick={() => setLanguage("fi")}
            class={`px-2 py-1 text-xs rounded ${language() === "fi" ? "bg-[#E3A977] text-[#4C250E]" : "text-[#F5D1A9] hover:text-[#FFF7EE]"
              }`}
          >
            {t("nav.language.fi")}
          </button>
          <button
            type="button"
            onclick={() => setLanguage("en")}
            class={`px-2 py-1 text-xs rounded ${language() === "en" ? "bg-[#E3A977] text-[#4C250E]" : "text-[#F5D1A9] hover:text-[#FFF7EE]"
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
              class="px-4 py-2 text-[#F5D1A9] bg-[#8E4F24] border border-[#A56C38] rounded-md hover:bg-[#A56C38] hover:text-[#FFF7EE] focus:outline-none transition-colors duration-200"
            >
              {t("nav.login")}
            </a>
          }
        >
          <form action={logout} method="post">
            <button
              type="submit"
              class="px-4 py-2 text-[#F5D1A9] bg-[#8E4F24] border border-[#A56C38] rounded-md hover:bg-[#A56C38] hover:text-[#FFF7EE] focus:outline-none transition-colors duration-200"
            >
              {t("nav.signOut")}
            </button>
          </form>
        </Show>
      </div>
    </nav>
  );
}
