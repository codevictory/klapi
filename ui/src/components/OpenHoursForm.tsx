import { createAsync, useSubmission } from "@solidjs/router";
import { For, Show, createEffect, createMemo, createSignal } from "solid-js";
import { createLokOpenHours, deleteLokOpenHours, queryLokOpenHours } from "~/api";
import { t } from "~/i18n";

const NEW_VERSION_OPTION = "__new__";

export default function OpenHoursForm() {
  const [refreshKey, setRefreshKey] = createSignal(0);
  const openHours = createAsync(() => queryLokOpenHours(refreshKey()).catch(() => []));
  const createSubmission = useSubmission(createLokOpenHours);
  const deleteSubmission = useSubmission(deleteLokOpenHours);
  const [selectedVersion, setSelectedVersion] = createSignal("");
  const [name, setName] = createSignal("");
  const [paragraph1, setParagraph1] = createSignal("");
  const [paragraph2, setParagraph2] = createSignal("");
  const [paragraph3, setParagraph3] = createSignal("");
  const [paragraph4, setParagraph4] = createSignal("");
  const [kitchenNotice, setKitchenNotice] = createSignal("");

  const latestFive = createMemo(() => openHours() ?? []);

  const selectedOpenHours = createMemo(() =>
    latestFive().find(version => String(version.id) === selectedVersion())
  );

  createEffect(() => {
    if (!createSubmission.result) return;
    setRefreshKey(previous => previous + 1);
    setSelectedVersion("");
  });

  createEffect(() => {
    if (!deleteSubmission.result) return;
    setRefreshKey(previous => previous + 1);
    setSelectedVersion("");
  });

  createEffect(() => {
    const versions = latestFive();
    const current = selectedVersion();

    if (versions.length === 0) {
      if (current !== NEW_VERSION_OPTION) {
        setSelectedVersion(NEW_VERSION_OPTION);
      }
      return;
    }

    if (current === NEW_VERSION_OPTION) {
      return;
    }

    const hasCurrent = versions.some(version => String(version.id) === current);
    if (!current || !hasCurrent) {
      setSelectedVersion(String(versions[0].id));
    }
  });

  const reuseSelected = () => {
    const selected = selectedOpenHours();
    if (!selected) {
      setName("");
      setParagraph1("");
      setParagraph2("");
      setParagraph3("");
      setParagraph4("");
      setKitchenNotice("");
      return;
    }

    setName(selected.name);
    setParagraph1(selected.paragraph1);
    setParagraph2(selected.paragraph2);
    setParagraph3(selected.paragraph3);
    setParagraph4(selected.paragraph4);
    setKitchenNotice(selected.kitchenNotice);
  };

  createEffect(() => {
    reuseSelected();
  });

  return (
    <section class="w-full max-w-3xl rounded-2xl border border-[#C99763] bg-[#F5D1A9] p-6 shadow-md">
      <h2 class="text-2xl font-semibold text-[#4C250E]">{t("home.openHours.heading")}</h2>

      <Show when={latestFive().length > 0} fallback={<p class="mt-3 text-[#70421E]">{t("home.openHours.empty")}</p>}>
        <div class="mt-2 flex gap-2">
          <select
            id="open-hours-version"
            class="min-w-0 flex-1 rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
            value={selectedVersion()}
            onInput={event => setSelectedVersion(event.currentTarget.value)}
          >
            <For each={latestFive()}>
              {version => (
                <option value={String(version.id)}>
                  {version.name || t("home.openHours.latest")} · {new Date(version.version).toLocaleString()}
                </option>
              )}
            </For>
            <option value={NEW_VERSION_OPTION}>{t("home.openHours.new")}</option>
          </select>
        </div>

        <form action={deleteLokOpenHours} method="post" class="mt-3">
          <input type="hidden" name="id" value={selectedOpenHours()?.id ?? ""} />
          <button
            type="submit"
            disabled={!selectedOpenHours() || deleteSubmission.pending}
            class="rounded-md border border-[#8E4F24] bg-[#EED5B8] px-4 py-2 text-[#70421E] hover:bg-[#E3A977] disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {t("home.openHours.delete")}
          </button>
        </form>
      </Show>

      <form action={createLokOpenHours} method="post" class="mt-5 space-y-3">
        <div>
          <label for="name" class="block text-sm font-medium text-[#4C250E]">{t("home.openHours.name")}</label>
          <input
            id="name"
            name="name"
            required
            value={name()}
            onInput={event => setName(event.currentTarget.value)}
            class="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
          <Show when={!name().trim()}>
            <p class="mt-1 text-xs text-[#8E4F24]">{t("home.openHours.nameRequired")}</p>
          </Show>
        </div>

        <div>
          <label for="paragraph1" class="block text-sm font-medium text-[#4C250E]">{t("home.openHours.paragraph1")}</label>
          <textarea
            id="paragraph1"
            name="paragraph1"
            rows={2}
            value={paragraph1()}
            onInput={event => setParagraph1(event.currentTarget.value)}
            class="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </div>

        <div>
          <label for="paragraph2" class="block text-sm font-medium text-[#4C250E]">{t("home.openHours.paragraph2")}</label>
          <textarea
            id="paragraph2"
            name="paragraph2"
            rows={2}
            value={paragraph2()}
            onInput={event => setParagraph2(event.currentTarget.value)}
            class="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </div>

        <div>
          <label for="paragraph3" class="block text-sm font-medium text-[#4C250E]">{t("home.openHours.paragraph3")}</label>
          <textarea
            id="paragraph3"
            name="paragraph3"
            rows={2}
            value={paragraph3()}
            onInput={event => setParagraph3(event.currentTarget.value)}
            class="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </div>

        <div>
          <label for="paragraph4" class="block text-sm font-medium text-[#4C250E]">{t("home.openHours.paragraph4")}</label>
          <textarea
            id="paragraph4"
            name="paragraph4"
            rows={2}
            value={paragraph4()}
            onInput={event => setParagraph4(event.currentTarget.value)}
            class="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </div>

        <div>
          <label for="kitchenNotice" class="block text-sm font-medium text-[#4C250E]">{t("home.openHours.kitchenNotice")}</label>
          <textarea
            id="kitchenNotice"
            name="kitchenNotice"
            rows={2}
            value={kitchenNotice()}
            onInput={event => setKitchenNotice(event.currentTarget.value)}
            class="mt-1 w-full rounded-md border border-[#C99763] bg-[#FFF7EE] px-3 py-2 text-[#4C250E] focus:outline-none focus:ring-2 focus:ring-[#A56C38]"
          />
        </div>

        <div class="flex flex-wrap gap-3 pt-1">
          <button
            type="button"
            disabled={!selectedOpenHours()}
            onClick={reuseSelected}
            class="rounded-md border border-[#A56C38] bg-[#EED5B8] px-4 py-2 text-[#70421E] hover:bg-[#E3A977] disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {t("home.openHours.reuse")}
          </button>
          <button
            type="submit"
            disabled={createSubmission.pending}
            class="rounded-md border border-[#70421E] bg-[#8E4F24] px-4 py-2 text-[#FFF7EE] hover:bg-[#70421E] disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {createSubmission.pending ? t("home.openHours.saving") : t("home.openHours.submit")}
          </button>
        </div>

        <Show when={createSubmission.result}>
          <p class="text-sm text-[#4C250E]">{t("home.openHours.saved")}</p>
        </Show>
        <Show when={deleteSubmission.result}>
          <p class="text-sm text-[#4C250E]">{t("home.openHours.deleted")}</p>
        </Show>
        <Show when={createSubmission.error} keyed>
          {error => <p class="text-sm text-[#8E4F24]">{error.message}</p>}
        </Show>
        <Show when={deleteSubmission.error} keyed>
          {error => <p class="text-sm text-[#8E4F24]">{error.message}</p>}
        </Show>
      </form>
    </section>
  );
}
