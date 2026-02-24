import { createSignal } from "solid-js";
import { t } from "~/i18n";

export default function Counter() {
  const [count, setCount] = createSignal(0);

  return (
    <button
      class="w-52 rounded-full bg-[#F5D1A9] border-2 border-[#C99763] text-[#4C250E] focus:border-[#A56C38] hover:bg-[#EED5B8] transition-colors py-4"
      onclick={() => setCount(prev => prev + 1)}
    >
      {t("counter.clicks")}: {count()}
    </button>
  );
}
