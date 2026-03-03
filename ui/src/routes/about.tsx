import { useEffect, useState } from "react";
import { queryApiVersion } from "~/api";
import { useT } from "~/i18n";

export default function About() {
  const t = useT();
  const [apiVersion, setApiVersion] = useState<string | null>(null);

  useEffect(() => {
    document.title = t("about.title");
  }, [t]);

  useEffect(() => {
    let active = true;

    queryApiVersion()
      .then((version) => {
        if (active) {
          setApiVersion(version);
        }
      })
      .catch(() => {
        if (active) {
          setApiVersion(null);
        }
      });

    return () => {
      active = false;
    };
  }, []);

  return (
    <main className="space-y-4 px-4">
      <p className="mx-auto max-w-md text-center text-lg font-semibold text-[#70421E]">
        {t("about.description")}
      </p>
      <p className="mx-auto max-w-md text-center text-sm text-[#70421E]/90">
        {t("about.bugReportsPrefix")} {" "}
        <a
          href="mailto:info@tietokonepaja.fi"
          className="font-medium underline underline-offset-2 hover:opacity-80 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-current"
        >
          info@tietokonepaja.fi
        </a>
        .
      </p>
      <p className="mx-auto max-w-md text-center text-xs uppercase tracking-wide text-[#70421E]/80">
        {t("about.apiVersion")}: {apiVersion ?? t("about.loading")}
      </p>
    </main>
  );
}
