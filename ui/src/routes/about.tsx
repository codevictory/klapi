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
    <main>
      <p className="text-center text-[#70421E]">
        {t("about.apiVersion")}: {apiVersion ?? t("about.loading")}
      </p>
    </main>
  );
}
