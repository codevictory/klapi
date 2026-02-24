import { Title } from "@solidjs/meta";
import { t } from "~/i18n";

export default function NotFound() {
  return (
    <main class="text-center">
      <Title>{t("notFound.title")}</Title>
      <h1>{t("notFound.heading")}</h1>
      {t("notFound.message")}
      <a
        href="/"
        class="px-4 py-2 border border-[#C99763] rounded-xl text-[#70421E] hover:bg-[#F5D1A9] transition-colors duration-200"
      >
        {t("notFound.goHome")}
      </a>
    </main>
  );
}
