import { Title } from "@solidjs/meta";
import { useSubmission } from "@solidjs/router";
import { Show } from "solid-js";
import { useOAuthLogin } from "start-oauth";
import { formLogin } from "~/auth";
import { language, t } from "~/i18n";

export default function Login() {
  const login = useOAuthLogin();

  return (
    <main>
      <Title>{t("login.title")}</Title>
      <h1>{t("login.heading")}</h1>
      <div class="space-y-6 font-medium">
        <PasswordLogin />
      </div>
    </main>
  );
}

function PasswordLogin() {
  const submission = useSubmission(formLogin);

  return (
    <form action={formLogin} method="post" class="space-y-4 space-x-12">
      <input type="hidden" name="lang" value={language()} />
      <label for="email" class="block text-left w-full">
        {t("login.email")}
        <input
          id="email"
          name="email"
          type="email"
          autocomplete="email"
          placeholder="john@doe.com"
          required
          class="bg-[#FFF7EE] mt-1 block w-full px-4 py-2 border border-[#C99763] rounded-md focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
        />
      </label>
      <label for="password" class="block text-left w-full">
        {t("login.password")}
        <input
          id="password"
          name="password"
          type="password"
          autocomplete="current-password"
          placeholder="••••••••"
          minLength={6}
          required
          class="bg-[#FFF7EE] mt-1 block w-full px-4 py-2 border border-[#C99763] rounded-md focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
        />
      </label>
      <button
        type="submit"
        disabled={submission.pending}
        class="w-full px-4 py-2 bg-gradient-to-r from-[#A56C38] to-[#70421E] text-[#FFF7EE] rounded-lg hover:from-[#8E4F24] hover:to-[#4C250E] focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed transition-colors duration-300 shadow-lg shadow-[#70421E]/30"
      >
        {t("login.submit")}
      </button>
      <Show when={submission.error} keyed>
        {({ message }) => <p class="text-[#8E4F24] mt-2 text-xs text-center">{message}</p>}
      </Show>
    </form>
  );
}
