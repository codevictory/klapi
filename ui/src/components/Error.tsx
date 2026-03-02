import { useSearchParams } from "@solidjs/router";
import { createEffect, onCleanup, Show } from "solid-js";
import { t } from "~/i18n";
import { X } from "./Icons";

export default function ErrorNotification() {
  const [searchParams, setSearchParams] = useSearchParams();

  createEffect(() => {
    if (searchParams.error) {
      const timer = setTimeout(() => setSearchParams({ error: "" }), 5000);
      onCleanup(() => clearTimeout(timer));
    }
  });

  return (
    <Show when={typeof searchParams.error === "string" && searchParams.error} keyed>
      {msg => (
        <aside class="flex items-start gap-3 fixed bottom-4 left-4 max-w-sm bg-[#F5D1A9] border border-[#C99763] rounded-xl p-4 shadow-lg z-50 transition-all duration-300 text-sm">
          <div>
            <strong class="font-medium text-[#4C250E]">{t("error.title")}</strong>
            <p class="text-[#70421E] mt-1 select-text">{msg}</p>
          </div>
          <button
            type="button"
            onclick={() => setSearchParams({ error: "" })}
            class="text-[#A56C38] hover:text-[#4C250E] transition-colors"
          >
            <X class="w-4 h-4" />
          </button>
        </aside>
      )}
    </Show>
  );
}
