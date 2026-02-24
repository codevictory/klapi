import { Title } from "@solidjs/meta";
import OpenHoursForm from "~/components/OpenHoursForm";
import { t } from "~/i18n";

export default function Home() {
  return (
    <main>
      <Title>{t("home.title")}</Title>
      <h1 class="text-center">{t("home.heading")}</h1>
      <img src="/favicon.svg" alt={t("home.logoAlt")} class="w-28" />
      <OpenHoursForm />
    </main>
  );
}
