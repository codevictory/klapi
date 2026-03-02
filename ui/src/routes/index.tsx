import { useEffect } from "react";
import OpenHoursForm from "~/components/OpenHoursForm";
import { useT } from "~/i18n";

export default function Home() {
  const t = useT();

  useEffect(() => {
    document.title = t("home.title");
  }, [t]);

  return (
    <main>
      <h1 className="text-center">{t("home.heading")}</h1>
      <img src="/favicon.svg" alt={t("home.logoAlt")} className="w-28" />
      <OpenHoursForm />
    </main>
  );
}
