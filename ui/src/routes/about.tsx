import { Title } from "@solidjs/meta";
import { createAsync } from "@solidjs/router";
import { Show } from "solid-js";
import Counter from "~/components/Counter";
import { queryApiVersion } from "~/api";

export default function About() {
  const apiVersion = createAsync(() => queryApiVersion());

  return (
    <main>
      <Title>Tietoja</Title>
      <p class="text-gray-700 text-center">
        API version:{" "}
        <Show when={apiVersion()} fallback="Loading...">
          {apiVersion()}
        </Show>
      </p>
    </main>
  );
}
