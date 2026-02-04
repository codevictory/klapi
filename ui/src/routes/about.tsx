import { Title } from "@solidjs/meta";
import { createAsync } from "@solidjs/router";
import { Show } from "solid-js";
import { queryApiVersion } from "~/api";
import { t } from "~/i18n";

export default function About() {
  const apiVersion = createAsync(() => queryApiVersion());

  return (
    <main>
      <Title>{t("about.title")}</Title>
      <p class="text-gray-700 text-center">
        {t("about.apiVersion")}:
        <Show when={apiVersion()} fallback={t("about.loading")}>
          {apiVersion()}
        </Show>
      </p>
    </main>
  );
}
