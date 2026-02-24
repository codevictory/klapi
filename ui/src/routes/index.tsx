import { Title } from "@solidjs/meta";
import { useAuth } from "~/components/Context";
import { t } from "~/i18n";

export default function Home() {
  const { session } = useAuth();

  return (
    <main>
      <Title>{t("home.title")}</Title>
      <h1 class="text-center">{t("home.heading")}</h1>
      <img src="/favicon.svg" alt={t("home.logoAlt")} class="w-28" />
    </main>
  );
}
