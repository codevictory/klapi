import { Link } from "react-router-dom";
import { useEffect } from "react";
import { useT } from "~/i18n";

export default function NotFound() {
  const t = useT();

  useEffect(() => {
    document.title = t("notFound.title");
  }, [t]);

  return (
    <main className="text-center">
      <h1>{t("notFound.heading")}</h1>
      <p>{t("notFound.message")}</p>
      <Link
        to="/"
        className="rounded-xl border border-[#C99763] px-4 py-2 text-[#70421E] transition-colors duration-200 hover:bg-[#F5D1A9]"
      >
        {t("notFound.goHome")}
      </Link>
    </main>
  );
}
