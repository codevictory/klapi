import { useEffect } from "react";
import OpenHoursForm from "~/components/OpenHoursForm";
import RoleGuard from "~/components/RoleGuard";
import { APP_ROLES } from "~/auth/roles";
import { useT } from "~/i18n";

export default function Home() {
  const t = useT();

  useEffect(() => {
    document.title = t("home.title");
  }, [t]);

  return (
    <main>
      <h1 className="text-center">{t("home.heading")}</h1>
      <h2 className="text-center">{t("home.subheading")}</h2>
      <img src="/favicon.svg" alt={t("home.logoAlt")} className="w-28" />
      <RoleGuard roles={[APP_ROLES.lok]}>
        <OpenHoursForm />
      </RoleGuard>
    </main>
  );
}
